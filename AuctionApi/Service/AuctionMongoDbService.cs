using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using Models;

namespace Services
{
    public class AuctionMongoDBService : IAuctionDBRepository
    {
        private readonly IMongoCollection<Product> _products;
        private readonly ILogger<AuctionMongoDBService> _logger;

        public AuctionMongoDBService(ILogger<AuctionMongoDBService> logger, IConfiguration configuration)
        {
            _logger = logger;
            var connectionString = configuration["MongoConnectionString"] ?? "<blank>";
            var databaseName = configuration["DatabaseName"] ?? "<blank>";
            var collectionName = configuration["CollectionName"] ?? "Auctions";
            _logger.LogInformation($"Connected to MongoDB using: {connectionString}");
            _logger.LogInformation($" Using database: {databaseName}");
            _logger.LogInformation($" Using Collection: {collectionName}");

            try
            {
                var client = new MongoClient(connectionString);
                var database = client.GetDatabase(databaseName);
                _products = database.GetCollection<Product>(collectionName);
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to connect to MongoDB: {0}", ex.Message);
            }
        }

        public async Task<Product> GetProductByIdAsync(string id)
        {
            return await _products.Find(p => p.Id.ToString() == id).FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<Product>> GetAllProductsAsync()
        {
            return await _products.Find(_ => true).ToListAsync();
        }

        public async Task<Product> CreateProductAsync(Product product)
        {
            product.Id = Guid.NewGuid(); // Ensure a new GUID is generated if not set
            await _products.InsertOneAsync(product);
            return product;
        }

        public async Task<Product> UpdateProductAsync(string id, Product updatedProduct)
        {
            await _products.ReplaceOneAsync(p => p.Id.ToString() == id, updatedProduct);
            return updatedProduct;
        }

        public async Task<bool> DeleteProductAsync(string id)
        {
            var result = await _products.DeleteOneAsync(p => p.Id.ToString() == id);
            return result.DeletedCount > 0;
        }
    }
}