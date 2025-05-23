using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using Models;

public class AddProductModel : PageModel
{
    private readonly IHttpClientFactory _clientFactory;

    public AddProductModel(IHttpClientFactory clientFactory)
    {
        _clientFactory = clientFactory;
    }

    [BindProperty]
    public Product Product { get; set; } = new();

    [BindProperty]
    [Required]
    public IFormFile ImageFile { get; set; }

    public bool Submitted { get; set; }

    public async Task<IActionResult> OnPostAsync()
    {
        Console.WriteLine("ssssssssssssssssssssssssssssssssssssssssssssssssssssss");

        foreach (var entry in ModelState)
        {
            Console.WriteLine($"Key: {entry.Key}, Errors: {entry.Value?.Errors.Count}");
        }
       
        try
        {

            // Convert image to base64
            if (ImageFile != null && ImageFile.Length > 0)
            {
                using var ms = new MemoryStream();
                await ImageFile.CopyToAsync(ms);
                Product.Image = Convert.ToBase64String(ms.ToArray());
            }
            
            var client = _clientFactory.CreateClient("gateway");
            var endpoint = "/auction/AddProduct";

            Console.WriteLine($"This is the endpoint: {endpoint}");

            var response = await client.PostAsJsonAsync(endpoint, Product);

            Console.WriteLine($"This is the endpoint: {endpoint}");
            Console.WriteLine($"This is the endpoint: {Product.Name}");
            


        

            if (response.IsSuccessStatusCode)
            {
                Submitted = true;
                return RedirectToPage("/Admin"); // Or wherever you want to go
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }

        ModelState.AddModelError(string.Empty, "Noget gik galt under oprettelsen.");
        return Page();
    }
}
