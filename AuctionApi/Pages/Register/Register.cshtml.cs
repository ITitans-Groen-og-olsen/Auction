using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Net.Http;
using System.Net.Http.Json;


namespace MyApp.Namespace
{
    public class RegisterPageModel : PageModel
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly ILogger<RegisterPageModel> _logger; // Logger for this class

        // Inject IHttpClientFactory and ILogger via constructor
        public RegisterPageModel(IHttpClientFactory clientFactory, ILogger<RegisterPageModel> logger)
        {
            _clientFactory = clientFactory;
            _logger = logger;
        }

        // Property bound to the form inputs on the Razor Page
        [BindProperty]
        public RegisterInputModel RegisterModel { get; set; } = new();

        // Handles POST requests triggered when the registration form is submitted
        public async Task<IActionResult> OnPostAsync()
        {
            // Check if form validation failed; if so, redisplay the page with errors
            if (!ModelState.IsValid)
                return Page();

            // Create an HttpClient instance named "gateway" from the factory
            var client = _clientFactory.CreateClient("gateway");

            try
            {
                // Post the registration model as JSON to the User/AddUser API endpoint
                var response = await client.PostAsJsonAsync("User/AddUser", RegisterModel);

                // Log important details for debugging (avoid logging sensitive info like passwords in production)
                _logger.LogInformation("Registration attempt for email: {Email}", RegisterModel.EmailAddress);

                var content = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Response status: {StatusCode}, content: {Content}", response.StatusCode, content);

                // If registration succeeded, redirect user to login page
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("User registered successfully.");
                    return Redirect("/auction/Login");
                }

                // If the response indicates failure, add the server response as a model error to display on the form
                var errorContent = await response.Content.ReadAsStringAsync();
                ModelState.AddModelError(string.Empty, $"Registration failed: {errorContent}");
                _logger.LogWarning("Registration failed with response: {ErrorContent}", errorContent);
            }
            catch (Exception ex)
            {
                // Log the exception and add a generic error message to ModelState
                _logger.LogError(ex, "Exception occurred during registration.");
                ModelState.AddModelError(string.Empty, $"An error occurred: {ex.Message}");
            }

            // Redisplay the page with error messages
            return Page();
        }

        // Class representing the input data from the registration form
        public class RegisterInputModel
        {
            public string? FirstName { get; set; }
            public string? LastName { get; set; }
            public string? Address { get; set; }
            public short PostalCode { get; set; }
            public string? City { get; set; }
            public string? PhoneNumber { get; set; }

            [Required]                      
            public string? EmailAddress { get; set; }

            [Required]                      
            public string? Password { get; set; }

            public int CustomerNumber { get; set; } = 0;  
        }
    }
}
