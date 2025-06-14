using LTWNC.Data;
using LTWNC.Models.Entities;
using LTWNC.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Text.Json;

namespace LTWNC.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly IMongoCollection<Product> _products;

        public ProductsController(MongoDbService mongoDbService)
        {
            _products =  mongoDbService.Database?.GetCollection<Product>("Products")
                ?? throw new ArgumentNullException(nameof(mongoDbService.Database), "MongoDB Database is null");
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreateProductWithImages(
            [FromForm] string name,
            [FromForm] decimal price,
            [FromForm] string? description,
            [FromForm] string variantsJson,
            [FromForm] List<IFormFile> images,
            [FromServices] IImageService imageService)
        {
            // Parse variants từ JSON
            List<ProductVariant> variants;
            try
            {
                variants = JsonSerializer.Deserialize<List<ProductVariant>>(variantsJson);
            }
            catch
            {
                return BadRequest("Invalid variants format");
            }

            // Upload ảnh qua service
            var imageUrls = await imageService.UploadImagesAsync(images, "products");

            // Tạo object và lưu MongoDB
            var product = new Product
            {
                Name = name,
                Price = price,
                Description = description,
                Images = imageUrls,
                Variants = variants
            };

            await _products.InsertOneAsync(product);
            return Ok(new { message = "Product created", productId = product.Id });
        }

        [HttpGet]
        public async Task<IActionResult> GetPagedProducts(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 2)
        {
            if (page <= 0 || pageSize <= 0)
                return BadRequest("Page and PageSize must be greater than 0");

            var skip = (page - 1) * pageSize;

            var totalCount = await _products.CountDocumentsAsync(Builders<Product>.Filter.Empty);

            var products = await _products
                .Find(Builders<Product>.Filter.Empty)
                .SortByDescending(p => p.CreatedAt)
                .Skip(skip)
                .Limit(pageSize)
                .ToListAsync();

            return Ok(new
            {
                currentPage = page,
                pageSize = pageSize,
                totalItems = totalCount,
                totalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                items = products
            });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct([FromRoute] string id)
        {
            var result = await _products.DeleteOneAsync(p => p.Id == id);

            if (result.DeletedCount == 0)
                return NotFound(new { message = "Product not found" });

            return Ok(new { message = "Product deleted successfully" });
        }


        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(
            [FromRoute] string id,
            [FromBody] Product updatedProduct)
        {
            var filter = Builders<Product>.Filter.Eq(p => p.Id, id);

            var update = Builders<Product>.Update
                .Set(p => p.Name, updatedProduct.Name)
                .Set(p => p.Price, updatedProduct.Price)
                .Set(p => p.Description, updatedProduct.Description)
                .Set(p => p.Variants, updatedProduct.Variants)
                .Set(p => p.Images, updatedProduct.Images);
            var result = await _products.UpdateOneAsync(filter, update);

            if (result.MatchedCount == 0)
                return NotFound(new { message = "Product not found" });

            return Ok(new { message = "Product updated successfully" });
        }

    }
}
