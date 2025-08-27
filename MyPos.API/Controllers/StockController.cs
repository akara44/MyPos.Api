using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyPos.Domain.Entities;
using MyPos.Infrastructure.Persistence;
using System.Threading.Tasks;

[Route("api/[controller]")]
[ApiController]
[Authorize]
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
        var validator = new StockAdjustmentValidator();
        var validationResult = await validator.ValidateAsync(dto);

        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.Errors);
        }

        var product = await _context.Products.FindAsync(dto.ProductId);
        if (product == null)
        {
            return NotFound("Product not found.");
        }

        if (product.Stock + dto.QuantityChange < 0)
        {
            return BadRequest("Cannot decrease stock below zero.");
        }

        product.Stock += dto.QuantityChange;

        var stockTransaction = new StockTransaction
        {
            ProductId = dto.ProductId,
            QuantityChange = dto.QuantityChange,
            TransactionType = dto.QuantityChange > 0 ? "IN" : "OUT",
            Reason = dto.Reason,
            Date = DateTime.Now,
            BalanceAfter = product.Stock
        };

        _context.StockTransaction.Add(stockTransaction);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Stock updated successfully." });
    }

    [HttpGet("history/{productId}")]
    public async Task<IActionResult> GetStockHistory(int productId)
    {
        var history = await _context.StockTransaction
            .Where(t => t.ProductId == productId)
            .OrderByDescending(t => t.Date)
            .Select(t => new
            {
                t.Id,
                t.ProductId,
                ProductName = t.Product != null ? t.Product.Name : string.Empty,
                t.QuantityChange,
                t.TransactionType,
                t.Reason,
                t.Date,
                t.BalanceAfter
            })
            .ToListAsync();

        if (history.Count == 0)
        {
            return NotFound("No stock history found for this product.");
        }

        return Ok(history);
    }
}
