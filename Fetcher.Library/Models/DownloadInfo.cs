using System.Text.Json;
using Azure.Storage.Blobs.Models;

namespace Fetcher.Library.Models;
[Serializable]
public class DownloadInfo()
{
    public Guid Id {get; set;} = Guid.NewGuid();
    public BlobProperties? BlobProperties {get; set;}
    public List<ChunkInfo> Chunks {get;set;} = [];
    public long TotalBytesDownloaded => Chunks.Sum(chunk => chunk.BytesRead);
    public long TotalBytesSavedToDisk => Chunks.Sum(chunk => chunk.BytesOnDisk);
    public double PercentDownloaded => BlobProperties is not null ? 
        (double)TotalBytesSavedToDisk / BlobProperties.ContentLength : 0;
    private readonly JsonSerializerOptions JsonSerializerOptions = new() { WriteIndented = true };
    public async Task SaveAsync()
    {
        try 
        {
            await File.WriteAllTextAsync($"{Directory.GetCurrentDirectory()}/Download-{Id}.json"
                , JsonSerializer.Serialize(this, JsonSerializerOptions));
        }
        catch (System.IO.IOException) {}
    }
    public void Save()
    {
        try 
        {
            File.WriteAllText($"{Directory.GetCurrentDirectory()}/Download-{Id}.json"
                , JsonSerializer.Serialize(this, JsonSerializerOptions));
        }
        catch (System.IO.IOException){}
    }
}