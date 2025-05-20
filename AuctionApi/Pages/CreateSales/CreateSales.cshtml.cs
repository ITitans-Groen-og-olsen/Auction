using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Models;

public class AddProductModel : PageModel
{
    [BindProperty]
    public Product Product { get; set; }

    [BindProperty]
    public IFormFile ImageFile { get; set; }

    public bool Submitted { get; set; }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid || ImageFile == null)
            return Page();

        // Convert image to Base64 string
        using var ms = new MemoryStream();
        await ImageFile.CopyToAsync(ms);
        Product.Image = Convert.ToBase64String(ms.ToArray());

        // Send product to API
        using var httpClient = new HttpClient();
        httpClient.BaseAddress = new Uri("http://auction-service:8080");

        var json = JsonSerializer.Serialize(Product);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await httpClient.PostAsync("/auction", content);

        if (response.IsSuccessStatusCode)
        {
            Submitted = true;
            return Page();
        }

        ModelState.AddModelError("", "Der opstod en fejl ved oprettelse af produktet.");
        return Page();
    }
}