using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using System.Text;
using System.Text.Json;

namespace MyApp.Namespace
{
    public class UserModel : PageModel
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly ILogger<UserModel> _logger;

        public UserModel(IHttpClientFactory clientFactory, ILogger<UserModel> logger)
        {
            _clientFactory = clientFactory;
            _logger = logger;
        }

        public UserData? User { get; set; }
        public List<ProductModel> ActiveBids { get; set; } = new();

        [BindProperty]
        public UpdateUserModel UpdateForm { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = HttpContext.Session.GetString("userId");
            var jwtToken = HttpContext.Session.GetString("jwtToken");

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(jwtToken))
                return Redirect("/Login");

            var client = _clientFactory.CreateClient("gateway");
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken);

            var userResponse = await client.GetAsync($"/User/GetUserById/{userId}");
            if (!userResponse.IsSuccessStatusCode || userResponse.StatusCode == System.Net.HttpStatusCode.NoContent)
                return Redirect("/Login");

            User = await userResponse.Content.ReadFromJsonAsync<UserData>();
            if (User is null)
                return Redirect("/Login");

            UpdateForm = new UpdateUserModel
            {
                Id = User.Id,
                FirstName = User.FirstName,
                LastName = User.LastName,
                EmailAddress = User.EmailAddress
            };

            var productResponse = await client.GetAsync("/auction/GetAllProducts");
            if (!productResponse.IsSuccessStatusCode)
                return Page();

            var products = await productResponse.Content.ReadFromJsonAsync<List<ProductModel>>();
            if (products is null)
                return Page();

            ActiveBids = products
                .Where(p => p.CurrentBidder == User.CustomerNumber)
                .ToList();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            _logger.LogInformation("🛠️ OnPostAsync triggered with form: {@UpdateForm}", UpdateForm);

            var client = _clientFactory.CreateClient("gateway");

            // Get existing user from API
            var getResponse = await client.GetAsync($"/User/GetUserById/{UpdateForm.Id}");
            if (!getResponse.IsSuccessStatusCode || getResponse.StatusCode == System.Net.HttpStatusCode.NoContent)
            {
                ModelState.AddModelError("", "Could not load user data.");
                return await OnGetAsync();
            }

            var fullUser = await getResponse.Content.ReadFromJsonAsync<UserData>();
            if (fullUser == null)
            {
                ModelState.AddModelError("", "User not found.");
                return await OnGetAsync();
            }

            _logger.LogInformation("👤 Existing user from API: {@fullUser}", fullUser);

            // Update fields from form
            fullUser.FirstName = UpdateForm.FirstName;
            fullUser.LastName = UpdateForm.LastName;
            fullUser.EmailAddress = UpdateForm.EmailAddress;

            fullUser.Password ??= ""; // fallback to avoid breaking update
            fullUser.CustomerNumber = fullUser.CustomerNumber;

            // Send as JSON
            var json = JsonSerializer.Serialize(fullUser);
            _logger.LogInformation("📦 Sending JSON to User API: {json}", json);

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var putResponse = await client.PutAsync($"/User/UpdateUser/{fullUser.Id}", content);

            if (putResponse.IsSuccessStatusCode)
            {
                _logger.LogInformation("✅ Update successful for user {UserId}", fullUser.Id);
                return Redirect("/auction/User");
            }

            _logger.LogWarning("❌ Update failed for user {UserId}", fullUser.Id);
            ModelState.AddModelError("", "Update failed.");
            return await OnGetAsync();
        }

        public class UpdateUserModel
        {
            public Guid Id { get; set; }
            public string? FirstName { get; set; }
            public string? LastName { get; set; }
            public string? EmailAddress { get; set; }
        }

        public class UserData
        {
            public Guid Id { get; set; }
            public int CustomerNumber { get; set; }
            public string? FirstName { get; set; }
            public string? LastName { get; set; }
            public string? EmailAddress { get; set; }
            public string? Password { get; set; }
        }

        public class ProductModel
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
            public decimal? CurrentBid { get; set; }
            public DateTime EndOfAuction { get; set; }
            public int? CurrentBidder { get; set; }
            public List<BidHistory>? BidHistory { get; set; }
        }

        public class BidHistory
        {
            public int BidderId { get; set; }
            public decimal BidAmount { get; set; }
            public DateTime BidTime { get; set; }
        }
    }
}
