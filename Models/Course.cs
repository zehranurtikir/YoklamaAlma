using System.ComponentModel.DataAnnotations;

namespace YoklamaAlma.Models
{
    public class Course
    {
        public int Id { get; set; }

        [Required]
        [StringLength(10)]
        public string CourseCode { get; set; }

        [Required]
        [StringLength(100)]
        public string CourseName { get; set; }

        [Required]
        public int Credits { get; set; }

        [Required]
        [StringLength(20)]
        public string Semester { get; set; }

        // Navigation properties
        public virtual ICollection<Attendance> Attendances { get; set; }
        public virtual ICollection<StudentCourse> StudentCourses { get; set; }
    }
} 