using StudentImportDemo.Model;

namespace StudentImportDemo.Services;

public interface IImport
{
    StudentExcelReadResult Read(Stream steam);
}