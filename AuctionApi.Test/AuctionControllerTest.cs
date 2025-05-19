using Moq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Models;
using Microsoft.Extensions.Logging;
using Services;
using AuctionService.Controllers;
using System.Threading.Tasks;

namespace AuctionApi.Test;

[TestClass]
public class AuctionTest
{
    [TestClass]
    public class AuctionControllerTests
    {
        private Mock<ILogger<AuctionController>> _mockLogger = null!;
        private Mock<IAuctionDBRepository> _mockAuctionDBRepository;
        private AuctionController _controller;


        [TestInitialize]
        public void Setup()
        {
            _mockLogger = new Mock<ILogger<AuctionController>>();
            _mockAuctionDBRepository = new Mock<IAuctionDBRepository>();
            _controller = new AuctionController(_mockLogger.Object, _mockAuctionDBRepository.Object);
        }

        [TestMethod]
        public async Task GetProductById_ReturnsProduct()
        {
            // Arrange
            var productId = Guid.NewGuid();
            var product = new Product
            {
                Id = productId,
                Name = "Test Product",
                Description = "Test Description",
                StartPrice = 100,
                currentbid = 150,
                Brand = "TestBrand",
                Image = "test.jpg",
                EndOfAuction = DateTime.UtcNow.AddDays(1),
                CurrentBidder = 42
            };

            _mockAuctionDBRepository
                .Setup(repo => repo.GetProductByIdAsync(productId))
                .ReturnsAsync(product);

            // Act
            var result = await _controller.GetProductById(productId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(productId, result.Id);
            Assert.AreEqual("Test Product", result.Name);
        }

        [TestMethod]
        public async Task AddProduct_ReturnsCreatedProduct()
        {
            // Arrange
            var inputProduct = new Product
            {
                Id = Guid.NewGuid(),
                Name = "New Product",
                StartPrice = 75,
                Image = "new.jpg",
                EndOfAuction = DateTime.UtcNow.AddHours(12)
            };

            _mockAuctionDBRepository
                .Setup(repo => repo.CreateProductAsync(inputProduct))
                .ReturnsAsync(inputProduct);

            // Act
            var result = await _controller.AddProduct(inputProduct);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(inputProduct.Id, result.Id);
            Assert.AreEqual("New Product", result.Name);
        }

        [TestMethod]
        public async Task GetAllProducts_ReturnsProductList()
        {
            // Arrange
            var products = new List<Product>
    {
        new Product
        {
            Id = Guid.NewGuid(),
            Name = "Product A",
            StartPrice = 10,
            Image = "a.jpg",
            EndOfAuction = DateTime.UtcNow.AddHours(5)
        },
        new Product
        {
            Id = Guid.NewGuid(),
            Name = "Product B",
            StartPrice = 20,
            Image = "b.jpg",
            EndOfAuction = DateTime.UtcNow.AddHours(6)
        }
    };

            _mockAuctionDBRepository
                .Setup(repo => repo.GetAllProductsAsync())
                .ReturnsAsync(products);

            // Act
            var result = await _controller.GetAllProducts();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, ((List<Product>)result).Count);
        }
        [TestMethod]
        public async Task UpdateProduct_ReturnsUpdatedProduct()
        {
            // Arrange
            var productId = Guid.NewGuid();
            var updatedProduct = new Product
            {
                Id = productId,
                Name = "Updated Product",
                Description = "Updated Description",
                StartPrice = 200,
                currentbid = 250,
                Brand = "UpdatedBrand",
                Image = "updated.jpg",
                EndOfAuction = DateTime.UtcNow.AddDays(3),
                CurrentBidder = 101
            };
            _mockAuctionDBRepository
                .Setup(repo => repo.UpdateProductAsync(productId.ToString(), updatedProduct))
                .ReturnsAsync(updatedProduct);

            // Act
            var result = await _controller.UpdateProduct(productId.ToString(), updatedProduct);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(productId, result.Id);
            Assert.AreEqual("Updated Product", result.Name);
            Assert.AreEqual("UpdatedBrand", result.Brand);
        }
        [TestMethod]
        public async Task DeleteProduct_ReturnsTrue()
        {
            // Arrange
            var productId = Guid.NewGuid().ToString();


            _mockAuctionDBRepository
                .Setup(repo => repo.DeleteProductAsync(productId))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.DeleteProduct(productId);

            // Assert
            Assert.IsTrue(result);
        }
    }
}