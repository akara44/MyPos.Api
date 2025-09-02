using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyPos.Application.Dtos;
using MyPos.Domain.Entities;
using MyPos.Infrastructure.Persistence;

namespace MyPos.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
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
            var variantTypes = await _context.VariantTypes
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
            var variantType = await _context.VariantTypes.FindAsync(id);
            if (variantType == null)
            {
                return NotFound();
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
            var validationResult = await _createVariantTypeValidator.ValidateAsync(dto);
            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.Errors);
            }

            var variantType = new VariantType { Name = dto.Name };
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
            if (id != dto.Id)
            {
                return BadRequest("ID'ler eşleşmiyor.");
            }

            var validationResult = await _updateVariantTypeValidator.ValidateAsync(dto);
            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.Errors);
            }

            var variantType = await _context.VariantTypes.FindAsync(id);
            if (variantType == null)
            {
                return NotFound();
            }

            variantType.Name = dto.Name;
            await _context.SaveChangesAsync();

            return NoContent(); // 204 No Content
        }

        // Varyant tipi sil
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var variantType = await _context.VariantTypes.FindAsync(id);
            if (variantType == null)
            {
                return NotFound();
            }

            _context.VariantTypes.Remove(variantType);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}