using ClosedXML.Excel;

namespace StudentImportDemo.Services.Excel
{
    public interface IExcelImportDefinition<T>
    {
        string SheetName { get; }
        IReadOnlyList<string> Header { get; }
        T MapRow(int rowNumber, IXLWorksheet worksheet);
    }
}
