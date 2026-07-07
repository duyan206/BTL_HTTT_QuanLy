using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;

namespace QL_ThuChiNoiBo.Filters
{
    public class HasPermissionAttribute : TypeFilterAttribute
    {
        public HasPermissionAttribute(string permission) : base(typeof(HasPermissionFilter))
        {
            Arguments = new object[] { permission };
        }
    }

    public class HasPermissionFilter : IAuthorizationFilter
    {
        private readonly string _permission;

        public HasPermissionFilter(string permission)
        {
            _permission = permission;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            bool hasAccess = context.HttpContext.User.HasClaim("Permission", _permission);
            if (!hasAccess)
            {
                var factory = context.HttpContext.RequestServices.GetService<ITempDataDictionaryFactory>();
                if (factory != null)
                {
                    var tempData = factory.GetTempData(context.HttpContext);
                    tempData["Error"] = "Lỗ hổng RBAC bị chặn: Bạn tuyệt đối không được cấp quyền truy cập chức năng này!";
                }

                var controller = context.RouteData.Values["controller"]?.ToString();
                var area = context.RouteData.Values["area"]?.ToString();

                var isSystemAdmin = context.HttpContext.User.HasClaim(c => c.Type == "Permission" && (c.Value == "USER_MANAGE" || c.Value == "ROLE_MANAGE" || c.Value == "AUDIT_VIEW"));

                if (isSystemAdmin && area != "Admin")
                {
                    context.Result = new RedirectResult("/Admin/Home/Index");
                }
                else if (isSystemAdmin && area == "Admin")
                {
                    context.Result = new RedirectToActionResult("AccessDenied", "Auth", new { area = "" });
                }
                else 
                {
                    context.Result = new RedirectToActionResult("Index", "PhieuDeXuat", new { area = "" });
                }
            }
        }
    }
}



