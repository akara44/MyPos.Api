using Microsoft.AspNetCore.Mvc;
using MyPos.Infrastructure.Persistence;
using MyPos.Application.Dtos;
using MyPos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using MyPos.Application.Validators;

namespace YourProjectNamespace.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
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
            var paymentTypes = await _context.PaymentTypes.ToListAsync();
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
            var paymentType = await _context.PaymentTypes.FindAsync(id);

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

        // POST: api/PaymentTypes
        [HttpPost]
        public async Task<ActionResult<PaymentTypeDto>> CreatePaymentType(PaymentTypeDto paymentTypeDto)
        {
            var validationResult = await _validator.ValidateAsync(paymentTypeDto);
            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.Errors);
            }

            var paymentType = new PaymentType
            {
                Name = paymentTypeDto.Name,
                CashRegisterType = paymentTypeDto.CashRegisterType,
                CreatedDate = DateTime.Now
            };

            _context.PaymentTypes.Add(paymentType);
            await _context.SaveChangesAsync();

            paymentTypeDto.Id = paymentType.Id;
            return CreatedAtAction(nameof(GetPaymentType), new { id = paymentTypeDto.Id }, paymentTypeDto);
        }

        // PUT: api/PaymentTypes/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePaymentType(int id, PaymentTypeDto paymentTypeDto)
        {
            if (id != paymentTypeDto.Id)
            {
                return BadRequest();
            }

            var validationResult = await _validator.ValidateAsync(paymentTypeDto);
            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.Errors);
            }

            var paymentTypeToUpdate = await _context.PaymentTypes.FindAsync(id);
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
                if (!PaymentTypeExists(id))
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

        // DELETE: api/PaymentTypes/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePaymentType(int id)
        {
            var paymentType = await _context.PaymentTypes.FindAsync(id);
            if (paymentType == null)
            {
                return NotFound();
            }

            _context.PaymentTypes.Remove(paymentType);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool PaymentTypeExists(int id)
        {
            return _context.PaymentTypes.Any(e => e.Id == id);
        }
    }
}