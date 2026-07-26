namespace StudentImportDemo.Model;

public sealed record StudentExcelReadResult(
    bool IsSuccess,
    string? ErrorMessage,
    IReadOnlyList<StudentImportRow> Students);