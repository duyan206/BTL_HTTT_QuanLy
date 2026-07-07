using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QL_ThuChiNoiBo.Constants;

namespace QL_ThuChiNoiBo.Areas.Director.Controllers
{
    [Area("Director")]
    [Authorize(Roles = RoleConstants.Director)]
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
