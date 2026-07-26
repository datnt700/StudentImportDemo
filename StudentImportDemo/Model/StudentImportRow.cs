namespace StudentImportDemo.Model;

public sealed record StudentImportRow(
    int RowNumber,
    string StudentCode,
    string FullName,
    string Email,
    string DateOfBirth,
    string ClassCode);