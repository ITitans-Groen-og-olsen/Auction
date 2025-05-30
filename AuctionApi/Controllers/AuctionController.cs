using Microsoft.AspNetCore.Mvc;
using Models;
using Services;
using System.Diagnostics;

namespace AuctionService.Controllers;

[ApiController]
[Route("[controller]")]
public class AuctionController : ControllerBase
{
    private readonly ILogger<AuctionController> _logger;
    private readonly IAuctionDBRepository _productRepository;

    public AuctionController(ILogger<AuctionController> logger, IAuctionDBRepository productRepository)
    {
        _logger = logger;
        _productRepository = productRepository;

        var hostName = System.Net.Dns.GetHostName();
        var ips = System.Net.Dns.GetHostAddresses(hostName);
        var _ipaddr = ips.First().MapToIPv4().ToString();
        _logger.LogInformation("Auction Service responding from {IP}", _ipaddr);
    }
   // Returns the product that matches the ID
    [HttpGet("GetProductById/{productId}")]
    public async Task<Product> GetProductById(Guid productId)
    {
        _logger.LogInformation("GetProductById called with ID: {ProductId}", productId);
        try
        {
            var product = await _productRepository.GetProductByIdAsync(productId);
            if (product == null)
            {
                _logger.LogWarning("Product with ID {ProductId} not found", productId);
            }
            return product;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetProductById for ID: {ProductId}", productId);
            throw;
        }
    }
    // Returns all the products in the database
    [HttpGet("GetAllProducts")]
    public async Task<IEnumerable<Product>> GetAllProducts()
    {
        _logger.LogInformation("GetAllProducts called");
        try
        {
            var products = await _productRepository.GetAllProductsAsync();
            _logger.LogInformation("Retrieved {Count} products", products.Count());
            return products;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetAllProducts");
            throw;
        }
    }
    // Adds a new product to the database
    [HttpPost("AddProduct")]
    public async Task<Product> AddProduct([FromBody] Product product)
    {
        _logger.LogInformation("AddProduct called with Title: {Title}", product.Name);
        try
        {
            product.IsApproved = false;
            product.EndOfAuction = DateTime.Now.AddDays(30);
            product.BidHistory = new List<BidHistory>();

            var created = await _productRepository.CreateProductAsync(product);
            _logger.LogInformation("Product created with ID: {ProductId}", created.Id);
            return created;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in AddProduct");
            throw;
        }
    }
    // Updates the product with matching Id
    [HttpPut("UpdateProduct/{productId}")]
    public async Task<Product> UpdateProduct(string productId, Product product)
    {
        _logger.LogInformation("UpdateProduct called for ID: {ProductId}", productId);
        try
        {
            var updated = await _productRepository.UpdateProductAsync(productId, product);
            _logger.LogInformation("Product with ID: {ProductId} updated successfully", productId);
            return updated;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in UpdateProduct for ID: {ProductId}", productId);
            throw;
        }
    }
    // Delete the product with matching Id
    [HttpDelete("DeleteProduct/{productId}")]
    public async Task<bool> DeleteProduct(string productId)
    {
        _logger.LogInformation("DeleteProduct called for ID: {ProductId}", productId);
        try
        {
            var success = await _productRepository.DeleteProductAsync(productId);
            if (success)
            {
                _logger.LogInformation("Product with ID: {ProductId} deleted successfully", productId);
            }
            else
            {
                _logger.LogWarning("Failed to delete product with ID: {ProductId}", productId);
            }
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in DeleteProduct for ID: {ProductId}", productId);
            throw;
        }
    }
    // Add a bid to a product. 
    [HttpPost("bid/{productId}")]
public async Task<IActionResult> PlaceBid(Guid productId, [FromBody] BidHistory bid)
{
    _logger.LogInformation("PlaceBid called for Product ID: {ProductId} with Bid Amount: {BidAmount}", productId, bid?.BidAmount);

    if (bid == null || bid.BidAmount <= 0)
    {
        _logger.LogWarning("Invalid bid submitted for Product ID: {ProductId}", productId);
        return BadRequest("Invalid bid. Bid amount must be greater than 0.");
    }

    try
    {
        // Fetch the product by Guid
        var product = await _productRepository.GetProductByIdAsync(productId);
        if (product == null)
        {
            _logger.LogWarning("Product not found: {ProductId}", productId);
            return NotFound($"Product with ID {productId} not found.");
        }

        // Determine the highest existing bid safely
        decimal highestBid = 0;

        if (product.BidHistory != null && product.BidHistory.Any())
        {
            highestBid = product.BidHistory.Max(b => b.BidAmount);
        }

        // Minimum allowed bid must be at least the start price
        decimal minimumAllowedBid = Math.Max(highestBid, product.StartPrice);

        if (bid.BidAmount <= minimumAllowedBid)
        {
            _logger.LogWarning("Bid too low for Product ID: {ProductId}. Bid: {BidAmount}, Min Allowed: {MinAllowed}",
                productId, bid.BidAmount, minimumAllowedBid);
            return BadRequest($"Your bid must be at least above {minimumAllowedBid:C}.");
        }

        // Set bid time to now
        bid.BidTime = DateTime.Now;

        // Add the bid
        var updatedProduct = await _productRepository.AddBidAsync(productId, bid);
        if (updatedProduct == null)
        {
            _logger.LogWarning("Failed to add bid. Product not found: {ProductId}", productId);
            return NotFound($"Product with ID {productId} not found.");
        }

        _logger.LogInformation("Bid placed successfully on Product ID: {ProductId}", productId);
        return Ok(updatedProduct);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error placing bid for Product ID: {ProductId}", productId);
        return StatusCode(500, "An error occurred while placing the bid.");
    }
}


    // returns the version that is currently used
    [HttpGet("version")]
    public async Task<Dictionary<string, string>> GetVersion()
    {
        _logger.LogInformation("GetVersion called");
        var properties = new Dictionary<string, string>();
        var assembly = typeof(Program).Assembly;

        properties["service"] = "Auction Service";
        var ver = FileVersionInfo.GetVersionInfo(assembly.Location).ProductVersion;
        properties["version"] = ver!;

        try
        {
            var hostName = System.Net.Dns.GetHostName();
            var ips = await System.Net.Dns.GetHostAddressesAsync(hostName);
            var ipa = ips.First().MapToIPv4().ToString();
            properties["hosted-at-address"] = ipa;
            _logger.LogInformation("Service version {Version} hosted at {IP}", ver, ipa);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving host IP address");
            properties["hosted-at-address"] = "Could not resolve IP-address";
        }

        return properties;
    }
}
