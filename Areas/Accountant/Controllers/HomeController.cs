using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QL_ThuChiNoiBo.Constants;

namespace QL_ThuChiNoiBo.Areas.Accountant.Controllers
{
    [Area("Accountant")]
    [Authorize(Roles = RoleConstants.Accountant)]
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
