using Microsoft.AspNetCore.Mvc;
using Models;
using Services;
using System.ComponentModel;
using System.Diagnostics;

namespace AuctionService.Controllers;

[ApiController]
[Route("[controller]")]
public class AuctionController : ControllerBase
{
    private readonly ILogger<AuctionController> _logger;
    private string _imagePath = string.Empty;
    private readonly IAuctionDBRepository _productRepository;

    public AuctionController(ILogger<AuctionController> logger, IAuctionDBRepository productRepository)
    {
        _logger = logger;
        _productRepository = productRepository;

        var hostName = System.Net.Dns.GetHostName();
        var ips = System.Net.Dns.GetHostAddresses(hostName);
        var _ipaddr = ips.First().MapToIPv4().ToString();
        _logger.LogInformation(1, $"Auction Service responding from {_ipaddr}");
    }

    [HttpGet("GetProductById/{productId}")]
    public Task<Product> GetProductById(Guid productId)
    {
        try
        {
            return _productRepository.GetProductByIdAsync(productId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            throw;
        }
    }

    [HttpGet("GetAllProducts")]
    public Task<IEnumerable<Product>> GetAllProducts()
    {
        try
        {
            return _productRepository.GetAllProductsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            throw;
        }
    }

    [HttpPost("AddProduct")]
public async Task<IActionResult> AddProduct(
    [FromForm] string? Name,
    [FromForm] decimal StartPrice,
    [FromForm] string? Description,
    [FromForm] IFormFile? Image)
    {
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = Name,
            Description = Description,
            StartPrice = StartPrice,
            Image = null, 
            EndOfAuction = DateTime.UtcNow.AddDays(14)
        };

        await _productRepository.CreateProductAsync(product);
        return Ok(product);
    }

[HttpPost("AddImage"), DisableRequestSizeLimit]
public async Task<IActionResult> UploadImage()
{
    List<Uri> images = new List<Uri>();

    try
    {
        var formId = Request.Form["guid"];
        if (string.IsNullOrEmpty(formId))
            return BadRequest("The product id could not be identified.");

        Guid productId = new Guid(formId!);
        var result = await _productRepository.GetProductByIdAsync(productId);

        if (result != null)
        {
            foreach (var formFile in Request.Form.Files)
            {
                if (formFile.Length > 0)
                {
                    var fileName = "image-" + Guid.NewGuid() + ".jpg";
                    var fullPath = Path.Combine(_imagePath, fileName);
                    _logger.LogInformation($"Saving file {fullPath}");

                    using (var stream = new FileStream(fullPath, FileMode.Create))
                    {
                        await formFile.CopyToAsync(stream);
                    }

                    var imageURI = new Uri(fileName, UriKind.RelativeOrAbsolute);

                    bool updated = await _productRepository.AddImageToProductItem(productId, imageURI);
                    if (updated)
                        images.Add(imageURI);
                }
                else
                {
                    return BadRequest("Empty file submitted.");
                }
            }
        }
        else
        {
            return NotFound("Product not found.");
        }
    }
    catch (Exception ex)
    {
        _logger.LogError("Upload image failure: {0}", ex.Message);
        return StatusCode(500, "Internal server error.");
    }

    return Ok(images);
}



    [HttpPut("UpdateProduct/{productId}")]
    public Task<Product> UpdateProduct(string productId, Product product)
    {
        try
        {
            return _productRepository.UpdateProductAsync(productId, product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            throw;
        }
    }

    [HttpDelete("DeleteProduct/{productId}")]
    public Task<bool> DeleteProduct(string productId)
    {
        try
        {
            return _productRepository.DeleteProductAsync(productId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            throw;
        }
    }

    [HttpPost("{productId}/bids")]
    public async Task<IActionResult> PlaceBid(string productId, [FromBody] BidHistory bid)
    {
        if (bid == null || bid.BidAmount <= 0)
            return BadRequest("Invalid bid.");

        var updatedProduct = await _productRepository.AddBidAsync(productId, bid);
        if (updatedProduct == null)
            return NotFound();

        return Ok(updatedProduct);
    }


    [HttpGet("version")]
    public async Task<Dictionary<string, string>> GetVersion()
    {
        var properties = new Dictionary<string, string>();
        var assembly = typeof(Program).Assembly;
        properties.Add("service", "Auction Service");
        var ver = FileVersionInfo.GetVersionInfo(typeof(Program).Assembly.Location).ProductVersion;
        properties.Add("version", ver!);
        try
        {
            var hostName = System.Net.Dns.GetHostName();
            var ips = await System.Net.Dns.GetHostAddressesAsync(hostName);
            var ipa = ips.First().MapToIPv4().ToString();
            properties.Add("hosted-at-address", ipa);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            properties.Add("hosted-at-address", "Could not resolve IP-address");
        }
        return properties;
    }
}
