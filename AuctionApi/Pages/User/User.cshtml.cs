using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using System.Text;
using System.Text.Json;

namespace MyApp.Namespace
{
    public class UserDashboardModel : PageModel
    {
        private readonly IHttpClientFactory _clientFactory;

        public UserDashboardModel(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        public UserModel? User { get; set; }
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
            if (!userResponse.IsSuccessStatusCode)
                return Redirect("/Login");

            User = await userResponse.Content.ReadFromJsonAsync<UserModel>();
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

        public async Task<IActionResult> OnPostUpdateAsync()
        {
            Console.WriteLine("✅ OnPostUpdateAsync called");

            var client = _clientFactory.CreateClient("gateway");

            // Get full user first
            var getResponse = await client.GetAsync($"/User/GetUserById/{UpdateForm.Id}");
            if (!getResponse.IsSuccessStatusCode)
            {
                ModelState.AddModelError("", "Could not load user data.");
                return await OnGetAsync();
            }

            var fullUser = await getResponse.Content.ReadFromJsonAsync<UserModel>();
            if (fullUser == null)
            {
                ModelState.AddModelError("", "User not found.");
                return await OnGetAsync();
            }

            fullUser.FirstName = UpdateForm.FirstName;
            fullUser.LastName = UpdateForm.LastName;
            fullUser.EmailAddress = UpdateForm.EmailAddress;

            var json = JsonSerializer.Serialize(fullUser);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var putResponse = await client.PutAsync($"/User/UpdateUser/{fullUser.Id}", content);

            if (putResponse.IsSuccessStatusCode)
                return RedirectToPage();

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

        public class UserModel
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
