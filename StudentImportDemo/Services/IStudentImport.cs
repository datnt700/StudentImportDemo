using StudentImportDemo.Model;

namespace StudentImportDemo.Services;

public interface IStudentImport
{
    Task<ExcelReadResult<StudentImportRow>> Read(Stream stream);
}