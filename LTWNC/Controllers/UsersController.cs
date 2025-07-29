using LTWNC.Data;
using LTWNC.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace LTWNC.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IMongoCollection<Users> _users;

        public UsersController(MongoDbService mongoDbService)
        {
            _users = mongoDbService.Database.GetCollection<Users>("users");
        }
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> GetPagedUsers(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            if (page <= 0 || pageSize <= 0)
                return BadRequest("Page và PageSize phải lớn hơn 0");

            var filter = Builders<Users>.Filter.Ne(u => u.Role, Role.Admin);
            var skip = (page - 1) * pageSize;
            var totalCount = await _users.CountDocumentsAsync(filter);

            var users = await _users
                .Find(filter)
                .SortByDescending(u => u.CreatedAt)
                .Skip(skip)
                .Limit(pageSize)
                .ToListAsync();

            return Ok(new
            {
                currentPage = page,
                pageSize = pageSize,
                totalItems = totalCount,
                totalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                items = users
            });
        }

        // GET: /api/users/all
        [Authorize(Roles = "Admin")]
        [HttpGet("all")]
        public async Task<IEnumerable<Users>> GetAllUsers()
        {
            var filter = Builders<Users>.Filter.Ne(u => u.Role, Role.Admin);
            return await _users
                .Find(filter)
                .SortByDescending(u => u.CreatedAt)
                .ToListAsync();
        }

        // GET: /api/users/{id}
        [Authorize]
        [HttpGet("{id}")]
        public async Task<ActionResult<Users>> GetById(string id)
        {
            var user = await _users.Find(u => u.Id == id).FirstOrDefaultAsync();
            if (user == null)
                return NotFound($"Không tìm thấy người dùng với ID {id}");

            return Ok(user);
        }

        // POST: /api/users
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Users user)
        {
            try
            {
                if (user.Role == Role.Admin)
                    return BadRequest("Không thể tạo tài khoản với vai trò Admin");

                if (user.CreatedAt == default)
                    user.CreatedAt = DateTime.UtcNow;

                await _users.InsertOneAsync(user);
                return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi khi tạo user: {ex.Message}");
            }
        }

        // PUT: /api/users/{id}
        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] Users updated)
        {
            var existing = await _users.Find(u => u.Id == id).FirstOrDefaultAsync();
            if (existing == null)
                return NotFound($"Không tìm thấy user với ID {id}");

            if (existing.Role == Role.Admin)
                return BadRequest("Không thể chỉnh sửa tài khoản Admin");

            updated.Id = id;
            var result = await _users.ReplaceOneAsync(u => u.Id == id, updated);
            return Ok(new { message = "Cập nhật thành công" });
        }

        // DELETE: /api/users/{id}
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _users.Find(u => u.Id == id).FirstOrDefaultAsync();
            if (user == null)
                return NotFound($"Không tìm thấy user với ID {id}");

            if (user.Role == Role.Admin)
                return BadRequest("Không thể xoá tài khoản Admin");

            var result = await _users.DeleteOneAsync(u => u.Id == id);
            return Ok(new { message = "Xoá thành công" });
        }
    }
}
