using System;

namespace Fetcher.Library.Utilities;

public static class Format
{
    public static string ByteUnits(double bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        int unitIndex = 0;
        while (unitIndex < units.Length - 1 && bytes >= 1024)
        {
            bytes /= 1024.0;
            unitIndex++;
        }
        return $"{bytes:F2} {units[unitIndex]}";
    }
}
