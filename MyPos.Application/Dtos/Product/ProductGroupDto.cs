namespace MyPos.Application.Dtos.Product;

public class ProductGroupDto
{
    public string Name { get; set; } = string.Empty;
    public int? ParentGroupId { get; set; } // yeni eklendi
}
