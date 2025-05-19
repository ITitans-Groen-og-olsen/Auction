using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Models;
using System.Net.Http.Json;

public class CreateSalesModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;

    public CreateSalesModel(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [BindProperty]
    public Product Product { get; set; } = new();

    public bool Submitted { get; set; } = false;

    public async Task<IActionResult> OnPostAsync()
    {
        if (!User.Identity?.IsAuthenticated ?? true)
        {
            return Unauthorized();
        }

        // Set default fields
        Product.Id = Guid.NewGuid();
        Product.currentbid = null;
        Product.BidHistory = new List<BidHistory>();
        Product.CurrentBidder = null;
        Product.Brand = "Brugerforslag";
        // You can add a status field in DB manually, or in another property if needed

        var client = _httpClientFactory.CreateClient("gateway");

        var response = await client.PostAsJsonAsync("Auction/CreateProduct", Product);

        if (response.IsSuccessStatusCode)
        {
            Submitted = true;
        }
        else
        {
            ModelState.AddModelError(string.Empty, "Noget gik galt ved oprettelse.");
        }

        return Page();
    }
}
