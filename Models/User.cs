using System.ComponentModel.DataAnnotations;

namespace YoklamaAlma.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Username { get; set; }

        [Required]
        [StringLength(100)]
        public string PasswordHash { get; set; }

        [Required]
        public UserRole Role { get; set; }

        // If the user is a student, link to student record
        public int? StudentId { get; set; }
        public virtual Student Student { get; set; }
    }
} 