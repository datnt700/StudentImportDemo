namespace StudentImportDemo.Model;

public sealed record ExcelReadResult<T>(
    bool IsSuccess,
    string? ErrorMessage,
    IReadOnlyList<T> Items, List<RowValidationError> Errors
    );
