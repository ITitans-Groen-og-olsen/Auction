using Microsoft.AspNetCore.Mvc.RazorPages;
using Models;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace MyApp.Namespace
{
    public class CatalogListModel : PageModel
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly ILogger<CatalogListModel> _logger;

        public List<Product>? Products { get; set; }


        // Inject IHttpClientFactory and ILogger
        public CatalogListModel(IHttpClientFactory clientFactory, ILogger<CatalogListModel> logger)
        {
            _clientFactory = clientFactory;
            _logger = logger;
        }

        public async Task OnGetAsync()
        {
            try
            {
                using HttpClient client = _clientFactory.CreateClient("gateway");
                var allProducts = await client.GetFromJsonAsync<List<Product>>("Auction/GetAllProducts");
                _logger.LogInformation("Fetching products from Auction microservice");

                //  Only include approved products that have not expired
                Products = allProducts?
                    .Where(p => p.IsApproved && p.EndOfAuction > DateTime.UtcNow)
                    .ToList();
                _logger.LogInformation("Loaded {Count} approved products", Products?.Count ?? 0);
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch products");
            }
        }

    }
}
