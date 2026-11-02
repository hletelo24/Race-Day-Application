using Microsoft.AspNetCore.Mvc;

namespace race_day_front_end.Controllers
{
    public class EnrolmentsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
