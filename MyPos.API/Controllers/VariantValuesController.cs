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
using System.Collections.Generic;

namespace MyPos.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Auth'u aç ve tüm controller için geçerli kıl
    public class VariantValuesController : ControllerBase
    {
        private readonly MyPosDbContext _context;
        private readonly IValidator<CreateVariantValueDto> _createVariantValueValidator;
        private readonly IValidator<UpdateVariantValueDto> _updateVariantValueValidator;

        public VariantValuesController(
            MyPosDbContext context,
            IValidator<CreateVariantValueDto> createVariantValueValidator,
            IValidator<UpdateVariantValueDto> updateVariantValueValidator)
        {
            _context = context;
            _createVariantValueValidator = createVariantValueValidator;
            _updateVariantValueValidator = updateVariantValueValidator;
        }

        // Tüm varyant değerlerini listele (veya belirli bir varyant tipine göre)
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int? variantTypeId = null)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var query = _context.VariantValues
                .Include(vv => vv.VariantType)
                .Where(vv => vv.UserId == currentUserId) // Sadece mevcut kullanıcıya ait olanları getir
                .AsQueryable();

            if (variantTypeId.HasValue)
            {
                // Belirli bir varyant tipine göre filtrelerken yetki kontrolü
                var variantType = await _context.VariantTypes.FirstOrDefaultAsync(vt => vt.Id == variantTypeId.Value && vt.UserId == currentUserId);
                if (variantType == null)
                {
                    return NotFound("Varyant tipi bulunamadı veya yetkiniz yok.");
                }
                query = query.Where(vv => vv.VariantTypeId == variantTypeId.Value);
            }

            var variantValues = await query
                .Select(vv => new VariantValueResponseDto
                {
                    Id = vv.Id,
                    Value = vv.Value,
                    VariantTypeId = vv.VariantTypeId,
                    VariantTypeName = vv.VariantType != null ? vv.VariantType.Name : string.Empty
                })
                .ToListAsync();

            return Ok(variantValues);
        }

        // ID'ye göre varyant değeri getir
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var variantValue = await _context.VariantValues
                .Include(vv => vv.VariantType)
                .FirstOrDefaultAsync(vv => vv.Id == id && vv.UserId == currentUserId); // Yetki kontrolü

            if (variantValue == null)
            {
                return NotFound("Varyant değeri bulunamadı veya yetkiniz yok.");
            }

            var response = new VariantValueResponseDto
            {
                Id = variantValue.Id,
                Value = variantValue.Value,
                VariantTypeId = variantValue.VariantTypeId,
                VariantTypeName = variantValue.VariantType != null ? variantValue.VariantType.Name : string.Empty
            };
            return Ok(response);
        }

        // Yeni varyant değeri oluştur
        [HttpPost]
        public async Task<ActionResult<VariantValueResponseDto>> Create([FromBody] CreateVariantValueDto dto)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var validationResult = await _createVariantValueValidator.ValidateAsync(dto);
            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.Errors);
            }

            // VariantType'ın varlığını ve kullanıcıya ait olduğunu kontrol edin
            var variantType = await _context.VariantTypes
                .FirstOrDefaultAsync(vt => vt.Id == dto.VariantTypeId && vt.UserId == currentUserId);
            if (variantType == null)
            {
                return BadRequest("Belirtilen varyant tipi bulunamadı veya yetkiniz yok.");
            }

            var variantValue = new VariantValue
            {
                Value = dto.Value,
                VariantTypeId = dto.VariantTypeId,
                UserId = currentUserId // Kullanıcının ID'sini ekle
            };
            _context.VariantValues.Add(variantValue);
            await _context.SaveChangesAsync();

            var response = new VariantValueResponseDto
            {
                Id = variantValue.Id,
                Value = variantValue.Value,
                VariantTypeId = variantValue.VariantTypeId,
                VariantTypeName = variantType.Name
            };
            return CreatedAtAction(nameof(GetById), new { id = variantValue.Id }, response);
        }

        // Varyant değeri güncelle
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateVariantValueDto dto)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (id != dto.Id)
            {
                return BadRequest("ID'ler eşleşmiyor.");
            }

            var validationResult = await _updateVariantValueValidator.ValidateAsync(dto);
            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.Errors);
            }

            // Varyant değerini ve bağlı olduğu tipi yetki kontrolüyle bul
            var variantValue = await _context.VariantValues
                .Include(vv => vv.VariantType)
                .FirstOrDefaultAsync(vv => vv.Id == id && vv.UserId == currentUserId);
            if (variantValue == null)
            {
                return NotFound("Varyant değeri bulunamadı veya yetkiniz yok.");
            }

            // VariantType'ın varlığını ve kullanıcıya ait olduğunu kontrol edin (eğer değişiyorsa)
            if (variantValue.VariantTypeId != dto.VariantTypeId)
            {
                var newVariantType = await _context.VariantTypes
                    .FirstOrDefaultAsync(vt => vt.Id == dto.VariantTypeId && vt.UserId == currentUserId);
                if (newVariantType == null)
                {
                    return BadRequest("Belirtilen yeni varyant tipi bulunamadı veya yetkiniz yok.");
                }
                variantValue.VariantType = newVariantType;
            }

            variantValue.Value = dto.Value;
            variantValue.VariantTypeId = dto.VariantTypeId;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // Varyant değeri sil
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Veriyi bulurken hem ID'yi hem de UserId'yi kontrol et
            var variantValue = await _context.VariantValues
                .FirstOrDefaultAsync(vv => vv.Id == id && vv.UserId == currentUserId);

            if (variantValue == null)
            {
                return NotFound("Varyant değeri bulunamadı veya yetkiniz yok.");
            }

            _context.VariantValues.Remove(variantValue);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}