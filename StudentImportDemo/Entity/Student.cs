using System.ComponentModel.DataAnnotations;

namespace StudentImportDemo.Entity
{
    public class Student
    {
        [Key]
        public string StudentCode { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string ClassCode { get; set; }

    }
}
