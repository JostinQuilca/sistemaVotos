using Microsoft.AspNetCore.Mvc;

namespace SistemaVotoMVC.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
           return View();
        }
        //public IActionResult Acceder()
        //{
        //    return RedirectToAction("Login", "Aut");
        //}
    }
}
