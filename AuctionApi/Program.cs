using Microsoft.Net.Http.Headers;
using Services;

var builder = WebApplication.CreateBuilder(args);

// Setup gateway client (used to call backend APIs)
var gatewayUrl = builder.Configuration["GatewayUrl"] ?? "http://localhost:4000/";
builder.Services.AddHttpClient("gateway", client =>
{
    client.BaseAddress = new Uri(gatewayUrl);
    client.DefaultRequestHeaders.Add(HeaderNames.Accept, "application/json");
});

// Razor Pages + Controllers
builder.Services.AddRazorPages();
builder.Services.AddControllers();

// MongoDB repository
builder.Services.AddScoped<IAuctionDBRepository, AuctionMongoDBService>();

// Swagger for API testing
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Dev-only Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Redirect HTTP -> HTTPS
app.UseHttpsRedirection();

// Optional: use static files (e.g., images, CSS)
app.UseStaticFiles();

// Enable authentication (even without Identity, so you can still simulate logged-in state via cookies or middleware)
app.UseRouting();
app.UseAuthorization();

// Route endpoints
app.MapControllers();
app.MapRazorPages();

app.Run();
