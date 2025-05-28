using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LTWNC.Pages
{
    [AllowAnonymous] // Đảm bảo trang này không yêu cầu đăng nhập
    public class LoginModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}