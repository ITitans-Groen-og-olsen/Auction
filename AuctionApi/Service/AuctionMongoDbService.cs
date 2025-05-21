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
           var connectionString = Environment.GetEnvironmentVariable("MongoConnectionString") 
                                    ?? configuration.GetValue<string>("MongoConnectionString");

        var databaseName = Environment.GetEnvironmentVariable("DatabaseName") 
                           ?? configuration.GetValue<string>("DatabaseName", "AuctionDB");

        var collectionName = Environment.GetEnvironmentVariable("CollectionName") 
                             ?? configuration.GetValue<string>("CollectionName", "Products");

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

        public async Task<Product> GetProductByIdAsync(Guid id)
        {
            return await _products.Find(p => p.Id == id).FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<Product>> GetAllProductsAsync()
        {
            return await _products.Find(_ => true).ToListAsync();
        }

        public async Task<Product> CreateProductAsync(Product product)
        {
            product.Id = Guid.NewGuid();
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
        public async Task<Product> AddBidAsync(string productId, BidHistory bid)
        {
            var filter = Builders<Product>.Filter.Eq(p => p.Id, Guid.Parse(productId));
            var update = Builders<Product>.Update
                .Push(p => p.BidHistory, bid)
                .Set(p => p.currentbid, bid.BidAmount)
                .Set(p => p.CurrentBidder, bid.BidderId);

            var result = await _products.FindOneAndUpdateAsync(filter, update, new FindOneAndUpdateOptions<Product>
            {
                ReturnDocument = ReturnDocument.After
            });

            return result;
        }

    }
}