using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LTWNC.Middleware
{
    public class AuthenticationMiddleware
    {
        private readonly RequestDelegate _next;

        public AuthenticationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Kiểm tra nếu đang truy cập Razor Page
            if (context.Request.Path.StartsWithSegments("/Home"))
            {
                var token = context.Request.Cookies["accessToken"];
                if (string.IsNullOrEmpty(token))
                {
                    // Nếu không có token, chuyển hướng về trang Login
                    context.Response.Redirect("/");
                    return;
                }
            }

            await _next(context);
        }
    }
} 