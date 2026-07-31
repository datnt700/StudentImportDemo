namespace StudentImportDemo.Services;

public interface IStudentImport
{
    Task ProcessImportJobAsync(string importId, CancellationToken cancellationToken = default);
}