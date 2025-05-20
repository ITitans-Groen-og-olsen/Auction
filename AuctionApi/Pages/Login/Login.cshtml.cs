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

        [BindProperty]
        public RegisterInputModel RegisterModel { get; set; } = new();

        public bool LoginFailed { get; set; }

        public async Task<IActionResult> OnPostAsync()
        {
            var client = _clientFactory.CreateClient("gateway");
            var endpoint = LoginModel.Role == "Admin" ? "admin/login" : "user/login";

            var response = await client.PostAsJsonAsync(endpoint, LoginModel);
            if (response.IsSuccessStatusCode)
            {
                return RedirectToPage(LoginModel.Role == "Admin" ? "/AdminDashboard" : "/UserDashboard");
            }

            LoginFailed = true;
            return Page();
        }

        public async Task<IActionResult> OnPostRegisterAsync()
        {
            var client = _clientFactory.CreateClient("gateway");

            var response = await client.PostAsJsonAsync("user/register", RegisterModel);
            if (response.IsSuccessStatusCode)
            {
                return RedirectToPage("/UserDashboard");
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError(string.Empty, $"Registration failed: {errorContent}");
            return Page();
        }

        public class LoginInputModel
        {
            [Required]
            public string Email { get; set; } = "";

            [Required]
            public string Password { get; set; } = "";

            [Required]
            public string Role { get; set; } = "User";
        }

        public class RegisterInputModel
        {
            public string? FirstName { get; set; }
            public string? LastName { get; set; }
            public string? Address { get; set; }
            public short PostalCode { get; set; }
            public string? City { get; set; }
            public string? PhoneNumber { get; set; }

            [Required]
            public string? Email { get; set; }

            [Required]
            public string? Password { get; set; }

            public int CustomerNumber { get; set; } = 0;
        }
    }
}
