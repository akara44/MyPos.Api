using Microsoft.AspNetCore.Mvc;
using MyPos.Infrastructure.Migrations;
using MyPos.Infrastructure.Persistence;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System;

[ApiController]
[Route("api/[controller]")]
public class PersonnelController : ControllerBase
{
    private readonly MyPosDbContext _context;
    private readonly IWebHostEnvironment _env;

    public PersonnelController(MyPosDbContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromForm] PersonnelCreateDto dto)
    {
        var validator = new PersonnelCreateDtoValidator();
        var result = validator.Validate(dto);
        if (!result.IsValid)
            return BadRequest(result.Errors);

          string? imagePath = null;

        if (dto.Image != null)
        {
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(dto.Image.FileName)}";
            var savePath = Path.Combine(_env.WebRootPath, "images", fileName);

            var directory = Path.GetDirectoryName(savePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using var image = SixLabors.ImageSharp.Image.Load(dto.Image.OpenReadStream());
            image.Mutate(x => x.Resize(150, 150));
            await image.SaveAsync(savePath);
            imagePath = $"images/{fileName}";
        }

        var personnel = new Personnel
        {
            Name = dto.Name,
            Code = dto.Code,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            IsActive = dto.IsActive,
            ViewCustomer = dto.ViewCustomer,
            AddOrUpdateProduct = dto.AddOrUpdateProduct,
            DeleteProduct = dto.DeleteProduct,
            ManageCompany = dto.ManageCompany,
            ViewIncomeExpense = dto.ViewIncomeExpense,
            PurchaseInvoiceFullAccess = dto.PurchaseInvoiceFullAccess,
            PurchaseInvoiceCreate = dto.PurchaseInvoiceCreate,
            StockCount = dto.StockCount,
            SalesDiscount = dto.SalesDiscount,
            DailyReport = dto.DailyReport,
            HistoricalReport = dto.HistoricalReport,
            ViewTurnoverInReports = dto.ViewTurnoverInReports,
            MaxDiscount = dto.MaxDiscount,
            DiscountCurrency = dto.DiscountCurrency,
            IdentityNumber = dto.IdentityNumber,
            Role = dto.Role,
            Phone = dto.Phone,
            Address = dto.Address,
            EmergencyContact = dto.EmergencyContact,
            BloodType = dto.BloodType,
            StartingSalary = dto.StartingSalary,
            CurrentSalary = dto.CurrentSalary,
            IBAN = dto.IBAN,
            Notes = dto.Notes,
            ImagePath = imagePath ?? string.Empty

        };

        _context.Personnel.Add(personnel);
        await _context.SaveChangesAsync();

        return Ok("Personel başarıyla eklendi.");
    }
    [HttpGet("getall")]
    public IActionResult GetAll()
    {
        var personals = _context.Personnel.ToList();
        return Ok(personals);
    }
    [HttpDelete("delete/{id}")]
    public IActionResult Delete(Guid id) 
    {
        var personal = _context.Personnel.FirstOrDefault(p => p.Id == id);
        if (personal == null) return NotFound("Kullanıcı bulunamadı.");

        _context.Personnel.Remove(personal);
        _context.SaveChanges();

        return Ok("Kullanıcı silindi.");
    }
    [HttpPut("update/{id}")]
    public IActionResult Update(Guid id, [FromBody] Personnel updated)
    {
        var personal = _context.Personnel.FirstOrDefault(p => p.Id == id);
        if (personal == null) return NotFound("Kullanıcı bulunamadı.");

        personal.Name = updated.Name;
        personal.Code = updated.Code;
        personal.Phone = updated.Phone;
        personal.Address = updated.Address;
        personal.IdentityNumber = updated.IdentityNumber;
        personal.BloodType = updated.BloodType;
        personal.Role = updated.Role;
        personal.EmergencyContact = updated.EmergencyContact;
        personal.IBAN = updated.IBAN;
        personal.Notes = updated.Notes;
        personal.ImagePath = updated.ImagePath;

        _context.SaveChanges();

        return Ok("Kullanıcı güncellendi.");
    }

    [HttpPut("update-password/{id}")]
    public IActionResult UpdatePassword(Guid id, [FromBody] string newPassword) 
    {
        var personal = _context.Personnel.FirstOrDefault(p => p.Id == id);
        if (personal == null) return NotFound("Kullanıcı bulunamadı.");

        personal.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        _context.SaveChanges();

        return Ok("Şifre güncellendi.");
    }


}
