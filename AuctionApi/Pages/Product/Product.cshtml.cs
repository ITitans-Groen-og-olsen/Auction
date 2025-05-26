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

    public UserModel? LoggedInUser { get; set; }


    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [BindProperty]
    public decimal BidAmount { get; set; }

    public Product? Product { get; set; }
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = HttpContext.Session.GetString("userId");
        var jwtToken = HttpContext.Session.GetString("jwtToken");

        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(jwtToken))
        {
            return Redirect("/Login");
        }

        var client = _httpClientFactory.CreateClient("gateway");
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken);

        // Fetch user
        var userResponse = await client.GetAsync($"/User/GetUserById/{userId}");
        if (userResponse.IsSuccessStatusCode)
        {
            LoggedInUser = await userResponse.Content.ReadFromJsonAsync<UserModel>();
        }

        // Fetch product
        Product = await client.GetFromJsonAsync<Product>($"Auction/GetProductById/{Id}");

        if (Product == null)
        {
            return NotFound();
        }

        return Page();
    }


    public async Task<IActionResult> OnPostAsync()
    {
        var userId = HttpContext.Session.GetString("userId");
        var jwtToken = HttpContext.Session.GetString("jwtToken");

        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(jwtToken))
        {
            ErrorMessage = "You must be logged in to place a bid.";
            return Redirect("/Login");
        }

        var client = _httpClientFactory.CreateClient("gateway");
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken);

        // Fetch user
        var userResponse = await client.GetAsync($"/User/GetUserById/{userId}");
        if (!userResponse.IsSuccessStatusCode)
        {
            ErrorMessage = "Could not verify user.";
            return Page();
        }

        LoggedInUser = await userResponse.Content.ReadFromJsonAsync<UserModel>();
        if (LoggedInUser is null)
        {
            ErrorMessage = "Could not load user info.";
            return Page();
        }

        // Fetch product
        Product = await client.GetFromJsonAsync<Product>($"Auction/GetProductById/{Id}");
        if (Product == null)
        {
            return NotFound();
        }

        var minBid = Product.currentbid ?? Product.StartPrice;
        if (BidAmount <= minBid)
        {
            ErrorMessage = $"Your bid must be higher than {minBid:C}.";
            return Page();
        }

        var bid = new BidHistory
        {
            CustomerNumber = LoggedInUser.CustomerNumber,
            BidAmount = BidAmount,
            BidTime = DateTime.UtcNow
        };

        var endpoint = $"Auction/bid/{Id}";      
        Console.WriteLine($"This is the base adress {client.BaseAddress!}");
        Console.WriteLine($"Tis is the bid value {bid.BidAmount}");
         

        var response = await client.PostAsJsonAsync(endpoint, bid);

        Console.WriteLine($"This is the endpoint before: {endpoint}");
        Console.WriteLine($"This is the base adress {client.BaseAddress!}");
        Console.WriteLine($"This is the bid value {bid.BidAmount}");


        if (!response.IsSuccessStatusCode)
        {
            ErrorMessage = "Failed to place bid.";
            return Page();
        }

        SuccessMessage = "Bid placed successfully!";
        Product = await client.GetFromJsonAsync<Product>($"Auction/GetProductById/{Id}");
        return Page();
    }
}

public class UserModel
{
    public Guid Id { get; set; }
    public int CustomerNumber { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? EmailAddress { get; set; }
}
