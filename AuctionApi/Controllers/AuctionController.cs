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

    [HttpPost("AddProduct")]
    public async Task<Product> AddProduct([FromBody] Product product)
    {
        _logger.LogInformation("AddProduct called with Title: {Title}", product.Name);
        try
        {
            product.IsApproved = false;
            product.EndOfAuction = DateTime.Now.AddDays(30);
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

    [HttpPost("{productId}/bids")]
    public async Task<IActionResult> PlaceBid(string productId, [FromBody] BidHistory bid)
    {
        _logger.LogInformation("PlaceBid called for Product ID: {ProductId} with Bid Amount: {BidAmount}", productId, bid?.BidAmount);

        if (bid == null || bid.BidAmount <= 0)
        {
            _logger.LogWarning("Invalid bid submitted for Product ID: {ProductId}", productId);
            return BadRequest("Invalid bid.");
        }

        try
        {
            var updatedProduct = await _productRepository.AddBidAsync(productId, bid);
            if (updatedProduct == null)
            {
                _logger.LogWarning("Product not found for placing bid: {ProductId}", productId);
                return NotFound();
            }

            _logger.LogInformation("Bid placed successfully on Product ID: {ProductId}", productId);
            return Ok(updatedProduct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in PlaceBid for Product ID: {ProductId}", productId);
            throw;
        }
    }

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
