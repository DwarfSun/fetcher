using System;

namespace Fetcher.Library.Utilities;

public static class Compare
{
    public static bool ByteArraysEqual(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        return a.SequenceEqual(b);
    }
}
