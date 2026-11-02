using Microsoft.AspNetCore.Mvc;

namespace front_end_web.Controllers
{
    public class EnrolmentsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
