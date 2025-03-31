using System.Collections.Concurrent;

namespace NetworkScanner;

internal static class Line
{
    private const char __EmptyChar = ' ';

    private static readonly ConcurrentDictionary<int, string> __FullLines = [];

    public static string Empty(int length) => __FullLines.GetOrAdd(length, CreateEmptyLine);

    private static string CreateEmptyLine(int length) => new(__EmptyChar, length);
}
