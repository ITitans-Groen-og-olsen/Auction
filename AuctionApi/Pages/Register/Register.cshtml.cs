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

        public RegisterPageModel(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        [BindProperty]
        public RegisterInputModel RegisterModel { get; set; } = new();

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var client = _clientFactory.CreateClient("gateway");
            

            try
            {
                var response = await client.PostAsJsonAsync("User/AddUser", RegisterModel);

                Console.WriteLine($"Email: {RegisterModel.EmailAddress}");
                Console.WriteLine($"Password: {RegisterModel.Password}");

                var content = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Status: {response.StatusCode}, Content: {content}");

                if (response.IsSuccessStatusCode)
                {
                    return Redirect("/auction/Login");
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                ModelState.AddModelError(string.Empty, $"Registration failed: {errorContent}");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"An error occurred: {ex.Message}");
            }

            return Page();
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
            public string? EmailAddress { get; set; }

            [Required]
            public string? Password { get; set; }

            public int CustomerNumber { get; set; } = 0;
        }
    }
}
