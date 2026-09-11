using MyFn.Domain.Enums;

namespace MyFn.Domain.Entities;

public class Category
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public CategoryType Type { get; set; }
    public string Color { get; set; } = "#1b7a4e";
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }

    public User? User { get; set; }
}
