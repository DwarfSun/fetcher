using System;

namespace Fetcher.Library.Exceptions;

public class FileAlreadyDownloadedException(
    object? uri = null,
    object? filename = null,
    string? message = null, 
    Exception? innerException = null
    ) : Exception(message, innerException)
{
    public string? Uri = $"{uri}";
    public string? LocalFile = $"{filename}";
}
