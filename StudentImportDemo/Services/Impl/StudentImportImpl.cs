using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using StudentImportDemo.Data;
using StudentImportDemo.Entity;
using StudentImportDemo.Model;
using StudentImportDemo.Services.Excel;

namespace StudentImportDemo.Services.Impl;

public class StudentImportImpl : IStudentImport
{
    private readonly IExcelImportReader<StudentImportRow> _reader;
    private readonly IExcelImportDefinition<StudentImportRow> _definition;
    private readonly AppDbContext _db;
    private readonly IHubContext<ImportHub> _hub;
    private readonly IWebHostEnvironment _environment;

    public StudentImportImpl(
        IExcelImportReader<StudentImportRow> reader,
        IExcelImportDefinition<StudentImportRow> definition,
        AppDbContext db,
        IHubContext<ImportHub> hub,
        IWebHostEnvironment environment)
    {
        _reader = reader;
        _definition = definition;
        _db = db;
        _hub = hub;
        _environment = environment;
    }

    public async Task ProcessImportJobAsync(string importId, CancellationToken cancellationToken = default)
    {
        var job = await _db.ImportJobs.FirstOrDefaultAsync(x => x.Id == importId, cancellationToken);
        if (job == null || job.Status != "Pending")
        {
            return;
        }

        job.Status = "Processing";
        job.StartedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        var importedItems = new List<StudentImportRow>();
        var failedRecords = 0;
        var storedFilePath = ResolveStoredFilePath(job.StoredFilePath);

        try
        {
            await using var stream = File.OpenRead(storedFilePath);
            var batches = await _reader.Read(stream, _definition);

            foreach (var batch in batches)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!batch.IsSuccess)
                {
                    foreach (var error in batch.Errors)
                    {
                        failedRecords++;
                        await SaveAndSendRow(importId, error.RowNumber, string.Empty, string.Empty, "Failed", error.Message, cancellationToken);
                    }

                    if (!string.IsNullOrWhiteSpace(batch.ErrorMessage))
                    {
                        failedRecords++;
                        await SaveAndSendRow(importId, 0, string.Empty, string.Empty, "Failed", batch.ErrorMessage, cancellationToken);
                    }

                    continue;
                }

                var validRows = new List<StudentImportRow>();

                foreach (var row in batch.Items)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    await SendRowProcessing(importId, row.RowNumber, row.StudentCode, row.FullName, cancellationToken);

                    var rowErrors = ValidateRow(row);
                    if (rowErrors.Count > 0)
                    {
                        failedRecords++;
                        var message = string.Join(" ", rowErrors.Select(error => error.Message));
                        await SaveAndSendRow(importId, row.RowNumber, row.StudentCode, row.FullName, "Failed", message, cancellationToken);
                        continue;
                    }

                    validRows.Add(row);
                }

                if (validRows.Count == 0)
                {
                    continue;
                }

                var uniqueRows = new List<StudentImportRow>();
                var seenCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var row in validRows)
                {
                    if (!seenCodes.Add(row.StudentCode))
                    {
                        failedRecords++;
                        var message = $"StudentCode '{row.StudentCode}' is duplicated in the file.";
                        await SaveAndSendRow(importId, row.RowNumber, row.StudentCode, row.FullName, "Failed", message, cancellationToken);
                        continue;
                    }

                    uniqueRows.Add(row);
                }

                if (uniqueRows.Count == 0)
                {
                    continue;
                }

                var batchStudentCodes = uniqueRows.Select(row => row.StudentCode).ToList();
                var existingCodes = await _db.Students
                    .Where(student => batchStudentCodes.Contains(student.StudentCode))
                    .Select(student => student.StudentCode)
                    .ToListAsync(cancellationToken);

                var existingCodeSet = existingCodes.ToHashSet(StringComparer.OrdinalIgnoreCase);

                foreach (var row in uniqueRows.Where(row => existingCodeSet.Contains(row.StudentCode)))
                {
                    failedRecords++;
                    var message = $"StudentCode '{row.StudentCode}' is duplicated in the database. Row skipped.";
                    await SaveAndSendRow(importId, row.RowNumber, row.StudentCode, row.FullName, "Failed", message, cancellationToken);
                }

                var newRows = uniqueRows.Where(row => !existingCodeSet.Contains(row.StudentCode)).ToList();
                if (newRows.Count == 0)
                {
                    continue;
                }

                var students = newRows.Select(row => new Student
                {
                    StudentCode = row.StudentCode,
                    FullName = row.FullName,
                    Email = row.Email,
                    DateOfBirth = DateTime.Parse(row.DateOfBirth),
                    ClassCode = row.ClassCode
                }).ToList();

                try
                {
                    _db.Students.AddRange(students);
                    await _db.SaveChangesAsync(cancellationToken);

                    importedItems.AddRange(newRows);

                    foreach (var row in newRows)
                    {
                        await SaveAndSendRow(importId, row.RowNumber, row.StudentCode, row.FullName, "Success", "Imported successfully.", cancellationToken);
                    }
                }
                catch (DbUpdateException)
                {
                    failedRecords += newRows.Count;
                    _db.ChangeTracker.Clear();

                    var startRow = newRows.Min(row => row.RowNumber);
                    var endRow = newRows.Max(row => row.RowNumber);
                    var message = $"Rows {startRow}-{endRow} could not be saved. No rows in this batch were imported. The data may conflict with existing database constraints.";

                    foreach (var row in newRows)
                    {
                        await SaveAndSendRow(importId, row.RowNumber, row.StudentCode, row.FullName, "Failed", message, cancellationToken);
                    }
                }
            }

            job = await _db.ImportJobs.FirstAsync(x => x.Id == importId, cancellationToken);
            job.Status = "Completed";
            job.SuccessCount = importedItems.Count;
            job.FailedCount = failedRecords;
            job.CompletedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);

            await _hub.Clients.Group(importId).SendAsync("importSummary", new
            {
                success = job.SuccessCount,
                failed = job.FailedCount,
                message = "Import completed."
            }, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _db.ChangeTracker.Clear();

            job = await _db.ImportJobs.FirstAsync(x => x.Id == importId, cancellationToken);
            job.Status = "Failed";
            job.CompletedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);

            await _hub.Clients.Group(importId).SendAsync("importSummary", new
            {
                success = job.SuccessCount,
                failed = job.FailedCount,
                message = "Import failed. " + ex.Message
            }, cancellationToken);
        }
    }

    private List<RowValidationError> ValidateRow(StudentImportRow row)
    {
        var rowErrors = new List<RowValidationError>();

        if (string.IsNullOrWhiteSpace(row.StudentCode))
        {
            rowErrors.Add(new RowValidationError(row.RowNumber, nameof(row.StudentCode), "StudentCode is required."));
        }

        if (string.IsNullOrWhiteSpace(row.FullName))
        {
            rowErrors.Add(new RowValidationError(row.RowNumber, nameof(row.FullName), "FullName is required."));
        }

        if (string.IsNullOrWhiteSpace(row.Email))
        {
            rowErrors.Add(new RowValidationError(row.RowNumber, nameof(row.Email), "Email is required."));
        }

        if (!DateTime.TryParse(row.DateOfBirth, out _))
        {
            rowErrors.Add(new RowValidationError(row.RowNumber, nameof(row.DateOfBirth), "DateOfBirth is invalid."));
        }

        if (string.IsNullOrWhiteSpace(row.ClassCode))
        {
            rowErrors.Add(new RowValidationError(row.RowNumber, nameof(row.ClassCode), "ClassCode is required."));
        }

        return rowErrors;
    }

    private async Task SaveAndSendRow(
        string importId,
        int rowNumber,
        string? studentCode,
        string? fullName,
        string status,
        string message,
        CancellationToken cancellationToken)
    {
        var result = new ImportRowResult
        {
            ImportJobId = importId,
            RowNumber = rowNumber,
            StudentCode = studentCode,
            FullName = fullName,
            Status = status,
            Message = message
        };

        var job = await _db.ImportJobs.FirstAsync(x => x.Id == importId, cancellationToken);
        if (status == "Success")
        {
            job.SuccessCount++;
        }
        else if (status == "Failed")
        {
            job.FailedCount++;
        }

        _db.ImportRowResults.Add(result);
        await _db.SaveChangesAsync(cancellationToken);

        await _hub.Clients.Group(importId).SendAsync("rowProcessed", new
        {
            rowNumber,
            studentCode,
            fullName,
            status,
            message
        }, cancellationToken);

        await Task.Delay(350, cancellationToken);
    }


    private async Task SendRowProcessing(
        string importId,
        int rowNumber,
        string? studentCode,
        string? fullName,
        CancellationToken cancellationToken)
    {
        await _hub.Clients.Group(importId).SendAsync("rowProcessed", new
        {
            rowNumber,
            studentCode,
            fullName,
            status = "Processing",
            message = "Processing row..."
        }, cancellationToken);

        await Task.Delay(200, cancellationToken);
    }
    private string ResolveStoredFilePath(string storedFilePath)
    {
        if (Path.IsPathRooted(storedFilePath))
        {
            return storedFilePath;
        }

        return Path.Combine(_environment.ContentRootPath, storedFilePath);
    }
}