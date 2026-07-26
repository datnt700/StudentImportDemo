using ClosedXML.Excel;
using StudentImportDemo.Model;

namespace StudentImportDemo.Services.Impl;

public class ImportImpl: IImport
{
    public StudentExcelReadResult Read(Stream stream)
    {
   
        using var workbook = new XLWorkbook(stream);
        var firstSheet = workbook.Worksheets.First();
        if (!string.Equals(firstSheet.Name, "Students", StringComparison.OrdinalIgnoreCase))
        {
          
            return new StudentExcelReadResult(
                false,
                "Sheet name is incorrect.",
                Array.Empty<StudentImportRow>());
        }

        var headerExpected = new[] { "StudentCode", "FullName", "Email", "DateOfBirth", "ClassCode" };
        for (var i = 0; i < headerExpected.Length; i++)
        {
            string headerActual =  firstSheet.Cell(1,i+1).Value.ToString();
            if (!string.Equals(headerActual, headerExpected[i], StringComparison.OrdinalIgnoreCase))
            {
                return new StudentExcelReadResult(
                    false,
                    "Header is incorrect.",
                    Array.Empty<StudentImportRow>());
            }
        }

        var lastRow = firstSheet.LastRowUsed();
        if (lastRow == null || lastRow.RowNumber() == 1)
        {
            return new StudentExcelReadResult(
                false,
                "Data is empty.",
                Array.Empty<StudentImportRow>());
        }
        List<StudentImportRow> students = new List<StudentImportRow>();
        for (var rowNumber = 2; rowNumber <= lastRow.RowNumber(); rowNumber++)
        {
            var studentCode = firstSheet.Cell(rowNumber, 1).Value.ToString().Trim();
            var fullName = firstSheet.Cell(rowNumber, 2).Value.ToString().Trim();
            var email = firstSheet.Cell(rowNumber, 3).Value.ToString().Trim();
            var dateOfBirth = firstSheet.Cell(rowNumber, 4).Value.ToString().Trim();
            var classCode = firstSheet.Cell(rowNumber, 5).Value.ToString().Trim();

            students.Add(new StudentImportRow(
                rowNumber,
                studentCode,
                fullName,
                email,
                dateOfBirth,
                classCode
            ));
        }

        return new StudentExcelReadResult(
            true,
            "File Import Successful",
            students
        );
    }
}