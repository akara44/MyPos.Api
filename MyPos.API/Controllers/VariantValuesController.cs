using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyPos.Application.Dtos.Product;
using MyPos.Domain.Entities;
using MyPos.Infrastructure.Persistence;

namespace MyPos.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
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
            var query = _context.VariantValues.Include(vv => vv.VariantType).AsQueryable();

            if (variantTypeId.HasValue)
            {
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
            var variantValue = await _context.VariantValues.Include(vv => vv.VariantType).FirstOrDefaultAsync(vv => vv.Id == id);
            if (variantValue == null)
            {
                return NotFound();
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
            var validationResult = await _createVariantValueValidator.ValidateAsync(dto);
            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.Errors);
            }

            // VariantType'ın varlığını kontrol edin
            var variantType = await _context.VariantTypes.FindAsync(dto.VariantTypeId);
            if (variantType == null)
            {
                return BadRequest("Belirtilen varyant tipi bulunamadı.");
            }

            var variantValue = new VariantValue
            {
                Value = dto.Value,
                VariantTypeId = dto.VariantTypeId
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
            if (id != dto.Id)
            {
                return BadRequest("ID'ler eşleşmiyor.");
            }

            var validationResult = await _updateVariantValueValidator.ValidateAsync(dto);
            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.Errors);
            }

            var variantValue = await _context.VariantValues.Include(vv => vv.VariantType).FirstOrDefaultAsync(vv => vv.Id == id);
            if (variantValue == null)
            {
                return NotFound();
            }

            // VariantType'ın varlığını kontrol edin (eğer değişiyorsa)
            if (variantValue.VariantTypeId != dto.VariantTypeId)
            {
                var newVariantType = await _context.VariantTypes.FindAsync(dto.VariantTypeId);
                if (newVariantType == null)
                {
                    return BadRequest("Belirtilen yeni varyant tipi bulunamadı.");
                }
                variantValue.VariantType = newVariantType; // Navigasyon özelliğini güncelleyin
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
            var variantValue = await _context.VariantValues.FindAsync(id);
            if (variantValue == null)
            {
                return NotFound();
            }

            _context.VariantValues.Remove(variantValue);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}