using System.Diagnostics;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Fetcher.Library.Exceptions;
using Fetcher.Library.Models;
using Fetcher.Library.Utilities;

namespace Fetcher.Library;
public class AzureBlobFile(
    dynamic uri,
    string? localPath = null, 
    string? accountKey = null, 
    int threads = 1,
    bool writeDebugJson = false)
{

    public readonly DownloadInfo DownloadInfo = new();
    private readonly bool _WriteDebugJson = writeDebugJson;
    readonly Stopwatch TimeProcess = new();
    readonly Stopwatch TimeDownload = new();
    readonly Stopwatch TimeAssembly = new();
    readonly Stopwatch TimeValidation = new();

    public string TotalDownloadTime => $"{TimeDownload.Elapsed:hh\\:mm\\:ss}";
    public string TotalBytesDownloaded => $"{Format.ByteUnits(DownloadInfo.TotalBytesDownloaded)}";
    public string AverageDownloadSpeed => TimeDownload.Elapsed.Seconds > 0 
        ? $"{Format.ByteUnits(DownloadInfo.TotalBytesDownloaded / TimeDownload.Elapsed.Seconds)}/s"
        : string.Empty;
    public string PercentDownloaded => $"{DownloadInfo.PercentDownloaded * 100 :N0} %";
    public string TotalBytesOnDisk => $"{Format.ByteUnits(DownloadInfo.TotalBytesSavedToDisk)}";
    
    private static readonly Random random = new();
    protected const int ChunkSize = 64 * 1024 * 1024; // 64 MB per chunk (67108864 bytes)

    protected readonly Uri? Uri
        = uri is Uri ? uri as Uri
        : new($"{uri}");
    protected readonly string LocalPath = !string.IsNullOrEmpty(localPath) 
        ? localPath
        : Directory.GetCurrentDirectory();
    protected readonly string? AzureAccountKey = accountKey;
    protected readonly int Threads = threads;
    protected string Filename 
    {
        get
        {
            if (Directory.Exists(LocalPath))
            {
                var filename = Path.GetFileName(Uri?.LocalPath);
                if (string.IsNullOrEmpty(filename))
                    throw new FileNotFoundException($"Error: Could not determine file name from blob URL '{Uri}'.");

                return Path.Combine(LocalPath, filename);
            }
            return LocalPath;
        }
    }
    private BlobClient? _BlobClient;
    protected (BlobClient Client, BlobProperties Properties) Blob 
    { 
        get 
        {
            _BlobClient ??= !string.IsNullOrEmpty(AzureAccountKey) 
                ? new (Uri, 
                    new Azure.Storage.StorageSharedKeyCredential(
                        Uri?.Host.Split('.')[0], AzureAccountKey))
                : new (Uri, new DefaultAzureCredential());

            DownloadInfo.BlobProperties ??= _BlobClient.GetProperties();

            return (_BlobClient, DownloadInfo.BlobProperties);
        }
    }
    
    public async Task DownloadAsync()
    {
        TimeProcess.Start();

        CheckLocalFile();

        int totalChunks = (int)Math.Ceiling((double)Blob.Properties.ContentLength / ChunkSize);

        List<Task> tasks = [];
        var throttler = new SemaphoreSlim(Threads);

        TimeDownload.Start();
        for (int i = 0; i < totalChunks; i++)
            PrepareChunkInfo(i);

        if(_WriteDebugJson) await DownloadInfo.SaveAsync();
        
        foreach(var chunk in DownloadInfo.Chunks)
        {   
            await throttler.WaitAsync();

            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    await DownloadChunkAsync(chunk);
                }
                finally
                {
                    throttler.Release();
                    if(_WriteDebugJson) await DownloadInfo.SaveAsync();
                }
            }));
        }
        await Task.WhenAll(tasks);
        if(_WriteDebugJson) await DownloadInfo.SaveAsync();
        TimeDownload.Stop();

        TimeAssembly.Start();
        await AssembleFile();
        TimeAssembly.Stop();
        
        TimeValidation.Start();
        _ = await PassesIntegrityCheckAsync();
        TimeValidation.Stop();
        
        TimeProcess.Stop();
    }

    private void PrepareChunkInfo(int index)
    {
        var offset = index * ChunkSize;
        ChunkInfo chunkInfo = new(index, $"{Filename}.{index:X4}")
        {
            Offset = offset,
            Length = Math.Min(ChunkSize, Blob.Properties.ContentLength - offset)
        };
        
        //chunkInfo.BytesRead = chunkInfo.FileInfo.Exists ? chunkInfo.FileInfo.Length : 0;

        DownloadInfo.Chunks.Add(chunkInfo);
    }

    private async Task DownloadChunkAsync(ChunkInfo chunk)
    {
        if (chunk.Length <= 0
        || chunk.Complete)
        {
            return;
        }

        byte[] buffer = new byte[65536];
        int bytesRead;

        var downloadResponse = await Blob.Client.DownloadStreamingAsync(
            options: chunk.BlobDownloadOptions);
        
        using var blobStream = downloadResponse.Value.Content;
        using var fileStream = new FileStream(chunk.Filename,
            FileMode.OpenOrCreate, FileAccess.Write, FileShare.Write);

        chunk.Stopwatch.Start();
        
        while ((bytesRead = await blobStream.ReadAsync(buffer)) > 0)
        {
            chunk.BytesRead += bytesRead;
            fileStream.Write(buffer, 0, bytesRead);
        }
        
        chunk.Stopwatch.Stop();
    }

    private async Task AssembleFile()
    {
        List<Task> tasks = [];
        var throttler = new SemaphoreSlim(Threads);
        object lockfile = new();

        using var FileStream = new FileStream(Filename, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Write);
        foreach (var chunk in DownloadInfo.Chunks)
        {
            await throttler.WaitAsync();

            tasks.Add(Task.Run(async () =>
            {
                try 
                {
                    using var ChunkStream = new FileStream(chunk.Filename, FileMode.Open, FileAccess.Read, FileShare.Read);

                    
                    byte[] buffer = new byte[65536];
                    int read = 0;
                    long chunkRead = 0;

                    while ((read = await ChunkStream.ReadAsync(buffer)) > 0)
                    {
                        lock (lockfile)
                        {
                            FileStream.Seek(chunk.Offset + chunkRead, SeekOrigin.Begin);
                            FileStream.Write(buffer, 0, read);
                        }
                        chunkRead += read;
                    }
                }
                finally
                {
                    throttler.Release();
                }
            }));
        }
        await Task.WhenAll(tasks);
    }

    private void CheckLocalFile()
    {
        if (File.Exists(LocalPath))
        {
            var info = new FileInfo(LocalPath);
            if (info.Length == Blob.Properties.ContentLength
                && PassesIntegrityCheckAsync().Result)
                throw new FileAlreadyDownloadedException(Uri, Filename);
        }
    }

    private async Task<bool> PassesIntegrityCheckAsync()
    {
        byte[] md5Hash = await Validate.GetFileMD5Async(Filename);
        if (!Compare.ByteArraysEqual(Blob.Properties.ContentHash, md5Hash))
        {
            throw new Md5HashMismatchException(Blob.Properties.ContentHash, md5Hash);
        }
        return true;
    }
}
