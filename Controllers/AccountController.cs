using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YoklamaAlma.Data;
using YoklamaAlma.Models;

namespace YoklamaAlma.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            if (User.Identity.IsAuthenticated)
            {
                var role = User.FindFirst(ClaimTypes.Role)?.Value;
                if (role == UserRole.Admin.ToString())
                    return RedirectToAction("Dashboard", "Admin");
                else if (role == UserRole.Student.ToString())
                    return RedirectToAction("Dashboard", "Student");
            }
            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (ModelState.IsValid)
            {
                User user;
                if (model.UserType == UserRole.Student)
                {
                    user = await _context.Users
                        .Include(u => u.Student)
                        .FirstOrDefaultAsync(u => u.Username == model.Username && u.Role == UserRole.Student);
                }
                else
                {
                    user = await _context.Users
                        .FirstOrDefaultAsync(u => u.Username == model.Username && u.Role == UserRole.Admin);
                }

                if (user != null && BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
                {
                    if (user.Role != model.UserType)
                    {
                        if (model.UserType == UserRole.Admin)
                            ModelState.AddModelError("", "Bu hesap admin yetkisine sahip değil.");
                        else
                            ModelState.AddModelError("", "Bu hesap öğrenci hesabı değil.");
                        return View(model);
                    }

                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, user.Username),
                        new Claim(ClaimTypes.Role, user.Role.ToString()),
                    };

                    if (user.StudentId.HasValue)
                    {
                        claims.Add(new Claim("StudentId", user.StudentId.Value.ToString()));
                    }

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = true,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddHours(3),
                        RedirectUri = returnUrl
                    };

                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        authProperties);

                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                        return Redirect(returnUrl);

                    if (user.Role == UserRole.Admin)
                        return RedirectToAction("Dashboard", "Admin");
                    else
                        return RedirectToAction("Dashboard", "Student");
                }

                ModelState.AddModelError("", "Geçersiz kullanıcı adı veya şifre");
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }
    }
} 