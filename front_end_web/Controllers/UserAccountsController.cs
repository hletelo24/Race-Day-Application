using Microsoft.AspNetCore.Mvc;

namespace front_end_web.Controllers
{
    public class UserAccountsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
