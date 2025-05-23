using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Models;
using System.Net.Http.Json;

public class ProductModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;

    public ProductModel(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [BindProperty]
    public decimal BidAmount { get; set; }

    public Product? Product { get; set; }
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        Console.WriteLine("🚀 Entered OnGetAsync");

        try
        {
            Console.WriteLine($"[OnGetAsync] Creating client for Product ID: {Id}");
            var client = _httpClientFactory.CreateClient("gateway");

            Console.WriteLine($"[OnGetAsync] Requesting Auction/GetProductById/{Id}");
            Product = await client.GetFromJsonAsync<Product>($"Auction/GetProductById/{Id}");

            if (Product == null)
            {
                Console.WriteLine($"[OnGetAsync] Product with ID {Id} not found.");
                return NotFound();
            }

            Console.WriteLine($"[OnGetAsync] Successfully retrieved product: {Product.Name}");
            return Page();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ [OnGetAsync] Exception occurred: {ex.Message}");
            return StatusCode(500);
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Console.WriteLine("🚀 Entered OnPostAsync");

        try
        {
            Console.WriteLine($"[OnPostAsync] Creating client for Product ID: {Id}");
            var client = _httpClientFactory.CreateClient("gateway");

            Console.WriteLine($"[OnPostAsync] Requesting Auction/GetProductById/{Id}");
            Product = await client.GetFromJsonAsync<Product>($"Auction/GetProductById/{Id}");

            if (Product == null)
            {
                Console.WriteLine($"[OnPostAsync] Product with ID {Id} not found.");
                return NotFound();
            }

            var minBid = Product.currentbid ?? Product.StartPrice;
            Console.WriteLine($"[OnPostAsync] Current bid: {Product.currentbid}, Start price: {Product.StartPrice}, Submitted bid: {BidAmount}");

            if (BidAmount <= minBid)
            {
                ErrorMessage = $"Your bid must be higher than {minBid:C}.";
                Console.WriteLine($"[OnPostAsync] Bid too low.");
                return Page();
            }

            if (!User.Identity?.IsAuthenticated ?? true)
            {
                ErrorMessage = "You must be logged in to place a bid.";
                Console.WriteLine($"[OnPostAsync] User not authenticated.");
                return Page();
            }

            int simulatedUserId = 1;

            var bid = new BidHistory
            {
                BidderId = simulatedUserId,
                BidAmount = BidAmount,
                BidTime = DateTime.UtcNow
            };

            Console.WriteLine($"[OnPostAsync] Sending bid: {bid.BidAmount} by user {bid.BidderId}");
            var response = await client.PostAsJsonAsync($"Auction/{Id}/bids", bid);

            if (!response.IsSuccessStatusCode)
            {
                ErrorMessage = "Failed to place bid.";
                Console.WriteLine($"[OnPostAsync] Failed to post bid. StatusCode: {response.StatusCode}");
                return Page();
            }

            SuccessMessage = "Bid placed successfully!";
            Product = await client.GetFromJsonAsync<Product>($"Auction/GetProductById/{Id}");
            Console.WriteLine($"[OnPostAsync] Bid placed successfully.");

            return Page();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ [OnPostAsync] Exception occurred: {ex.Message}");
            return StatusCode(500);
        }
    }
}
