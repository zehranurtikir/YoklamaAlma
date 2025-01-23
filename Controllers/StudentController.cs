using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using YoklamaAlma.Data;
using YoklamaAlma.Models;
using System.Security.Claims;

namespace YoklamaAlma.Controllers
{
    [Authorize(Roles = "Student")]
    public class StudentController : Controller
    {
        private readonly ApplicationDbContext _context;

        public StudentController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Dashboard()
        {
            ViewData["Title"] = "Ana Sayfa";

            var studentIdClaim = User.FindFirst("StudentId")?.Value;
            if (studentIdClaim == null || !int.TryParse(studentIdClaim, out int studentId))
            {
                return RedirectToAction("Login", "Account");
            }

            // Get student info
            var student = await _context.Students
                .Include(s => s.StudentCourses)
                .ThenInclude(sc => sc.Course)
                .FirstOrDefaultAsync(s => s.Id == studentId);

            if (student == null)
            {
                return RedirectToAction("Login", "Account");
            }

            ViewBag.StudentFullName = student.FullName;

            // Calculate statistics
            ViewBag.ActiveCourses = student.StudentCourses.Count;
            
            // Calculate average attendance
            var attendances = await _context.Attendances
                .Where(a => a.StudentId == studentId)
                .ToListAsync();

            ViewBag.AverageAttendance = attendances.Any() 
                ? (int)((double)attendances.Count(a => a.IsPresent) / attendances.Count * 100)
                : 0;

            // Get upcoming classes (next 7 days)
            var upcomingClasses = await _context.StudentCourses
                .Where(sc => sc.StudentId == studentId)
                .Include(sc => sc.Course)
                .Take(5)
                .ToListAsync();

            ViewBag.UpcomingClasses = upcomingClasses;

            return View();
        }

        public async Task<IActionResult> Courses()
        {
            ViewData["Title"] = "Derslerim";
            
            var studentIdClaim = User.FindFirst("StudentId")?.Value;
            if (studentIdClaim == null || !int.TryParse(studentIdClaim, out int studentId))
            {
                return RedirectToAction("Login", "Account");
            }

            var courses = await _context.StudentCourses
                .Where(sc => sc.StudentId == studentId)
                .Include(sc => sc.Course)
                .Select(sc => new CourseViewModel
                {
                    CourseId = sc.CourseId,
                    CourseName = sc.Course.CourseName,
                    CourseCode = sc.Course.CourseCode,
                    AttendancePercentage = _context.Attendances
                        .Count(a => a.StudentId == studentId && a.CourseId == sc.CourseId && a.IsPresent) * 100.0 /
                        Math.Max(1, _context.Attendances.Count(a => a.StudentId == studentId && a.CourseId == sc.CourseId))
                })
                .ToListAsync();

            return View(courses);
        }

        public async Task<IActionResult> Attendance()
        {
            ViewData["Title"] = "Yoklama";
            
            var studentIdClaim = User.FindFirst("StudentId")?.Value;
            if (studentIdClaim == null || !int.TryParse(studentIdClaim, out int studentId))
            {
                return RedirectToAction("Login", "Account");
            }

            // Get today's classes
            var todayClasses = await _context.StudentCourses
                .Where(sc => sc.StudentId == studentId)
                .Include(sc => sc.Course)
                .ToListAsync();

            return View(todayClasses);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAttendance([FromBody] MarkAttendanceRequest request)
        {
            var studentIdClaim = User.FindFirst("StudentId")?.Value;
            if (studentIdClaim == null || !int.TryParse(studentIdClaim, out int studentId))
            {
                return Json(new { success = false, message = "Öğrenci bilgisi bulunamadı." });
            }

            try
            {
                // Check if student is enrolled in the course
                var studentCourse = await _context.StudentCourses
                    .FirstOrDefaultAsync(sc => sc.StudentId == studentId && sc.CourseId == request.CourseId);

                if (studentCourse == null)
                {
                    return Json(new { success = false, message = "Bu derse kayıtlı değilsiniz." });
                }

                // Check if attendance already exists for today
                var today = DateTime.Today;
                var existingAttendance = await _context.Attendances
                    .FirstOrDefaultAsync(a => a.StudentId == studentId && 
                                            a.CourseId == request.CourseId && 
                                            a.AttendanceDate.Date == today);

                if (existingAttendance != null)
                {
                    return Json(new { success = false, message = "Bugün için yoklamanız zaten alınmış." });
                }

                // Create new attendance record
                var attendance = new Attendance
                {
                    StudentId = studentId,
                    CourseId = request.CourseId,
                    AttendanceDate = DateTime.Now,
                    ClassTime = DateTime.Now.TimeOfDay,
                    IsPresent = true
                };

                _context.Attendances.Add(attendance);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Yoklamanız başarıyla kaydedildi." });
            }
            catch (Exception ex)
            {
                // Log the error
                return Json(new { success = false, message = "Bir hata oluştu. Lütfen tekrar deneyin." });
            }
        }

        public async Task<IActionResult> Profile()
        {
            ViewData["Title"] = "Kişisel Bilgilerim";
            
            var studentIdClaim = User.FindFirst("StudentId")?.Value;
            if (studentIdClaim == null || !int.TryParse(studentIdClaim, out int studentId))
            {
                return RedirectToAction("Login", "Account");
            }

            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.Id == studentId);

            if (student == null)
            {
                return RedirectToAction("Login", "Account");
            }

            return View(student);
        }

        public IActionResult Support()
        {
            ViewData["Title"] = "Yardım ve Destek";
            return View();
        }
    }

    public class MarkAttendanceRequest
    {
        public int CourseId { get; set; }
    }
} 