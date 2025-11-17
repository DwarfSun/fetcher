using System;
using System.Security.Cryptography;

namespace Fetcher.Library.Utilities;

public class Validate
{
    public static async Task<byte[]> GetFileMD5Async(string filename)
    {
        using var md5 = MD5.Create();
        using var stream = File.OpenRead(filename);
        return await md5.ComputeHashAsync(stream);
    }
}
