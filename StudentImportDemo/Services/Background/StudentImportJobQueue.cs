using System.Threading.Channels;

namespace StudentImportDemo.Services.Background;

public class StudentImportJobQueue : IStudentImportJobQueue
{
    private readonly Channel<string> _queue = Channel.CreateUnbounded<string>();

    public void Enqueue(string importId)
    {
        if (string.IsNullOrWhiteSpace(importId))
        {
            return;
        }

        _queue.Writer.TryWrite(importId);
    }

    public ValueTask<string> DequeueAsync(CancellationToken cancellationToken)
    {
        return _queue.Reader.ReadAsync(cancellationToken);
    }
}