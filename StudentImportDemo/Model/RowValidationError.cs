namespace StudentImportDemo.Model
{
    public sealed record RowValidationError(int RowNumber, string ColumnName,  string Message);
}
