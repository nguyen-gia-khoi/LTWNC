using LTWNC.Data;
using LTWNC.Models.Entities;
using LTWNC.Services;
using Microsoft.AspNetCore.Authorization;
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
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreateProductWithImages(
            [FromForm] string name,
            [FromForm] decimal price,
            [FromForm] string? description,
            [FromForm] string variantsJson,
            [FromForm] List<IFormFile> images,
            [FromForm] string? categoryId,
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
                Variants = variants,
                CategoryId = categoryId,
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
        
        [HttpGet("{id}")]
        public async Task<ActionResult<Product?>> GetProductById( string id)
        {
            try
            {
                var filter = Builders<Product>.Filter.Eq(x => x.Id, id);
                var customer = await _products.Find(filter).FirstOrDefaultAsync();
                return customer is not null ? Ok(customer) : NotFound();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving customer: {ex.Message}");
            }
        }
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct([FromRoute] string id)
        {
            var result = await _products.DeleteOneAsync(p => p.Id == id);

            if (result.DeletedCount == 0)
                return NotFound(new { message = "Product not found" });

            return Ok(new { message = "Product deleted successfully" });
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdateProduct(
            [FromRoute] string id,
            [FromForm] string name,
            [FromForm] decimal price,
            [FromForm] string? description,
            [FromForm] string variantsJson,
            [FromForm] List<IFormFile> images,
            [FromForm] string? categoryId,
            [FromServices] IImageService imageService)
        {
            var existing = await _products.Find(p => p.Id == id).FirstOrDefaultAsync();
            if (existing == null)
                return NotFound(new { message = "Product not found" });

            // Parse variants
            List<ProductVariant> variants;
            try
            {
                variants = JsonSerializer.Deserialize<List<ProductVariant>>(variantsJson);
            }
            catch
            {
                return BadRequest("Invalid variants format");
            }

            // Upload ảnh mới nếu có
            var imageUrls = images.Count > 0
                ? await imageService.UploadImagesAsync(images, "products")
                : existing.Images;

            var update = Builders<Product>.Update
                .Set(p => p.Name, name)
                .Set(p => p.Price, price)
                .Set(p => p.Description, description)
                .Set(p => p.CategoryId, categoryId)
                .Set(p => p.Variants, variants)
                .Set(p => p.Images, imageUrls);

            var result = await _products.UpdateOneAsync(p => p.Id == id, update);
            return Ok(new { message = "Updated successfully" });
        }
    }
}
