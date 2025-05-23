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
            var endpoint = "/Auth/Userlogin";

            try
            {
                var response = await client.PostAsJsonAsync(endpoint, LoginModel);
                Console.WriteLine($"endpoint for user/login {endpoint}");
                Console.WriteLine($"Email: {LoginModel.EmailAddress}");
                Console.WriteLine($"Password: {LoginModel.Password}");

                var content = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Status: {response.StatusCode}, Raw Content: {content}");

                if (response.IsSuccessStatusCode)
                {
                    if (bool.TryParse(content, out bool loginResult) && loginResult)
                    {
                        Console.WriteLine("Login successful (API returned true).");
                        return Redirect("/auction/UserDashboard");
                    }
                    else
                    {
                        Console.WriteLine("Login failed or invalid response.");
                    }
                }
                else
                {
                    Console.WriteLine("HTTP response was not successful.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
            }

            LoginFailed = true;
            return Page();
        }

        public class LoginInputModel
        {
            public string EmailAddress { get; set; }
            public string Password { get; set; }
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
