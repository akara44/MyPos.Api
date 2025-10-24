namespace MyPos.Domain.Entities;

public class ProductGroup
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    // Hiyerarşi için eklenen alanlar
    public int? ParentGroupId { get; set; }
    public ProductGroup? ParentGroup { get; set; }

    public ICollection<ProductGroup>? SubGroups { get; set; }
    public string UserId { get; set; }
}
