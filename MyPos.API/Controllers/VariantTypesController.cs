using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyPos.Application.Dtos.Product;
using MyPos.Domain.Entities;
using MyPos.Infrastructure.Persistence;
using System.Linq;
using System.Security.Claims; // Bu using'i ekle
using System.Threading.Tasks;

namespace MyPos.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Auth'u aç
    public class VariantTypesController : ControllerBase
    {
        private readonly MyPosDbContext _context;
        private readonly IValidator<CreateVariantTypeDto> _createVariantTypeValidator;
        private readonly IValidator<UpdateVariantTypeDto> _updateVariantTypeValidator;

        public VariantTypesController(
            MyPosDbContext context,
            IValidator<CreateVariantTypeDto> createVariantTypeValidator,
            IValidator<UpdateVariantTypeDto> updateVariantTypeValidator)
        {
            _context = context;
            _createVariantTypeValidator = createVariantTypeValidator;
            _updateVariantTypeValidator = updateVariantTypeValidator;
        }

        // Tüm varyant tiplerini listele
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Sadece mevcut kullanıcıya ait varyant tiplerini getir
            var variantTypes = await _context.VariantTypes
                                             .Where(vt => vt.UserId == currentUserId)
                                             .Select(vt => new VariantTypeResponseDto
                                             {
                                                 Id = vt.Id,
                                                 Name = vt.Name
                                             })
                                             .ToListAsync();
            return Ok(variantTypes);
        }

        // ID'ye göre varyant tipi getir
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Veriyi bulurken hem ID'yi hem de UserId'yi kontrol et
            var variantType = await _context.VariantTypes
                                            .FirstOrDefaultAsync(vt => vt.Id == id && vt.UserId == currentUserId);
            if (variantType == null)
            {
                return NotFound("Varyant tipi bulunamadı veya yetkiniz yok.");
            }

            var response = new VariantTypeResponseDto
            {
                Id = variantType.Id,
                Name = variantType.Name
            };
            return Ok(response);
        }

        // Yeni varyant tipi oluştur
        [HttpPost]
        public async Task<ActionResult<VariantTypeResponseDto>> Create([FromBody] CreateVariantTypeDto dto)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var validationResult = await _createVariantTypeValidator.ValidateAsync(dto);
            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.Errors);
            }

            var variantType = new VariantType
            {
                Name = dto.Name,
                UserId = currentUserId // Kullanıcının ID'sini ekle
            };
            _context.VariantTypes.Add(variantType);
            await _context.SaveChangesAsync();

            var response = new VariantTypeResponseDto
            {
                Id = variantType.Id,
                Name = variantType.Name
            };
            return CreatedAtAction(nameof(GetById), new { id = variantType.Id }, response);
        }

        // Varyant tipi güncelle
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateVariantTypeDto dto)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (id != dto.Id)
            {
                return BadRequest("ID'ler eşleşmiyor.");
            }

            var validationResult = await _updateVariantTypeValidator.ValidateAsync(dto);
            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.Errors);
            }

            // Veriyi bulurken hem ID'yi hem de UserId'yi kontrol et
            var variantType = await _context.VariantTypes
                                            .FirstOrDefaultAsync(vt => vt.Id == id && vt.UserId == currentUserId);
            if (variantType == null)
            {
                return NotFound("Varyant tipi bulunamadı veya yetkiniz yok.");
            }

            variantType.Name = dto.Name;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // Varyant tipi sil
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Veriyi bulurken hem ID'yi hem de UserId'yi kontrol et
            var variantType = await _context.VariantTypes
                                            .FirstOrDefaultAsync(vt => vt.Id == id && vt.UserId == currentUserId);
            if (variantType == null)
            {
                return NotFound("Varyant tipi bulunamadı veya yetkiniz yok.");
            }

            _context.VariantTypes.Remove(variantType);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}