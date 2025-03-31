namespace NetworkScanner;

internal sealed class Printer
{
    private static readonly Lock __Lock = new();

    public static Printer Instance { get; } = new();

    private Printer() { }

    private int _MaxLinesCount;
    private int _CurrentLinesCount;
    private readonly Dictionary<int, int> _LineLength = [];

    private readonly int _StartLineIndex = Console.CursorTop;

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
                var empty_line = Line.Empty(line_len_delta);
                Console.WriteLine(empty_line);
            }
            else
                Console.WriteLine();

            return line;
        }
    }

    public void EndPrint()
    {
        lock (__Lock)
        {
            var buffer_width = Console.BufferWidth;
            var full_line = Line.Empty(buffer_width);

            var end_line = Console.CursorTop;

            for (var i = _CurrentLinesCount; i < _MaxLinesCount; i++)
                Console.WriteLine(full_line);

            Console.SetCursorPosition(0, end_line);

            _MaxLinesCount = _CurrentLinesCount;
        }
    }
}