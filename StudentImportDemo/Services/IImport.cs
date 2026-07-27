using StudentImportDemo.Model;

namespace StudentImportDemo.Services;

public interface IImport
{
    ExcelReadResult<StudentImportRow> Read(Stream steam);
}