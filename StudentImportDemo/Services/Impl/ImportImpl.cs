using StudentImportDemo.Model;
using StudentImportDemo.Services.Excel;

namespace StudentImportDemo.Services.Impl;

public class ImportImpl: IImport
{
    private readonly IExcelImportReader<StudentImportRow> _reader;
    private readonly IExcelImportDefinition<StudentImportRow> _definition;

    public ImportImpl(
        IExcelImportReader<StudentImportRow> reader,
        IExcelImportDefinition<StudentImportRow> definition)
    {
        _reader = reader;
        _definition = definition;
    }

    public ExcelReadResult<StudentImportRow> Read(Stream stream)
    {
        return _reader.Read(stream, _definition);
    }
}
