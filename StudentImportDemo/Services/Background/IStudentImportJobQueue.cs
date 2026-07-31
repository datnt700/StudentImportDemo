namespace StudentImportDemo.Services.Background;

public interface IStudentImportJobQueue
{
    void Enqueue(string importId);
    ValueTask<string> DequeueAsync(CancellationToken cancellationToken);
}