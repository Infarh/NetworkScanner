namespace NetworkScanner;

internal sealed class Printer
{
    private static readonly Lock __Lock = new();

    public static Printer Instance { get; } = new();

    private static readonly Dictionary<int, string> __FullLines = [];

    private Printer() { }

    private int _MaxLinesCount;
    private int _CurrentLinesCount;
    private readonly Dictionary<int, int> _LineLength = [];

    private readonly int _StartLineIndex = Console.CursorTop;

    private static string GetEmptyLine(int LineLength)
    {
        if (!__FullLines.TryGetValue(LineLength, out var full_line))
            __FullLines.Add(LineLength, full_line = new(' ', LineLength));

        return full_line;
    }

    public void Clear()
    {
        lock (__Lock)
        {
            Console.SetCursorPosition(0, _StartLineIndex);
            _MaxLinesCount = 0;
        }
    }

    public int WriteLine(string str)
    {
        lock (__Lock)
        {
            var line = Console.CursorTop;

            var last_line_length = _LineLength.GetValueOrDefault(line, 0);

            Console.Write(str);
            _CurrentLinesCount++;
            _LineLength[line] = str.Length;

            if (last_line_length - str.Length is > 0 and var line_len_delta)
            {
                var empty_line = GetEmptyLine(line_len_delta);
                Console.WriteLine(empty_line);
            }
            else
                Console.WriteLine();

            return line;
        }
    }

    private void EndPrint()
    {
        lock (__Lock)
        {
            var buffer_width = Console.BufferWidth;
            var full_line = GetEmptyLine(buffer_width);

            var end_line = Console.CursorTop;

            for (var i = _CurrentLinesCount; i < _MaxLinesCount; i++)
                Console.WriteLine(full_line);

            Console.SetCursorPosition(0, end_line);

            _MaxLinesCount = _CurrentLinesCount;
        }
    }
}