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
                Products = await client.GetFromJsonAsync<List<Product>>("Auction/GetAllProducts");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to fetch products: {ex.Message}");
            }
        }
    }
}
