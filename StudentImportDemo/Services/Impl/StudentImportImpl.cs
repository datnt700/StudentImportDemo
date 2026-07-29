using StudentImportDemo.Data;
using StudentImportDemo.Entity;
using StudentImportDemo.Model;
using StudentImportDemo.Services.Excel;
using Microsoft.EntityFrameworkCore;

namespace StudentImportDemo.Services.Impl;

public class StudentImportImpl : IStudentImport
{
    private readonly IExcelImportReader<StudentImportRow> _reader;
    private readonly IExcelImportDefinition<StudentImportRow> _definition;
    private readonly AppDbContext _db;

    public StudentImportImpl(
        IExcelImportReader<StudentImportRow> reader,
        IExcelImportDefinition<StudentImportRow> definition,
        AppDbContext db)
    {
        _reader = reader;
        _definition = definition;
        _db = db;
    }

    public async Task<ExcelReadResult<StudentImportRow>> Read(Stream stream)
    {
        var importedItems = new List<StudentImportRow>();
        var errors = new List<RowValidationError>();

        var batches = await _reader.Read(stream, _definition);

        foreach (var batch in batches)
        {
            if (!batch.IsSuccess)
            {
                errors.AddRange(batch.Errors);

                if (!string.IsNullOrWhiteSpace(batch.ErrorMessage))
                {
                    errors.Add(new RowValidationError(0, string.Empty, batch.ErrorMessage));
                }

                continue;
            }

            var validRows = new List<StudentImportRow>();

            foreach (var row in batch.Items)
            {
                if (string.IsNullOrWhiteSpace(row.StudentCode))
                {
                    errors.Add(new RowValidationError(row.RowNumber, nameof(row.StudentCode), "StudentCode is required."));
                    continue;
                }

                if (string.IsNullOrWhiteSpace(row.FullName))
                {
                    errors.Add(new RowValidationError(row.RowNumber, nameof(row.FullName), "FullName is required."));
                    continue;
                }

                if (string.IsNullOrWhiteSpace(row.Email))
                {
                    errors.Add(new RowValidationError(row.RowNumber, nameof(row.Email), "Email is required."));
                    continue;
                }

                if (!DateTime.TryParse(row.DateOfBirth, out _))
                {
                    errors.Add(new RowValidationError(row.RowNumber, nameof(row.DateOfBirth), "DateOfBirth is invalid."));
                    continue;
                }

                if (string.IsNullOrWhiteSpace(row.ClassCode))
                {
                    errors.Add(new RowValidationError(row.RowNumber, nameof(row.ClassCode), "ClassCode is required."));
                    continue;
                }

                validRows.Add(row);
            }

            if (validRows.Count == 0)
            {
                continue;
            }

            var uniqueRows = new List<StudentImportRow>();
            var seenCodes = new HashSet<string>();

            foreach (var row in validRows)
            {
                if (!seenCodes.Add(row.StudentCode))
                {
                    continue;
                }

                uniqueRows.Add(row);
            }

            var batchStudentCodes = uniqueRows
                .Select(row => row.StudentCode)
                .ToList();

            var existingCodes = await _db.Students
                .Where(student => batchStudentCodes.Contains(student.StudentCode))
                .Select(student => student.StudentCode)
                .ToListAsync();

            var existingCodeSet = existingCodes.ToHashSet();
            var newRows = uniqueRows
                .Where(row => !existingCodeSet.Contains(row.StudentCode))
                .ToList();

            if (newRows.Count == 0)
            {
                continue;
            }

            var students = newRows
                .Select(row => new Student
                {
                    StudentCode = row.StudentCode,
                    FullName = row.FullName,
                    Email = row.Email,
                    DateOfBirth = DateTime.Parse(row.DateOfBirth),
                    ClassCode = row.ClassCode
                })
                .ToList();

            _db.Students.AddRange(students);
            await _db.SaveChangesAsync();

            importedItems.AddRange(newRows);
        }

        return new ExcelReadResult<StudentImportRow>(
            true,
            errors.Count == 0 ? null : "Import completed with some row errors.",
            importedItems,
            errors);
    }
}
