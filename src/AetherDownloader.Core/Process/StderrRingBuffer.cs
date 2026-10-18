namespace AetherDownloader.Core.Process;

internal sealed class StderrRingBuffer
{
    private const int Capacity = 500;
    private readonly Queue<string> _lines = new(Capacity);
    private readonly object _lock = new();

    public void Add(string line)
    {
        lock (_lock)
        {
            if (_lines.Count >= Capacity)
            {
                _lines.Dequeue();
            }

            _lines.Enqueue(line);
        }
    }

    public IReadOnlyList<string> Snapshot()
    {
        lock (_lock)
        {
            return _lines.ToArray();
        }
    }
}
