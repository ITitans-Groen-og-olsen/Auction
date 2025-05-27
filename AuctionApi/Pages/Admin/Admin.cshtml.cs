using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Http;

namespace MyApp.Namespace
{
    public class AdminModel : PageModel
    {
        public string? JwtToken { get; private set; }

        public void OnGet()
        {
            JwtToken = HttpContext.Session.GetString("jwtToken");
            ViewData["IsAdmin"] = true; // ✅ This tells the layout we're on the admin page
        }
    }
}
