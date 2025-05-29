using Microsoft.AspNetCore.Mvc.RazorPages;
using Models;
using System.Net.Http.Json;

namespace MyApp.Namespace
{
    public class CatalogListModel : PageModel
    {
        private readonly IHttpClientFactory _clientFactory;

        public List<Product>? Products { get; set; }

        public CatalogListModel(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        public async Task OnGetAsync()
{
    try
    {
        using HttpClient client = _clientFactory.CreateClient("gateway");
        var allProducts = await client.GetFromJsonAsync<List<Product>>("Auction/GetAllProducts");

        //  Only include approved products that have not expired
        Products = allProducts?
            .Where(p => p.IsApproved && p.EndOfAuction > DateTime.UtcNow)
            .ToList();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Failed to fetch products: {ex.Message}");
    }
}

    }
}
