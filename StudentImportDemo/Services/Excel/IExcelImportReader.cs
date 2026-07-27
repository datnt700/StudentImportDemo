using StudentImportDemo.Model;

namespace StudentImportDemo.Services.Excel;

public interface IExcelImportReader<T>
{
    ExcelReadResult<T> Read(Stream stream, IExcelImportDefinition<T> definition);
}
