namespace StudentImportDemo.Entity
{
    public class ImportJob
    {
        public string Id { get; set; }
        public string FileName { get; set; }
        public string StoredFilePath { get; set; }
        public string Status { get; set; } // Pending, Processing, Completed, Failed
        public int SuccessCount { get; set; }
        public int FailedCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        public List<ImportRowResult> RowResults { get; set; } = new();
    }
}