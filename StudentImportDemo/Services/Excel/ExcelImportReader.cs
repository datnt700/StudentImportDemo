using ClosedXML.Excel;
using StudentImportDemo.Model;

namespace StudentImportDemo.Services.Excel;

public class ExcelImportReader<T> : IExcelImportReader<T>
{
    public ExcelReadResult<T> Read(Stream stream, IExcelImportDefinition<T> definition)
    {
        using var workbook = new XLWorkbook(stream);
        var firstSheet = workbook.Worksheets.First();

        if (!string.Equals(firstSheet.Name, definition.SheetName, StringComparison.OrdinalIgnoreCase))
        {
            return new ExcelReadResult<T>(
                false,
                "Sheet name is incorrect.",
                Array.Empty<T>());
        }

        var headerExpected = definition.Header;
        for (var i = 0; i < headerExpected.Count; i++)
        {
            var headerActual = firstSheet.Cell(1, i + 1).Value.ToString();
            if (!string.Equals(headerActual, headerExpected[i], StringComparison.OrdinalIgnoreCase))
            {
                return new ExcelReadResult<T>(
                    false,
                    "Header is incorrect.",
                    Array.Empty<T>());
            }
        }

        var lastRow = firstSheet.LastRowUsed();
        if (lastRow == null || lastRow.RowNumber() == 1)
        {
            return new ExcelReadResult<T>(
                false,
                "Data is empty.",
                Array.Empty<T>());
        }
        int lastRowNumber = firstSheet.LastRowUsed().RowNumber();
        int mapSize = 100;
        int startRow = 2;
        List<T> items = [];
        while (startRow <= lastRowNumber)
        {
            int endRow = Math.Min(startRow + mapSize - 1, lastRowNumber);
            var batch = definition.MapRows(
                startRow,
                endRow,
                firstSheet);
            startRow = endRow + 1;
            
            items.AddRange(batch);
        }
        return new ExcelReadResult<T>(
            true,
            "File Import Successful",
            items);
    }
}
