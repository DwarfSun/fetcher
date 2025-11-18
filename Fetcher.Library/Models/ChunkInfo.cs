using System.Diagnostics;
using System.Dynamic;
using Azure.Storage.Blobs.Models;

namespace Fetcher.Library.Models;
[Serializable]
public class ChunkInfo(int index, string filename)
{
    public int Index {get; set;} = index;
    public string Filename {get; set;} = filename;
    public FileInfo FileInfo = new(filename);
    public readonly Stopwatch Stopwatch = new();
    public long Offset {get; set;}
    public long Length {get; set;}
    public long BytesRead {get; set;} = 0;
    public long BytesOnDisk => File.Exists(Filename) ? new FileInfo(Filename).Length : 0;
    public long BytesRemaining => Length - BytesOnDisk;
    public long CurrentOffset => Offset + BytesOnDisk;
    public bool Complete => BytesOnDisk == Length;
    public BlobDownloadOptions? BlobDownloadOptions => BytesRemaining > 0 ? new() { Range = new Azure.HttpRange(CurrentOffset, BytesRemaining)} : null;
}


