using Microsoft.Net.Http.Headers;
using Services;

var builder = WebApplication.CreateBuilder(args);

var gatewayUrl = builder.Configuration["GatewayUrl"] ?? "http://localhost:4000/";
builder.Services.AddHttpClient("gateway", client =>
{
client.BaseAddress = new Uri(gatewayUrl);
client.DefaultRequestHeaders.Add(
HeaderNames.Accept, "application/json");
});

builder.Services.AddRazorPages();
builder.Services.AddControllers();
builder.Configuration.AddEnvironmentVariables();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


builder.Services.AddScoped<IAuctionDBRepository, AuctionMongoDBService>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapRazorPages();

app.Run();
