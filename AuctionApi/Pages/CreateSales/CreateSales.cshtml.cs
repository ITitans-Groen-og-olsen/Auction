using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using Models;


[IgnoreAntiforgeryToken]
public class AddProductModel : PageModel
{
    private readonly IHttpClientFactory _clientFactory;
    private readonly ILogger<AddProductModel> _logger;

    public AddProductModel(IHttpClientFactory clientFactory, ILogger<AddProductModel> logger)
    {
        _clientFactory = clientFactory;
        _logger = logger;
    }

    // Product object bound to form input
    [BindProperty]
    public Product Product { get; set; } = new();

    // Uploaded image file
    [BindProperty]
    public IFormFile ImageFile { get; set; }

    // Flag to show success message
    public bool Submitted { get; set; } = false;

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Form submission failed due to invalid model state.");
            return Page();
        }
        // Retrieve session-based authentication
        var userId = HttpContext.Session.GetString("userId");
        var jwtToken = HttpContext.Session.GetString("jwtToken");

        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(jwtToken))
        {
            _logger.LogWarning("Unauthorized attempt to post product — missing session tokens.");
            ModelState.AddModelError(string.Empty, "Du skal være logget ind for at tilføje et produkt.");
            return Redirect("/Login");
        }
        // Create a configured HttpClient
        var client = _clientFactory.CreateClient("gateway");
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken);

        try
        {
            // Fetch user details
            var userResponse = await client.GetAsync($"/User/GetUserById/{userId}");
            if (!userResponse.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to retrieve user data. Status code: {StatusCode}", userResponse.StatusCode);
                ModelState.AddModelError(string.Empty, "Brugeroplysninger kunne ikke hentes.");
                return Page();
            }

            var user = await userResponse.Content.ReadFromJsonAsync<UserData>();
            if (user is null)
            {
                _logger.LogError("User data returned null for ID: {UserId}", userId);
                ModelState.AddModelError(string.Empty, "Bruger ikke fundet.");
                return Page();
            }

            // Assign user's customer number to product
            Product.CustomerNumber = user.CustomerNumber;

            // Convert uploaded image to Base64 string
            if (ImageFile != null && ImageFile.Length > 0)
            {
                using var ms = new MemoryStream();
                await ImageFile.CopyToAsync(ms);
                Product.Image = Convert.ToBase64String(ms.ToArray());

                _logger.LogInformation("Image uploaded successfully and encoded to Base64.");
            }
            else
            {
                _logger.LogWarning("No image was uploaded.");
            }

            // Send product suggestion to the Auction API
            var response = await client.PostAsJsonAsync("Auction/AddProduct", Product);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Product submitted successfully: {ProductName}", Product.Name);
                Submitted = true;
                return Redirect("/auction/User");
            }

            _logger.LogError("API responded with failure when submitting product: {StatusCode}", response.StatusCode);
            ModelState.AddModelError(string.Empty, "Noget gik galt under oprettelsen.");
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred during product submission.");
            ModelState.AddModelError(string.Empty, "Uventet fejl under behandlingen af produktet.");
            return Page();
        }
    }
}
// DTO for user info returned from User service
public class UserData
{
    public Guid Id { get; set; }
    public int CustomerNumber { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? EmailAddress { get; set; }
}

