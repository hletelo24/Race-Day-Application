using Microsoft.AspNetCore.Mvc;

namespace front_end_web.Controllers
{
    public class ParticipantsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
