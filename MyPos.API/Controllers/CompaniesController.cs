using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyPos.Application.DTOs; // CompanyDto'nun burada olduğunu varsayıyorum
using MyPos.Application.Validators; // CompanyValidator'ın burada olduğunu varsayıyorum
using MyPos.Domain.Entities; // Company entity'sinin burada olduğunu varsayıyorum
using MyPos.Infrastructure.Persistence;
using System;
using System.Threading.Tasks; // Task kullanımı için

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

        /// <summary>
        /// Yeni bir firma oluşturur.
        /// </summary>
        /// <param name="dto">Oluşturulacak firma verileri.</param>
        /// <returns>Oluşturulan firma verileri ile birlikte 201 Created yanıtı.</returns>
        [HttpPost]
        public async Task<IActionResult> CreateCompany([FromBody] CompanyDto dto)
        {
            // Yeni bir firma oluşturulurken ID'nin 0 veya belirtilmemiş olması beklenir.
            // Bu kontrolü DTO'da yapmaya gerek yok, çünkü sadece yeni kayıt yapıyoruz.
            // FluentValidation'da Id için bir kuralınız yoksa, burada manuel bir kontrol ekleyebilirsiniz.
            if (dto.Id != 0)
            {
                return BadRequest("Yeni firma oluşturulurken ID belirtilmemelidir.");
            }

            // Validasyon
            var validationResult = await _validator.ValidateAsync(dto);
            if (!validationResult.IsValid)
            {
                // Hata mesajlarını daha okunabilir bir formatta döndürelim
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

            // Oluşturulan firmanın ID'sini DTO'ya geri atayalım ve CreatedDate'i de ekleyelim
            dto.Id = company.Id;
            dto.CreatedDate = company.CreatedDate;

            return CreatedAtAction(nameof(GetById), new { id = company.Id }, dto);
        }

        /// <summary>
        /// Mevcut bir firmayı günceller.
        /// </summary>
        /// <param name="id">Güncellenecek firmanın ID'si.</param>
        /// <param name="dto">Güncellenmiş firma verileri.</param>
        /// <returns>Güncellenmiş firma verileri ile birlikte 200 OK yanıtı veya 404 Not Found.</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCompany(int id, [FromBody] CompanyDto dto)
        {
            // URL'deki ID ile DTO'daki ID'nin eşleştiğinden emin olalım
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

            // Entity'yi DTO'dan gelen verilerle güncelleyelim
            company.Name = dto.Name;
            company.OfficialName = dto.OfficialName;
            company.TermDays = dto.TermDays;
            company.Phone = dto.Phone;
            company.MobilePhone = dto.MobilePhone;
            company.Address = dto.Address;
            company.TaxOffice = dto.TaxOffice;
            company.TaxNumber = dto.TaxNumber;
            // CreatedDate update işleminde değiştirilmez, sadece ilk oluşturulduğunda atanır.

            _context.Company.Update(company);
            await _context.SaveChangesAsync();

            return Ok(dto); // Güncellenmiş DTO'yu geri döndürelim
        }

        /// <summary>
        /// Belirtilen ID'ye sahip firmayı getirir.
        /// </summary>
        /// <param name="id">Getirilecek firmanın ID'si.</param>
        /// <returns>Firma verileri ile birlikte 200 OK yanıtı veya 404 Not Found.</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var company = await _context.Company.FindAsync(id);
            if (company == null)
                return NotFound("Firma bulunamadı."); // Daha spesifik bir hata mesajı

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

        /// <summary>
        /// Belirtilen ID'ye sahip firmayı siler.
        /// </summary>
        /// <param name="id">Silinecek firmanın ID'si.</param>
        /// <returns>204 No Content yanıtı veya 404 Not Found.</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var company = await _context.Company.FindAsync(id);
            if (company == null)
                return NotFound("Firma bulunamadı."); // Daha spesifik bir hata mesajı

            _context.Company.Remove(company);
            await _context.SaveChangesAsync();

            return NoContent(); // Başarılı silme için 204 No Content
        }

        /// <summary>
        /// Tüm firmaları listeler.
        /// </summary>
        /// <returns>Tüm firmaların listesi ile birlikte 200 OK yanıtı.</returns>
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