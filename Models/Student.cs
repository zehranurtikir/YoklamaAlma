using System.ComponentModel.DataAnnotations;

namespace YoklamaAlma.Models
{
    public class Student
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string FullName { get; set; }

        [Required]
        [StringLength(20)]
        public string StudentNumber { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; }

        public string PhotoUrl { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        public virtual ICollection<Attendance> Attendances { get; set; }
        public virtual ICollection<StudentCourse> StudentCourses { get; set; }
    }
} 