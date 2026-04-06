using Microsoft.AspNetCore.Mvc;

namespace GECPatan.Admin.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
