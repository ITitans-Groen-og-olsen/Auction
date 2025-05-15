using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

public class RegisterModel : PageModel
{
    private readonly HttpClient _httpClient;

    public RegisterModel(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient();
        _httpClient.BaseAddress = new Uri("http://gateway/"); // Or use config
    }

    [BindProperty]
    public RegisterRequest NewUser { get; set; } = new RegisterRequest();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        var response = await _httpClient.PostAsJsonAsync("user/create", NewUser);

        if (response.IsSuccessStatusCode)
        {
            // Redirect to login after successful registration
            return RedirectToPage("/Login");
        }

        ModelState.AddModelError(string.Empty, "Failed to create account. Please try again.");
        return Page();
    }

    public class RegisterRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = "";

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = "";
    }
}
