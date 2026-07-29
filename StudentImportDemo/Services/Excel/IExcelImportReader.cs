using StudentImportDemo.Model;

namespace StudentImportDemo.Services.Excel;

public interface IExcelImportReader<T>
{
    Task<IEnumerable<ExcelReadResult<T>>> Read(Stream stream, IExcelImportDefinition<T> definition);
}
