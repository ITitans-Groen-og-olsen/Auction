using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MyApp.Namespace
{
    [AllowAnonymous] // REMOVE or RESTRICT later after admin service is implemented
    public class AdminModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}
