using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Models;
using System.Net.Http.Json;

public class CreateSalesModel : PageModel
{
    private readonly IHttpClientFactory _clientFactory;
    private readonly ILogger<CreateSalesModel> _logger;

    public CreateSalesModel(IHttpClientFactory httpClientFactory, ILogger<CreateSalesModel> logger)
    {
        _clientFactory = httpClientFactory;
        _logger = logger;
    }

    [BindProperty]
    public Product Product { get; set; } = new();

    [BindProperty]
    public IFormFile ImageFile { get; set; }

    public bool Submitted { get; set; }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        if (ImageFile != null && ImageFile.Length > 0)
        {
            using var ms = new MemoryStream();
            await ImageFile.CopyToAsync(ms);
            var imageBytes = ms.ToArray();
            Product.Image = Convert.ToBase64String(imageBytes);
        }

        try
{
    using HttpClient client = _clientFactory.CreateClient("gateway");
    var response = await client.PostAsJsonAsync("Auction/AddProduct", Product);

    if (response.IsSuccessStatusCode)
    {
        Submitted = true;
        return RedirectToPage("auction/Catalog");
    }

    ModelState.AddModelError(string.Empty, "Noget gik galt. Prøv igen.");
}
catch (Exception ex)
{
    _logger.LogError(ex, "Failed to send product to Auction/AddProduct");
    ModelState.AddModelError(string.Empty, "Der opstod en fejl: " + ex.Message);
}
        return RedirectToPage("auction/Catalog");
    }
}
