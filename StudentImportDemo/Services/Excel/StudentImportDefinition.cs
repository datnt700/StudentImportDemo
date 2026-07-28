using ClosedXML.Excel;
using StudentImportDemo.Model;

namespace StudentImportDemo.Services.Excel
{
    public class StudentImportDefinition : IExcelImportDefinition<StudentImportRow>
    {
        public string SheetName => "Students";
        public IReadOnlyList<string> Header => new List<string>
        {
            "StudentCode",
            "FullName",
            "Email",
            "DateOfBirth",
            "ClassCode"
        };
        public StudentImportRow MapRow(int rowNumber, ClosedXML.Excel.IXLWorksheet worksheet)
        {
            var studentCode = worksheet.Cell(rowNumber, 1).Value.ToString().Trim();
            var fullName = worksheet.Cell(rowNumber, 2).Value.ToString().Trim();
            var email = worksheet.Cell(rowNumber, 3).Value.ToString().Trim();
            var dateOfBirth = worksheet.Cell(rowNumber, 4).Value.ToString().Trim();
            var classCode = worksheet.Cell(rowNumber, 5).Value.ToString().Trim();
            return new StudentImportRow(
                rowNumber,
                studentCode,
                fullName,
                email,
                dateOfBirth,
                classCode
            );
        }
        
        public IReadOnlyList<StudentImportRow> MapRows(
            int startRow,
            int endRow,
            IXLWorksheet worksheet)
        {
            var rows = new List<StudentImportRow>();

            for (var rowNumber = startRow;
                 rowNumber <= endRow;
                 rowNumber++)
            {
                rows.Add(MapRow(rowNumber, worksheet));
            }

            return rows;
        }
    }
}
