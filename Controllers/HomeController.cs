using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using YoklamaAlma.Models;
using System.Security.Claims;

namespace YoklamaAlma.Controllers
{
	public class HomeController : Controller
	{
		private readonly ILogger<HomeController> _logger;

		public HomeController(ILogger<HomeController> logger)
		{
			_logger = logger;
		}

		public IActionResult Index()
		{
			if (User.Identity.IsAuthenticated)
			{
				var role = User.FindFirst(ClaimTypes.Role)?.Value;
				if (role == UserRole.Admin.ToString())
					return RedirectToAction("Dashboard", "Admin");
				else if (role == UserRole.Student.ToString())
					return RedirectToAction("Dashboard", "Student");
			}
			return View();
		}

		public IActionResult Privacy()
		{
			return View();
		}

		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		public IActionResult Error()
		{
			return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
		}
	}
}
