namespace StudentImportDemo.Entity
{
    public class ImportRowResult
    {
        public int Id { get; set; }
        public string ImportJobId { get; set; }
        public int RowNumber { get; set; }
        public string? StudentCode { get; set; }
        public string? FullName { get; set; }
        public string Status { get; set; } 
        public string Message { get; set; }

        public ImportJob ImportJob { get; set; }
    }
}
