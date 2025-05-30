using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace MyApp.Namespace
{
    public class LoginPageModel : PageModel
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly ILogger<LoginPageModel> _logger;

        // Constructor injecting HttpClientFactory and Logger
        public LoginPageModel(IHttpClientFactory clientFactory, ILogger<LoginPageModel> logger)
        {
            _clientFactory = clientFactory;
            _logger = logger;
        }

        // Binds login input data from the form
        [BindProperty]
        public LoginInputModel LoginModel { get; set; } = new();

        // Used to indicate failed login attempt to UI
        public bool LoginFailed { get; set; }

        // Handle POST (login) request
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Login model state is invalid.");
                return Page();
            }

            var client = _clientFactory.CreateClient("gateway");

            // Clear existing session data
            HttpContext.Session.Clear();

            try
            {
                if (LoginModel.IsAdmin)
                {
                    // Admin login API call
                    var response = await client.PostAsJsonAsync("/Auth/AdminLogin", new
                    {
                        EmailAddress = LoginModel.EmailAddress,
                        Password = LoginModel.Password
                    });

                    var json = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode || string.IsNullOrWhiteSpace(json))
                    {
                        _logger.LogWarning("Admin login failed with status code {StatusCode}", response.StatusCode);
                        LoginFailed = true;
                        return Page();
                    }

                    var adminToken = JsonSerializer.Deserialize<AdminLoginResponse>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (!string.IsNullOrEmpty(adminToken?.Token))
                    {
                        // Save token in session and redirect to admin page
                        HttpContext.Session.SetString("jwtToken", adminToken.Token);
                        HttpContext.Session.SetString("role", "Admin");

                        _logger.LogInformation("Admin login successful for {Email}", LoginModel.EmailAddress);
                        return Redirect("/auction/Admin");
                    }
                }
                else
                {
                    // Regular user login API call
                    var response = await client.PostAsJsonAsync("/Auth/UserLogin", new
                    {
                        EmailAddress = LoginModel.EmailAddress,
                        Password = LoginModel.Password
                    });

                    var json = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode || string.IsNullOrWhiteSpace(json))
                    {
                        _logger.LogWarning("User login failed with status code {StatusCode}", response.StatusCode);
                        LoginFailed = true;
                        return Page();
                    }

                    var root = JsonSerializer.Deserialize<LoginResponseWrapper>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (!string.IsNullOrEmpty(root?.ReturnObject?.JwtToken))
                    {
                        // Save token and userId in session
                        HttpContext.Session.SetString("userId", root.ReturnObject.Id);
                        HttpContext.Session.SetString("jwtToken", root.ReturnObject.JwtToken);
                        HttpContext.Session.SetString("role", "User");

                        _logger.LogInformation("User login successful for {Email}", LoginModel.EmailAddress);
                        return Redirect("/auction/User");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred during login for {Email}", LoginModel.EmailAddress);
            }

            // Fall-through case: login failed
            LoginFailed = true;
            return Page();
        }

        // Model to hold login form input
        public class LoginInputModel
        {
            [Required(ErrorMessage = "Email is required.")]
            public string EmailAddress { get; set; }

            [Required(ErrorMessage = "Password is required.")]
            public string Password { get; set; }

            public bool IsAdmin { get; set; }
        }

        // Wrapper for user login response
        private class LoginResponseWrapper
        {
            [JsonPropertyName("returnObject")]
            public LoginResponse ReturnObject { get; set; }
        }

        // User login response payload
        private class LoginResponse
        {
            [JsonPropertyName("id")]
            public string Id { get; set; }

            [JsonPropertyName("jwtToken")]
            public string JwtToken { get; set; }
        }

        // Admin login response payload
        private class AdminLoginResponse
        {
            [JsonPropertyName("token")]
            public string Token { get; set; }
        }
    }
}
