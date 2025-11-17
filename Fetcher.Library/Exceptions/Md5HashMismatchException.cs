using System;

namespace Fetcher.Library.Exceptions;

public class Md5HashMismatchException(
    object? md5HashA = null,
    object? md5HashB = null,
    string? message = null, 
    Exception? innerException = null
    ) : Exception(message, innerException)
{
    public string? Md5HashA = $"{md5HashA}";
    public string? Md5HashB = $"{md5HashB}";
}