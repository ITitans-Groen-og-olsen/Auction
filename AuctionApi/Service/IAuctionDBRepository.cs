using Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Services
{
    public interface IAuctionDBRepository
    {
        Task<Product> CreateProductAsync(Product product);
        Task<Product> GetProductByIdAsync(Guid productId);
        Task<IEnumerable<Product>> GetAllProductsAsync();
        Task<Product> UpdateProductAsync(Guid productId, Product updatedProduct);
        Task<bool> DeleteProductAsync(Guid productId);
    }
}
