using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyPos.Domain.Entities;
using MyPos.Infrastructure.Persistence;
using System.Security.Claims;

namespace MyPos.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SettingsController : ControllerBase
    {
        private readonly MyPosDbContext _context;

        public SettingsController(MyPosDbContext context) { _context = context; }

        [HttpGet]
        public async Task<IActionResult> GetSettings()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var settings = await _context.UserSettings.FirstOrDefaultAsync(u => u.UserId == userId);

            if (settings == null) return Ok(new { blockSaleIfNoStock = false });
            return Ok(settings);
        }

        [HttpPost("update-stock-setting")]
        public async Task<IActionResult> UpdateStockSetting([FromBody] bool blockSale)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var settings = await _context.UserSettings.FirstOrDefaultAsync(u => u.UserId == userId);

            if (settings == null)
            {
                settings = new UserSetting { UserId = userId, BlockSaleIfNoStock = blockSale };
                _context.UserSettings.Add(settings);
            }
            else
            {
                settings.BlockSaleIfNoStock = blockSale;
            }

            await _context.SaveChangesAsync();
            return Ok("Ayar güncellendi.");
        }
    }
}
