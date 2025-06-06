using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LTWNC.Pages.Home
{
    [Authorize] // Yêu cầu người dùng phải đăng nhập để truy cập trang này
    public class IndexModel : PageModel
    {
        public void OnGet()
        {
        }
    }
} 