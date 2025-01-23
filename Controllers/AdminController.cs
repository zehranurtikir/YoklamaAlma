using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using YoklamaAlma.Data;
using YoklamaAlma.Models;
using System.IO;
using BCrypt.Net;
using Microsoft.EntityFrameworkCore;

namespace YoklamaAlma.Controllers
{
    [Route("Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public AdminController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        [Route("")]
        [Route("Dashboard")]
        public async Task<IActionResult> Dashboard()
        {
            // Get total number of students from database
            ViewBag.TotalStudents = await _context.Students.CountAsync();

            // Get total number of courses from database
            ViewBag.TotalCourses = await _context.Courses.CountAsync();

            // Get number of courses with low attendance (less than 70%)
            var coursesWithLowAttendance = await _context.Courses
                .Where(c => c.Attendances.Any())
                .Select(c => new
                {
                    CourseId = c.Id,
                    AttendanceRate = (double)c.Attendances.Count(a => a.IsPresent) / c.Attendances.Count() * 100
                })
                .Where(c => c.AttendanceRate < 70)
                .CountAsync();

            ViewBag.LowAttendanceCourses = coursesWithLowAttendance;

            // Get number of new students (added in the last 7 days)
            var lastWeek = DateTime.Now.AddDays(-7);
            ViewBag.NewStudents = await _context.Students
                .Where(s => s.CreatedAt >= lastWeek)
                .CountAsync();
            
            return View();
        }

        [Route("Students")]
        public IActionResult Students()
        {
            var students = _context.Students.ToList();
            return View(students);
        }

        [Route("AddStudent")]
        public IActionResult AddStudent()
        {
            return View("~/Views/Admin/AddStudent.cshtml");
        }

        [HttpPost]
        [Route("AddStudent")]
        public async Task<IActionResult> AddStudent(string userType, string fullName, string studentNumber, string email, 
            string adminUsername, string password, string status, IFormFile photo)
        {
            try
            {
                if (userType == "Admin")
                {
                    if (string.IsNullOrEmpty(adminUsername) || string.IsNullOrEmpty(password))
                    {
                        TempData["Error"] = "Lütfen tüm zorunlu alanları doldurun.";
                        return View();
                    }

                    // Check if admin username already exists
                    if (_context.Users.Any(u => u.Username == adminUsername))
                    {
                        TempData["Error"] = "Bu kullanıcı adı zaten kayıtlı.";
                        return View();
                    }

                    var adminUser = new User
                    {
                        Username = adminUsername,
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                        Role = UserRole.Admin
                    };

                    _context.Users.Add(adminUser);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Admin kullanıcısı başarıyla eklendi.";
                    return RedirectToAction("Students");
                }
                else // Student
                {
                    if (string.IsNullOrEmpty(fullName) || string.IsNullOrEmpty(studentNumber) || 
                        string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                    {
                        TempData["Error"] = "Lütfen tüm zorunlu alanları doldurun.";
                        return View();
                    }

                    // Check if student number already exists
                    if (_context.Students.Any(s => s.StudentNumber == studentNumber))
                    {
                        TempData["Error"] = "Bu öğrenci numarası zaten kayıtlı.";
                        return View();
                    }

                    var student = new Student
                    {
                        FullName = fullName,
                        StudentNumber = studentNumber,
                        Email = email,
                        IsActive = status == "active",
                        CreatedAt = DateTime.Now
                    };

                    // Handle photo upload
                    if (photo != null && photo.Length > 0)
                    {
                        var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "students");
                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        var uniqueFileName = $"{Guid.NewGuid()}_{photo.FileName}";
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await photo.CopyToAsync(fileStream);
                        }

                        student.PhotoUrl = $"/uploads/students/{uniqueFileName}";
                    }
                    else
                    {
                        student.PhotoUrl = "/images/default-avatar.png";
                    }

                    _context.Students.Add(student);
                    await _context.SaveChangesAsync();

                    // Create user account for the student
                    var user = new User
                    {
                        Username = studentNumber,
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                        Role = UserRole.Student,
                        StudentId = student.Id
                    };

                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Öğrenci başarıyla eklendi.";
                    return RedirectToAction("Students");
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Bir hata oluştu. Lütfen tekrar deneyin.";
                return View();
            }
        }

        [Route("Courses")]
        public async Task<IActionResult> Courses()
        {
            var courses = await _context.Courses.ToListAsync();
            return View(courses);
        }

        [Route("AddCourse")]
        public IActionResult AddCourse()
        {
            return View();
        }

        [HttpPost]
        [Route("AddCourse")]
        public async Task<IActionResult> AddCourse(string courseCode, string courseName, int credits, string semester)
        {
            if (string.IsNullOrEmpty(courseCode) || string.IsNullOrEmpty(courseName) || 
                credits <= 0 || string.IsNullOrEmpty(semester))
            {
                TempData["Error"] = "Lütfen tüm alanları doldurun.";
                return View();
            }

            // Check if course code already exists
            if (await _context.Courses.AnyAsync(c => c.CourseCode == courseCode))
            {
                TempData["Error"] = "Bu ders kodu zaten kullanılıyor.";
                return View();
            }

            var course = new Course
            {
                CourseCode = courseCode,
                CourseName = courseName,
                Credits = credits,
                Semester = semester
            };

            try
            {
                _context.Courses.Add(course);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Ders başarıyla eklendi.";
                return RedirectToAction("Courses");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Ders eklenirken bir hata oluştu.";
                return View();
            }
        }

        [HttpPost]
        [Route("DeleteCourse/{id}")]
        public async Task<IActionResult> DeleteCourse(int id)
        {
            var course = await _context.Courses.FindAsync(id);
            if (course == null)
            {
                TempData["Error"] = "Ders bulunamadı.";
                return RedirectToAction("Courses");
            }

            try
            {
                // Check if there are any attendances for this course
                var hasAttendances = await _context.Attendances.AnyAsync(a => a.CourseId == id);
                if (hasAttendances)
                {
                    TempData["Error"] = "Bu derse ait yoklama kayıtları olduğu için silinemez.";
                    return RedirectToAction("Courses");
                }

                // Remove student course relationships
                var studentCourses = await _context.StudentCourses.Where(sc => sc.CourseId == id).ToListAsync();
                _context.StudentCourses.RemoveRange(studentCourses);

                // Remove the course
                _context.Courses.Remove(course);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Ders başarıyla silindi.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Ders silinirken bir hata oluştu.";
            }

            return RedirectToAction("Courses");
        }

        [HttpGet]
        [Route("EditCourse/{id}")]
        public async Task<IActionResult> EditCourse(int id)
        {
            var course = await _context.Courses.FindAsync(id);
            if (course == null)
            {
                TempData["Error"] = "Ders bulunamadı.";
                return RedirectToAction("Courses");
            }

            return View(course);
        }

        [HttpPost]
        [Route("EditCourse/{id}")]
        public async Task<IActionResult> EditCourse(int id, string courseCode, string courseName, int credits, string semester)
        {
            var course = await _context.Courses.FindAsync(id);
            if (course == null)
            {
                TempData["Error"] = "Ders bulunamadı.";
                return RedirectToAction("Courses");
            }

            if (string.IsNullOrEmpty(courseCode) || string.IsNullOrEmpty(courseName) || 
                credits <= 0 || string.IsNullOrEmpty(semester))
            {
                TempData["Error"] = "Lütfen tüm alanları doldurun.";
                return View(course);
            }

            // Check if course code is changed and already exists
            if (courseCode != course.CourseCode && 
                await _context.Courses.AnyAsync(c => c.CourseCode == courseCode))
            {
                TempData["Error"] = "Bu ders kodu zaten kullanılıyor.";
                return View(course);
            }

            try
            {
                course.CourseCode = courseCode;
                course.CourseName = courseName;
                course.Credits = credits;
                course.Semester = semester;

                await _context.SaveChangesAsync();
                TempData["Success"] = "Ders başarıyla güncellendi.";
                return RedirectToAction("Courses");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Ders güncellenirken bir hata oluştu.";
                return View(course);
            }
        }

        [HttpGet]
        [Route("AddAttendance")]
        public async Task<IActionResult> AddAttendance()
        {
            ViewData["Title"] = "Yoklama Ekle";
            // Fetch all courses from the database
            var courses = await _context.Courses.ToListAsync();
            return View(courses);
        }

        [HttpPost]
        [Route("AddAttendance")]
        public async Task<IActionResult> AddAttendance(int courseId, DateTime classTime)
        {
            try
            {
                // Validate course exists
                var course = await _context.Courses.FindAsync(courseId);
                if (course == null)
                {
                    TempData["Error"] = "Seçilen ders bulunamadı.";
                    return RedirectToAction("AddAttendance");
                }

                // Check if attendance record already exists for this course and time
                var existingAttendance = await _context.Attendances
                    .FirstOrDefaultAsync(a => a.CourseId == courseId && a.AttendanceDate.Date == classTime.Date);

                if (existingAttendance != null)
                {
                    TempData["Error"] = "Bu ders için bugün zaten yoklama alınmış.";
                    return RedirectToAction("AddAttendance");
                }

                // Create a single attendance record for the course
                var attendance = new Attendance
                {
                    CourseId = courseId,
                    AttendanceDate = classTime,
                    IsPresent = false // Default to false
                };

                await _context.Attendances.AddAsync(attendance);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Yoklama başarıyla oluşturuldu.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Yoklama oluşturulurken bir hata oluştu.";
            }

            return RedirectToAction("AddAttendance");
        }

        [HttpPost]
        [Route("DeleteStudent/{id}")]
        public async Task<IActionResult> DeleteStudent(int id)
        {
            var student = await _context.Students.FindAsync(id);
            if (student == null)
            {
                TempData["Error"] = "Öğrenci bulunamadı.";
                return RedirectToAction("Students");
            }

            // Find and delete associated user account
            var user = await _context.Users.FirstOrDefaultAsync(u => u.StudentId == id);
            if (user != null)
            {
                _context.Users.Remove(user);
            }

            // Delete student's photo if it exists and is not the default avatar
            if (!string.IsNullOrEmpty(student.PhotoUrl) && student.PhotoUrl != "/images/default-avatar.png")
            {
                var photoPath = Path.Combine(_webHostEnvironment.WebRootPath, student.PhotoUrl.TrimStart('/'));
                if (System.IO.File.Exists(photoPath))
                {
                    System.IO.File.Delete(photoPath);
                }
            }

            _context.Students.Remove(student);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Öğrenci başarıyla silindi.";
            return RedirectToAction("Students");
        }

        [HttpPost]
        [Route("UpdateStudentStatus/{id}")]
        public async Task<IActionResult> UpdateStudentStatus(int id, bool isActive)
        {
            var student = await _context.Students.FindAsync(id);
            if (student == null)
            {
                TempData["Error"] = "Öğrenci bulunamadı.";
                return RedirectToAction("Students");
            }

            student.IsActive = isActive;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Öğrenci durumu {(isActive ? "aktif" : "pasif")} olarak güncellendi.";
            return RedirectToAction("Students");
        }

        [HttpGet]
        [Route("EditStudent/{id}")]
        public async Task<IActionResult> EditStudent(int id)
        {
            var student = await _context.Students.FindAsync(id);
            if (student == null)
            {
                TempData["Error"] = "Öğrenci bulunamadı.";
                return RedirectToAction("Students");
            }

            return View(student);
        }

        [HttpPost]
        [Route("EditStudent/{id}")]
        public async Task<IActionResult> EditStudent(int id, string fullName, string studentNumber, string email, string password, string status, IFormFile photo)
        {
            var student = await _context.Students.FindAsync(id);
            if (student == null)
            {
                TempData["Error"] = "Öğrenci bulunamadı.";
                return RedirectToAction("Students");
            }

            // Check if student number is changed and already exists
            if (student.StudentNumber != studentNumber && _context.Students.Any(s => s.StudentNumber == studentNumber))
            {
                TempData["Error"] = "Bu öğrenci numarası zaten kayıtlı.";
                return View(student);
            }

            // Find associated user
            var user = await _context.Users.FirstOrDefaultAsync(u => u.StudentId == id);
            if (user == null)
            {
                TempData["Error"] = "Öğrenci hesabı bulunamadı.";
                return RedirectToAction("Students");
            }

            // Update student information
            student.FullName = fullName;
            student.StudentNumber = studentNumber;
            student.Email = email;
            student.IsActive = status == "active";

            // Update user information
            user.Username = studentNumber; // Update username to match new student number

            // Handle photo upload if new photo is provided
            if (photo != null && photo.Length > 0)
            {
                // Delete old photo if it exists and is not the default avatar
                if (!string.IsNullOrEmpty(student.PhotoUrl) && student.PhotoUrl != "/images/default-avatar.png")
                {
                    var oldPhotoPath = Path.Combine(_webHostEnvironment.WebRootPath, student.PhotoUrl.TrimStart('/'));
                    if (System.IO.File.Exists(oldPhotoPath))
                    {
                        System.IO.File.Delete(oldPhotoPath);
                    }
                }

                // Save new photo
                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "students");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var uniqueFileName = $"{Guid.NewGuid()}_{photo.FileName}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await photo.CopyToAsync(fileStream);
                }

                student.PhotoUrl = $"/uploads/students/{uniqueFileName}";
            }

            // Update password if provided
            if (!string.IsNullOrEmpty(password))
            {
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Öğrenci bilgileri başarıyla güncellendi.";
            return RedirectToAction("Students");
        }
    }
} 