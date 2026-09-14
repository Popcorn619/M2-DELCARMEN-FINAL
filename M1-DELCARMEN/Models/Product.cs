using System.ComponentModel.DataAnnotations;

namespace M1_DELCARMEN.Models;

public class Product
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Brand { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Category { get; set; } = string.Empty;

    public decimal UnitPrice
    {
        get => Price;
        set => Price = value;
    }

    [Required]
    public decimal Price { get; set; }

    [Required]
    public int Stock { get; set; }

    public int ReorderLevel { get; set; }
}