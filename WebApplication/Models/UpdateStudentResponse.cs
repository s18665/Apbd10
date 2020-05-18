using System;

namespace WebApplication.Models
{
    public class UpdateStudentResponse
    {
        public string IndexNumber { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime BirthDate { get; set; }
    }
}