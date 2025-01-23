using System.ComponentModel.DataAnnotations;

namespace YoklamaAlma.Models
{
    public class Attendance
    {
        public int Id { get; set; }

        [Required]
        public int StudentId { get; set; }

        [Required]
        public int CourseId { get; set; }

        [Required]
        public DateTime AttendanceDate { get; set; }

        [Required]
        public TimeSpan ClassTime { get; set; }

        public bool IsPresent { get; set; }

        // Navigation properties
        public virtual Student Student { get; set; }
        public virtual Course Course { get; set; }
    }
} 