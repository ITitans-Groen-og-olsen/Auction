using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Models;
using System.Net.Http;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json;
using System.Text;
using System.Net.Http.Headers;

public class ImageUploadModel : PageModel
{
    [BindProperty]
    public string? Name { get; set; }

    [BindProperty]
    public decimal? StartPrice { get; set; }

    [BindProperty]
    public string? Description { get; set; }

    [BindProperty]
    public IFormFile? UploadedImage { get; set; }

    public string Base64Image { get; set; }

    private readonly IHttpClientFactory _clientFactory;

    public ImageUploadModel(IHttpClientFactory clientFactory)
    {
        _clientFactory = clientFactory;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        var client = _clientFactory.CreateClient("gateway");

        // STEP 1: Create Product using form data
        var formContent = new MultipartFormDataContent();
        formContent.Add(new StringContent(Name ?? ""), "Name");
        formContent.Add(new StringContent(StartPrice?.ToString() ?? "0"), "StartPrice");
        formContent.Add(new StringContent(Description ?? ""), "Description");


        var productResponse = await client.PostAsync("Auction/AddProduct", formContent);

        if (!productResponse.IsSuccessStatusCode)
        {
            ModelState.AddModelError("", "Failed to create product.");
            return Page();
        }

        var createdProductJson = await productResponse.Content.ReadAsStringAsync();
        var createdProduct = JsonSerializer.Deserialize<Product>(createdProductJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        // STEP 2: Upload image (same as before)
        if (UploadedImage != null && UploadedImage.Length > 0)
        {
            using var ms = new MemoryStream();
            await UploadedImage.CopyToAsync(ms);
            var imageBytes = ms.ToArray();

            Base64Image = $"data:{UploadedImage.ContentType};base64,{Convert.ToBase64String(imageBytes)}";

            var imageContent = new MultipartFormDataContent();
            imageContent.Add(new StringContent(createdProduct.Id.ToString()), "guid");

            var byteContent = new ByteArrayContent(imageBytes);
            byteContent.Headers.ContentType = new MediaTypeHeaderValue(UploadedImage.ContentType);
            imageContent.Add(byteContent, "formFile", UploadedImage.FileName);

            var imageResponse = await client.PostAsync("Auction/AddImage", imageContent);

            if (!imageResponse.IsSuccessStatusCode)
            {
                ModelState.AddModelError("", "Product created, but image upload failed.");
            }
        }

        return Page();
    }
}