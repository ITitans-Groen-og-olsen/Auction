using MongoDB.Driver;
using Microsoft.Extensions.Logging;
using Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Services
{
    public class AuctionMongoDBService : IAuctionDBRepository
    {
        private readonly IMongoCollection<Product> _productCollection;
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
                _productCollection = database.GetCollection<Product>(collectionName);
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to connect to MongoDB: {0}", ex.Message);
            }
        }

        public async Task<Product> CreateProductAsync(Product product) =>
            await Task.Run(async () => { await _productCollection.InsertOneAsync(product); return product; });

        public async Task<Product> GetProductByIdAsync(Guid productId) =>
            await _productCollection.Find(p => p.Id == productId).FirstOrDefaultAsync();

        public async Task<IEnumerable<Product>> GetAllProductsAsync() =>
            await _productCollection.Find(_ => true).ToListAsync();

        public async Task<Product> UpdateProductAsync(Guid productId, Product updatedProduct)
        {
            var result = await _productCollection.ReplaceOneAsync(p => p.Id == productId, updatedProduct);
            return result.MatchedCount > 0 ? updatedProduct : null;
        }

        public async Task<bool> DeleteProductAsync(Guid productId)
        {
            var result = await _productCollection.DeleteOneAsync(p => p.Id == productId);
            return result.DeletedCount > 0;
        }
    }
}
