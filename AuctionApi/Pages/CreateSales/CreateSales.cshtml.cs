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


    public AddProductModel(IHttpClientFactory clientFactory)
    {
        _clientFactory = clientFactory;
    }

    [BindProperty]
    public Product Product { get; set; } = new();

    [BindProperty]
    public IFormFile ImageFile { get; set; }

    public bool Submitted { get; set; } = false;

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        var userId = HttpContext.Session.GetString("userId");
        var jwtToken = HttpContext.Session.GetString("jwtToken");

        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(jwtToken))
        {
            ModelState.AddModelError(string.Empty, "Du skal være logget ind for at tilføje et produkt.");
            return Redirect("/Login");
        }

        var client = _clientFactory.CreateClient("gateway");
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken);

        // Get user details
        var userResponse = await client.GetAsync($"/User/GetUserById/{userId}");
        if (!userResponse.IsSuccessStatusCode)
        {
            ModelState.AddModelError(string.Empty, "Brugeroplysninger kunne ikke hentes.");
            return Page();
        }

        var user = await userResponse.Content.ReadFromJsonAsync<UserData>();
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "Bruger ikke fundet.");
            return Page();
        }

        // Set CustomerNumber
        Product.CustomerNumber = user.CustomerNumber;

        // Convert image to base64
        if (ImageFile != null && ImageFile.Length > 0)
        {
            using var ms = new MemoryStream();
            await ImageFile.CopyToAsync(ms);
            Product.Image = Convert.ToBase64String(ms.ToArray());
        }

        // Send product to API
        var response = await client.PostAsJsonAsync("Auction/AddProduct", Product);

        if (response.IsSuccessStatusCode)
        {
            Submitted = true;
            return Redirect("/auction/User");
        }

        ModelState.AddModelError(string.Empty, "Noget gik galt under oprettelsen.");
        return Page();
    }

}

public class UserData
{
    public Guid Id { get; set; }
    public int CustomerNumber { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? EmailAddress { get; set; }
}

