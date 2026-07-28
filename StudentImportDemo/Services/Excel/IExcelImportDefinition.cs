using ClosedXML.Excel;

namespace StudentImportDemo.Services.Excel
{
    public interface IExcelImportDefinition<T>
    {
        string SheetName { get; }
        IReadOnlyList<string> Header { get; }
        IReadOnlyList<T> MapRows(
            int startRow,
            int endRow,
            IXLWorksheet worksheet);
    }
}
