using Microsoft.AspNetCore.Mvc;
using ProgettoTesi.Models;
using System.Diagnostics;

namespace ProgettoTesi.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        private static readonly HttpClient client = new HttpClient();

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
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

        [HttpGet]
        public async Task<IActionResult> _AlertModale(string title, string message)
        {
            ViewBag.Title = title;
            ViewBag.Message = message;

            return await Task.FromResult(PartialView("_AlertModale"));
        }

    }
}