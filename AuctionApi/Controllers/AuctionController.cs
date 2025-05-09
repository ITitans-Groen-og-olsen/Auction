using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Services;
using System.Diagnostics;

[ApiController]
[Route("[controller]")]
public class AuctionController : ControllerBase
{
    private readonly ILogger<AuctionController> _logger;
    private readonly IAuctionDBRepository _auctionRepository;

    public AuctionController(IAuctionDBRepository auctionRepository, ILogger<AuctionController> logger)
    {
        _auctionRepository = auctionRepository;
        _logger = logger;

        try
        {
            var hostName = Dns.GetHostName();
            var ips = Dns.GetHostAddresses(hostName);
            var ipAddr = ips.FirstOrDefault()?.MapToIPv4().ToString();
            _logger.LogInformation($"Auction Service responding from {ipAddr}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving IP address.");
        }
    }

    // GET: /auction/{productId}
    [HttpGet("{productId}", Name = "GetAuctionItemById")]
    public async Task<ActionResult<Product>> Get(Guid productId)
    {
        try
        {
            var product = await _auctionRepository.GetProductByIdAsync(productId);
            if (product == null)
            {
                _logger.LogWarning($"Auction item with ID {productId} not found.");
                return NotFound();
            }

            return Ok(product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching auction item by ID.");
            return StatusCode(500, "Internal server error");
        }
    }

    // GET: /auction/
    [HttpGet(Name = "GetAllAuctionItems")]
    public async Task<ActionResult<List<Product>>> GetAll()
    {
        try
        {
            var products = await _auctionRepository.GetAllProductsAsync();
            return Ok(products);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching all auction items.");
            return StatusCode(500, "Internal server error");
        }
    }

    // POST: /auction/
    [HttpPost]
    public async Task<ActionResult<Product>> Create([FromBody] Product product)
    {
        try
        {
            var createdProduct = await _auctionRepository.CreateProductAsync(product);
            return CreatedAtAction(nameof(Get), new { productId = createdProduct.Id }, createdProduct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating new auction item.");
            return StatusCode(500, "Internal server error");
        }
    }

    // PUT: /auction/{productId}
    [HttpPut("{productId}")]
    public async Task<ActionResult<Product>> Update(Guid productId, [FromBody] Product updatedProduct)
    {
        try
        {
            var product = await _auctionRepository.GetProductByIdAsync(productId);
            if (product == null)
            {
                _logger.LogWarning($"Auction item with ID {productId} not found.");
                return NotFound();
            }

            updatedProduct.Id = productId;
            var result = await _auctionRepository.UpdateProductAsync(productId, updatedProduct);
            if (result == null)
            {
                return NotFound();
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating auction item.");
            return StatusCode(500, "Internal server error");
        }
    }

    // DELETE: /auction/{productId}
    [HttpDelete("{productId}")]
    public async Task<IActionResult> Delete(Guid productId)
    {
        try
        {
            var success = await _auctionRepository.DeleteProductAsync(productId);
            if (!success)
            {
                _logger.LogWarning($"Auction item with ID {productId} not found.");
                return NotFound();
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting auction item.");
            return StatusCode(500, "Internal server error");
        }
    }

    // GET: /auction/version
    [HttpGet("version")]
    public async Task<ActionResult<Dictionary<string, string>>> GetVersion()
    {
        var properties = new Dictionary<string, string>
        {
            ["service"] = "Auction Service"
        };

        try
        {
            var ver = FileVersionInfo.GetVersionInfo(typeof(Program).Assembly.Location).ProductVersion;
            properties["version"] = ver ?? "Unknown";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve version.");
            properties["version"] = "Error";
        }

        try
        {
            var hostName = Dns.GetHostName();
            var ips = await Dns.GetHostAddressesAsync(hostName);
            var ip = ips.FirstOrDefault()?.MapToIPv4().ToString();
            properties["hosted-at-address"] = ip ?? "Unknown";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resolve IP address.");
            properties["hosted-at-address"] = "Could not resolve IP-address";
        }

        return properties;
    }
}
