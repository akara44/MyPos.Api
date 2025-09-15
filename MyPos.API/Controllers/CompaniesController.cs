using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyPos.Application.Dtos.Company;
using MyPos.Application.Validators.Auth;
using MyPos.Domain.Entities;
using MyPos.Infrastructure.Persistence;
using System;
using System.Linq; // Hata çözümü için eklendi
using System.Security.Claims; // User ID'yi almak için eklendi
using System.Threading.Tasks;

namespace MyPos.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Auth'u aç ve tüm controller için geçerli kıl
    public class CompaniesController : ControllerBase
    {
        private readonly MyPosDbContext _context;
        private readonly CompanyValidator _validator;

        public CompaniesController(MyPosDbContext context, CompanyValidator validator)
        {
            _context = context;
            _validator = validator;
        }

        [HttpPost]
        public async Task<IActionResult> CreateCompany([FromBody] CompanyDto dto)
        {
            // JWT'den mevcut kullanıcının ID'sini al
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (dto.Id != 0)
            {
                return BadRequest("Yeni firma oluşturulurken ID belirtilmemelidir.");
            }

            // Validasyon
            var validationResult = await _validator.ValidateAsync(dto);
            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.Errors.Select(e => new { Field = e.PropertyName, Message = e.ErrorMessage }));
            }

            var company = new Company
            {
                Name = dto.Name,
                OfficialName = dto.OfficialName,
                TermDays = dto.TermDays,
                Phone = dto.Phone,
                MobilePhone = dto.MobilePhone,
                Address = dto.Address,
                TaxOffice = dto.TaxOffice,
                TaxNumber = dto.TaxNumber,
                CreatedDate = DateTime.UtcNow,
                UserId = currentUserId // Oluşturulan veriye kullanıcının ID'sini ekle
            };

            await _context.Company.AddAsync(company);
            await _context.SaveChangesAsync();

            dto.Id = company.Id;
            dto.CreatedDate = company.CreatedDate;

            return CreatedAtAction(nameof(GetById), new { id = company.Id }, dto);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCompany(int id, [FromBody] CompanyDto dto)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (id != dto.Id)
            {
                return BadRequest("URL'deki ID ile gönderilen veri uyuşmuyor.");
            }

            // Validasyon
            var validationResult = await _validator.ValidateAsync(dto);
            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.Errors.Select(e => new { Field = e.PropertyName, Message = e.ErrorMessage }));
            }

            // Veriyi bulurken hem ID'yi hem de UserId'yi kontrol et
            var company = await _context.Company
                                        .FirstOrDefaultAsync(c => c.Id == id && c.UserId == currentUserId);

            if (company == null)
            {
                // Firma bulunamazsa veya kullanıcıya ait değilse NotFound dön
                return NotFound("Firma bulunamadı veya yetkiniz yok.");
            }

            // Sadece güncellenecek alanları eşle
            company.Name = dto.Name;
            company.OfficialName = dto.OfficialName;
            company.TermDays = dto.TermDays;
            company.Phone = dto.Phone;
            company.MobilePhone = dto.MobilePhone;
            company.Address = dto.Address;
            company.TaxOffice = dto.TaxOffice;
            company.TaxNumber = dto.TaxNumber;

            _context.Company.Update(company);
            await _context.SaveChangesAsync();

            return Ok(dto);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Veriyi bulurken hem ID'yi hem de UserId'yi kontrol et
            var company = await _context.Company
                                        .FirstOrDefaultAsync(c => c.Id == id && c.UserId == currentUserId);

            if (company == null)
                return NotFound("Firma bulunamadı veya yetkiniz yok.");

            var result = new CompanyDto
            {
                Id = company.Id,
                Name = company.Name,
                OfficialName = company.OfficialName,
                TermDays = company.TermDays,
                Phone = company.Phone,
                MobilePhone = company.MobilePhone,
                Address = company.Address,
                TaxOffice = company.TaxOffice,
                TaxNumber = company.TaxNumber,
                CreatedDate = company.CreatedDate
            };

            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Veriyi bulurken hem ID'yi hem de UserId'yi kontrol et
            var company = await _context.Company
                                        .FirstOrDefaultAsync(c => c.Id == id && c.UserId == currentUserId);

            if (company == null)
                return NotFound("Firma bulunamadı veya yetkiniz yok.");

            _context.Company.Remove(company);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Sadece mevcut kullanıcıya ait firmaları listele
            var companies = await _context.Company
                                          .Where(c => c.UserId == currentUserId)
                                          .Select(c => new CompanyDto
                                          {
                                              Id = c.Id,
                                              Name = c.Name,
                                              OfficialName = c.OfficialName,
                                              TermDays = c.TermDays,
                                              Phone = c.Phone,
                                              MobilePhone = c.MobilePhone,
                                              Address = c.Address,
                                              TaxOffice = c.TaxOffice,
                                              TaxNumber = c.TaxNumber,
                                              CreatedDate = c.CreatedDate
                                          })
                                          .ToListAsync();

            return Ok(companies);
        }
    }
}