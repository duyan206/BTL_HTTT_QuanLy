using System.ComponentModel.DataAnnotations;

namespace QL_ThuChiNoiBo.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập Tên đăng nhập")]
        public string? Username { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập Mật khẩu")]
        [DataType(DataType.Password)]
        public string? Password { get; set; }

        public bool RememberMe { get; set; }
    }
}
