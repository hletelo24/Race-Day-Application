using Microsoft.AspNetCore.Mvc;

namespace race_day_front_end.Controllers
{
    public class UserAccountsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
