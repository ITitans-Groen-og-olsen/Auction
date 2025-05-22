using Microsoft.Net.Http.Headers;
using Services;
using NLog;
using NLog.Web;

var logger = NLog.LogManager.Setup().LoadConfigurationFromAppSettings()
.GetCurrentClassLogger();

try
{
    logger.Debug("Starting application");

    var builder = WebApplication.CreateBuilder(args);

    // Setup gateway client (used to call backend APIs)
    var gatewayUrl = builder.Configuration["GatewayUrl"] ?? "http://localhost:5001/";
    builder.Services.AddHttpClient("gateway", client =>
    {
        client.BaseAddress = new Uri(gatewayUrl);
        client.DefaultRequestHeaders.Add(HeaderNames.Accept, "application/json");
    });
    Console.WriteLine($"Gateway set to{gatewayUrl}");

    // Razor Pages + Controllers
    builder.Services.AddRazorPages();
    builder.Services.AddControllers();

    // MongoDB repository
    builder.Services.AddScoped<IAuctionDBRepository, AuctionMongoDBService>();

    // Swagger for API testing
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // NLog configuration
    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

    var app = builder.Build();

    // Dev-only Swagger
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    // app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseRouting();
    app.UseAuthorization();

    app.MapControllers();
    app.MapRazorPages();

    app.Run();
}
catch (Exception ex)
{
    // Catch any unhandled exceptions during startup
    logger.Error(ex, "Application stopped due to an exception");
    throw;
}
finally
{
    // Ensure logs are flushed and resources are released
    LogManager.Shutdown();
}
