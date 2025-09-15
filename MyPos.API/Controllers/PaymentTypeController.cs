using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyPos.Application.Dtos.PaymentType;
using MyPos.Application.Validators.PaymentType;
using MyPos.Domain.Entities;
using MyPos.Infrastructure.Persistence;
using System.Security.Claims; // Bu using'i ekle

namespace MyPos.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Auth'u aç ve tüm controller için geçerli kıl
    public class PaymentTypesController : ControllerBase
    {
        private readonly MyPosDbContext _context;
        private readonly PaymentTypeDtoValidator _validator;

        public PaymentTypesController(MyPosDbContext context, PaymentTypeDtoValidator validator)
        {
            _context = context;
            _validator = validator;
        }

        // GET: api/PaymentTypes
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PaymentTypeDto>>> GetPaymentTypes()
        {
            // JWT'den mevcut kullanıcının ID'sini al
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Sadece mevcut kullanıcıya ait verileri getir
            var paymentTypes = await _context.PaymentTypes
                                             .Where(p => p.UserId == currentUserId)
                                             .ToListAsync();

            var paymentTypeDtos = paymentTypes.Select(p => new PaymentTypeDto
            {
                Id = p.Id,
                Name = p.Name,
                CashRegisterType = p.CashRegisterType
            }).ToList();

            return Ok(paymentTypeDtos);
        }

        // GET: api/PaymentTypes/5
        [HttpGet("{id}")]
        public async Task<ActionResult<PaymentTypeDto>> GetPaymentType(int id)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Veriyi bulurken hem ID'yi hem de UserId'yi kontrol et
            var paymentType = await _context.PaymentTypes
                                             .FirstOrDefaultAsync(p => p.Id == id && p.UserId == currentUserId);

            if (paymentType == null)
            {
                return NotFound();
            }

            var paymentTypeDto = new PaymentTypeDto
            {
                Id = paymentType.Id,
                Name = paymentType.Name,
                CashRegisterType = paymentType.CashRegisterType
            };
            return Ok(paymentTypeDto);
        }

        // api/PaymentTypes
        [HttpPost]
        public async Task<ActionResult<PaymentTypeDto>> CreatePaymentType(PaymentTypeDto paymentTypeDto)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var validationResult = await _validator.ValidateAsync(paymentTypeDto);
            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.Errors);
            }

            var paymentType = new PaymentType
            {
                Name = paymentTypeDto.Name,
                CashRegisterType = paymentTypeDto.CashRegisterType,
                CreatedDate = DateTime.Now,
                UserId = currentUserId // Oluşturulan veriye kullanıcının ID'sini ekle
            };

            _context.PaymentTypes.Add(paymentType);
            await _context.SaveChangesAsync();

            paymentTypeDto.Id = paymentType.Id;
            return CreatedAtAction(nameof(GetPaymentType), new { id = paymentTypeDto.Id }, paymentTypeDto);
        }

        // api/PaymentTypes/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePaymentType(int id, PaymentTypeDto paymentTypeDto)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (id != paymentTypeDto.Id)
            {
                return BadRequest();
            }

            var validationResult = await _validator.ValidateAsync(paymentTypeDto);
            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.Errors);
            }

            // Veriyi bulurken hem ID'yi hem de UserId'yi kontrol et
            var paymentTypeToUpdate = await _context.PaymentTypes
                                                     .FirstOrDefaultAsync(p => p.Id == id && p.UserId == currentUserId);

            if (paymentTypeToUpdate == null)
            {
                return NotFound();
            }

            paymentTypeToUpdate.Name = paymentTypeDto.Name;
            paymentTypeToUpdate.CashRegisterType = paymentTypeDto.CashRegisterType;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                // Concurrency kontrolünde de UserId'yi kontrol et
                if (!PaymentTypeExists(id, currentUserId))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // api/PaymentTypes/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePaymentType(int id)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Veriyi silerken hem ID'yi hem de UserId'yi kontrol et
            var paymentType = await _context.PaymentTypes
                                             .FirstOrDefaultAsync(p => p.Id == id && p.UserId == currentUserId);

            if (paymentType == null)
            {
                return NotFound();
            }

            _context.PaymentTypes.Remove(paymentType);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // Metodun UserId parametresi alacak şekilde güncellenmesi
        private bool PaymentTypeExists(int id, string userId)
        {
            return _context.PaymentTypes.Any(e => e.Id == id && e.UserId == userId);
        }
    }
}   