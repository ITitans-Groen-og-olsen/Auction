using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;

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
            if (!ModelState.IsValid)
                return Page();

            var client = _clientFactory.CreateClient("gateway");
            var endpoint = "/Auth/UserLogin";

            try
            {
                var response = await client.PostAsJsonAsync(endpoint, new
                {
                    EmailAddress = LoginModel.EmailAddress,
                    Password = LoginModel.Password
                });

                var json = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Raw response: {json}");

                if (!response.IsSuccessStatusCode)
                {
                    LoginFailed = true;
                    return Page();
                }

                var root = JsonSerializer.Deserialize<LoginResponseWrapper>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (root?.ReturnObject?.JwtToken is not null)
                {
                    // ✅ Store in session
                    HttpContext.Session.SetString("userId", root.ReturnObject.Id);
                    HttpContext.Session.SetString("jwtToken", root.ReturnObject.JwtToken);

                    return Redirect("/auction/User");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Login exception: {ex.Message}");
            }

            LoginFailed = true;
            return Page();
        }

        public class LoginInputModel
        {
            [Required(ErrorMessage = "Email is required.")]
            public string EmailAddress { get; set; }

            [Required(ErrorMessage = "Password is required.")]
            public string Password { get; set; }
        }

        private class LoginResponseWrapper
        {
            [JsonPropertyName("returnObject")]
            public LoginResponse ReturnObject { get; set; }
        }

        private class LoginResponse
        {
            [JsonPropertyName("id")]
            public string Id { get; set; }

            [JsonPropertyName("jwtToken")]
            public string JwtToken { get; set; }
        }
    }
}
