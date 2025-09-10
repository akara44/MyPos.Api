using Microsoft.AspNetCore.Http;

namespace MyPos.Application.Dtos.Product
{
    public class UpdateProductWithImageDto : CreateProductDto
    {
        public IFormFile? ImageFile { get; set; } 
    }
}
