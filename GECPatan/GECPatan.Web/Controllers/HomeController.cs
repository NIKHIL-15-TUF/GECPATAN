using Microsoft.AspNetCore.Mvc;

namespace GECPatan.Web.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
