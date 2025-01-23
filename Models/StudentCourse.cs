using System.ComponentModel.DataAnnotations;

namespace YoklamaAlma.Models
{
    public class StudentCourse
    {
        public int Id { get; set; }

        [Required]
        public int StudentId { get; set; }

        [Required]
        public int CourseId { get; set; }

        // Navigation properties
        public virtual Student Student { get; set; }
        public virtual Course Course { get; set; }
    }
} 