using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Models;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

public class ProductModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ProductModel> _logger;

    public ProductModel(IHttpClientFactory httpClientFactory, ILogger<ProductModel> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public UserModel? LoggedInUser { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [BindProperty]
    public decimal BidAmount { get; set; }

    public Product? Product { get; set; }
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    /// <summary>
    /// Handles GET requests to load product details and logged-in user information.
    /// </summary>
    public async Task<IActionResult> OnGetAsync()
    {
        var userId = HttpContext.Session.GetString("userId");
        var jwtToken = HttpContext.Session.GetString("jwtToken");

        // If user is logged in, fetch user info
        if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(jwtToken))
        {
            var client = _httpClientFactory.CreateClient("gateway");
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken);

            var userResponse = await client.GetAsync($"/User/GetUserById/{userId}");
            if (userResponse.IsSuccessStatusCode)
            {
                LoggedInUser = await userResponse.Content.ReadFromJsonAsync<UserModel>();
                _logger.LogInformation("Successfully loaded user with ID: {UserId}", userId);
            }
            else
            {
                _logger.LogWarning("Failed to fetch user data for ID: {UserId}", userId);
            }
        }

        // Always load the product data
        var anonymousClient = _httpClientFactory.CreateClient("gateway");
        Product = await anonymousClient.GetFromJsonAsync<Product>($"Auction/GetProductById/{Id}");

        if (Product == null)
        {
            _logger.LogWarning("Product not found with ID: {ProductId}", Id);
            return NotFound();
        }

        _logger.LogInformation("Loaded product with ID: {ProductId}", Id);
        return Page();
    }

    /// <summary>
    /// Handles POST requests to submit a bid on a product.
    /// </summary>
    public async Task<IActionResult> OnPostAsync()
    {
        var userId = HttpContext.Session.GetString("userId");
        var jwtToken = HttpContext.Session.GetString("jwtToken");

        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(jwtToken))
        {
            ErrorMessage = "You must be logged in to place a bid.";
            _logger.LogWarning("Bid attempt without authentication.");
            return Redirect("/Login");
        }

        var client = _httpClientFactory.CreateClient("gateway");
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken);

        // Fetch user info
        var userResponse = await client.GetAsync($"/User/GetUserById/{userId}");
        if (!userResponse.IsSuccessStatusCode)
        {
            ErrorMessage = "Could not verify user.";
            _logger.LogError("Failed to verify user with ID: {UserId}", userId);
            return Page();
        }

        LoggedInUser = await userResponse.Content.ReadFromJsonAsync<UserModel>();
        if (LoggedInUser is null)
        {
            ErrorMessage = "Could not load user info.";
            _logger.LogError("Null user data returned for ID: {UserId}", userId);
            return Page();
        }

        // Fetch current product info
        Product = await client.GetFromJsonAsync<Product>($"Auction/GetProductById/{Id}");
        if (Product == null)
        {
            _logger.LogError("Product not found during bid with ID: {ProductId}", Id);
            return NotFound();
        }

        var minBid = Product.currentbid ?? Product.StartPrice;
        if (BidAmount <= minBid)
        {
            ErrorMessage = $"Your bid must be higher than {minBid:C}.";
            _logger.LogWarning("Bid too low: {BidAmount} <= {MinBid}", BidAmount, minBid);
            return Page();
        }

        var bid = new BidHistory
        {
            CustomerNumber = LoggedInUser.CustomerNumber,
            BidAmount = BidAmount,
            BidTime = DateTime.UtcNow
        };

        var endpoint = $"Auction/bid/{Id}";
        _logger.LogInformation("Placing bid for ProductId={ProductId}, Customer={Customer}, Amount={Amount}", Id, bid.CustomerNumber, bid.BidAmount);

        var response = await client.PostAsJsonAsync(endpoint, bid);

        if (!response.IsSuccessStatusCode)
        {
            ErrorMessage = "Failed to place bid.";
            _logger.LogError("Bid submission failed with status: {StatusCode}", response.StatusCode);
            return Page();
        }

        SuccessMessage = "Bid placed successfully!";
        _logger.LogInformation("Bid placed successfully for ProductId={ProductId} by Customer={Customer}", Id, bid.CustomerNumber);

        // Refresh product details after bid
        Product = await client.GetFromJsonAsync<Product>($"Auction/GetProductById/{Id}");
        return Page();
    }
}

// Supporting UserModel class
public class UserModel
{
    public Guid Id { get; set; }
    public int CustomerNumber { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? EmailAddress { get; set; }
}
