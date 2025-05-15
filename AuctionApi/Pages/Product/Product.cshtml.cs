using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Models;
using Services;

public class ProductModel : PageModel
{
    private readonly IAuctionDBRepository _repository;
    private readonly HttpClient _httpClient;

    public ProductModel(IAuctionDBRepository repository, IHttpClientFactory httpClientFactory)
    {
        _repository = repository;
        _httpClient = httpClientFactory.CreateClient("gateway");
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    public Product? Product { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        Product = await _repository.GetProductByIdAsync(Id);
        if (Product == null)
        {
            return NotFound();
        }

        return Page();
    }
}
