using QL_ThuChiNoiBo.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QL_ThuChiNoiBo.Constants;

namespace QL_ThuChiNoiBo.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    [HasPermission("USER_MANAGE")]
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}


