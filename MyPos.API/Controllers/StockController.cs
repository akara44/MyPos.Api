using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyPos.Domain.Entities;
using MyPos.Infrastructure.Persistence;
using System.Threading.Tasks;

[Route("api/[controller]")]
[ApiController]
public class StockController : ControllerBase
{
    private readonly MyPosDbContext _context;

    public StockController(MyPosDbContext context)
    {
        _context = context;
    }

    [HttpPost("adjust")]
    public async Task<IActionResult> AdjustStock([FromBody] StockAdjustmentDto dto)
    {
        // 1. DTO'yu doğrula
        var validator = new StockAdjustmentValidator();
        var validationResult = await validator.ValidateAsync(dto);

        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.Errors);
        }

        // 2. Ürünü bul
        var product = await _context.Products.FindAsync(dto.ProductId);
        if (product == null)
        {
            return NotFound("Product not found.");
        }

        // 3. Stok miktarını güncelle
        // Stok miktarının sıfırın altına düşmesini engelle
        if (product.Stock + dto.QuantityChange < 0)
        {
            return BadRequest("Cannot decrease stock below zero.");
        }
        product.Stock += dto.QuantityChange;

        // 4. Stok hareketini kayıt altına al
        var stockTransaction = new StockTransaction
        {
            ProductId = dto.ProductId,
            QuantityChange = dto.QuantityChange,
            TransactionType = dto.QuantityChange > 0 ? "IN" : "OUT",
            Reason = dto.Reason,
            Date = DateTime.Now
        };

        _context.StockTransaction.Add(stockTransaction);

        // 5. Değişiklikleri veritabanına kaydet
        await _context.SaveChangesAsync();

        return Ok(new { message = "Stock updated successfully." });
    }

    [HttpGet("history/{productId}")]
    public async Task<IActionResult> GetStockHistory(int productId)
    {
        // Stok geçmişini sorgula ve Product nesnesini de dahil et
        var history = await _context.StockTransaction
            .Include(t => t.Product)
            .Where(t => t.ProductId == productId)
            .OrderByDescending(t => t.Date)
            .ToListAsync();

        if (history == null || history.Count == 0)
        {
            return NotFound("No stock history found for this product.");
        }

        // Verileri daha temiz bir DTO objesi olarak dönmek isterseniz:
        // var historyDto = history.Select(h => new { ... });
        // return Ok(historyDto);

        return Ok(history);
    }
}