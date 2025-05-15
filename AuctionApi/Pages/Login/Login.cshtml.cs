using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Net.Http;
using System.Net.Http.Json;

namespace MyApp.Namespace
{
    public class LoginPageModel : PageModel
    {
        private readonly IHttpClientFactory _clientFactory;

        public LoginPageModel(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        [BindProperty]
        public LoginInputModel LoginModel { get; set; } = new();

        public bool LoginFailed { get; set; }

        public async Task<IActionResult> OnPostAsync()
        {
            var client = _clientFactory.CreateClient("gateway");

            var response = await client.PostAsJsonAsync("user/login", LoginModel);

            if (response.IsSuccessStatusCode)
            {
                // TODO: Handle successful login (e.g., redirect, set session, JWT, etc.)
                return RedirectToPage("/CatalogPage");
            }

            LoginFailed = true;
            return Page();
        }

        public class LoginInputModel
        {
            [Required]
            public string Email { get; set; } = "";

            [Required]
            public string Password { get; set; } = "";
        }
    }
}
