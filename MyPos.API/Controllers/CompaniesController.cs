using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyPos.Application.DTOs; 
using MyPos.Application.Validators; 
using MyPos.Domain.Entities; 
using MyPos.Infrastructure.Persistence;
using System;
using System.Threading.Tasks; 

namespace MyPos.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CompaniesController : ControllerBase
    {
        private readonly MyPosDbContext _context;
        private readonly CompanyValidator _validator; // DTO validasyonu için

        public CompaniesController(MyPosDbContext context, CompanyValidator validator)
        {
            _context = context;
            _validator = validator;
        }

        
        [HttpPost]
        public async Task<IActionResult> CreateCompany([FromBody] CompanyDto dto)
        {

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
                CreatedDate = DateTime.UtcNow // Oluşturma tarihi burada otomatik atanır
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

            var company = await _context.Company.FindAsync(id);
            if (company == null)
            {
                return NotFound("Firma bulunamadı!");
            }

            
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
            var company = await _context.Company.FindAsync(id);
            if (company == null)
                return NotFound("Firma bulunamadı."); 

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
            var company = await _context.Company.FindAsync(id);
            if (company == null)
                return NotFound("Firma bulunamadı."); 

            _context.Company.Remove(company);
            await _context.SaveChangesAsync();

            return NoContent(); // Başarılı silme için 204 No Content
        }

       
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var companies = await _context.Company
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