using Models;

namespace Services
{
    public interface IAuctionDBRepository
    {
        Task<Product> GetProductByIdAsync(Guid id);
        Task<IEnumerable<Product>> GetAllProductsAsync();
        Task<Product> CreateProductAsync(Product product);
        Task<Product> UpdateProductAsync(string id, Product updatedProduct);
        Task<bool> DeleteProductAsync(string id);
        Task<Product> AddBidAsync(Guid productId, BidHistory bid);

    }
}
