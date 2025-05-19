using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Models;
using Services;

public class ProductModel : PageModel
{
    private readonly IAuctionDBRepository _repository;
    private readonly IHttpClientFactory _httpClientFactory;

    public ProductModel(IAuctionDBRepository repository, IHttpClientFactory httpClientFactory)
    {
        _repository = repository;
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
        Product = await _repository.GetProductByIdAsync(Id);
        if (Product == null)
            return NotFound();

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Product = await _repository.GetProductByIdAsync(Id);
        if (Product == null)
            return NotFound();

        var minBid = Product.currentbid ?? Product.StartPrice;
        if (BidAmount <= minBid)
        {
            ErrorMessage = $"Your bid must be higher than {minBid:C}.";
            return Page();
        }

        if (!User.Identity?.IsAuthenticated ?? true)
        {
            ErrorMessage = "You must be logged in to place a bid.";
            return Page();
        }

        // Simulate logged-in user
        int simulatedUserId = 1;

        var bid = new BidHistory
        {
            BidderId = simulatedUserId,
            BidAmount = BidAmount,
            BidTime = DateTime.UtcNow
        };

        await _repository.AddBidAsync(Id.ToString(), bid);

        SuccessMessage = "Bid placed successfully!";
        Product = await _repository.GetProductByIdAsync(Id); // Refresh data

        return Page();
    }
}
