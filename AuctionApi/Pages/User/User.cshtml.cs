using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyApp.Namespace
{
    public class UserModel : PageModel
    {
        private readonly IHttpClientFactory _clientFactory;

        public UserModel(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        [BindProperty]
        public UserDto User { get; set; } = new();

        public List<BidDto> ActiveBids { get; set; } = new();
        public bool LoadError { get; set; }

        public async Task OnGetAsync()
        {
            string userId = "test-user-id"; // TODO: Replace with session or claims-based ID

            var client = _clientFactory.CreateClient("gateway");

            try
            {
                var userResponse = await client.GetAsync($"/User/GetUserById/{userId}");
                if (userResponse.IsSuccessStatusCode)
                {
                    User = await userResponse.Content.ReadFromJsonAsync<UserDto>();
                }
                else
                {
                    LoadError = true;
                }

                var bidsResponse = await client.GetAsync($"/Auction/GetBidsByUser/{userId}");
                if (bidsResponse.IsSuccessStatusCode)
                {
                    ActiveBids = await bidsResponse.Content.ReadFromJsonAsync<List<BidDto>>();
                }
                else
                {
                    LoadError = true;
                }
            }
            catch
            {
                LoadError = true;
            }
        }

        public async Task<IActionResult> OnPostUpdateProfileAsync()
        {
            var client = _clientFactory.CreateClient("gateway");

            var response = await client.PutAsJsonAsync($"/User/UpdateUser/{User.Id}", User);
            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError("", "Failed to update profile.");
                return Page();
            }

            return RedirectToPage();
        }

        public class UserDto
        {
            public string Id { get; set; }
            public string? FirstName { get; set; }
            public string? LastName { get; set; }
            public string? Address { get; set; }
            public short PostalCode { get; set; }
            public string? City { get; set; }
            public string? PhoneNumber { get; set; }
            public string? Email { get; set; }
        }

        public class BidDto
        {
            public string AuctionTitle { get; set; }
            public decimal YourBid { get; set; }
            public decimal HighestBid { get; set; }
            public string Status { get; set; }
            public string TimeLeft { get; set; }
        }
    }
}
