using Microsoft.AspNetCore.Http;

namespace MyPos.Application.Dtos
{
    public class UpdateProductWithImageDto : CreateProductDto
    {
        public IFormFile? ImageFile { get; set; } 
    }
}
