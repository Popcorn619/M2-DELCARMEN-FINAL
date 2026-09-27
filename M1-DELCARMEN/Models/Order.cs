using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace M1_DELCARMEN.Models;

public class Order
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string BuyerUsername { get; set; } = string.Empty;

    public List<OrderItem> OrderItems { get; set; } = new();

    public decimal TotalAmount { get; set; }

    public string PaymentMethod { get; set; } = string.Empty;

    public string ShippingMethod { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public bool IsShipped { get; set; }

    public DateTime OrderDate { get; set; }
}

public class OrderItem
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string ProductName { get; set; } = string.Empty;

    public int ProductId { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public int OrderId { get; set; }

    [System.ComponentModel.DataAnnotations.Schema.ForeignKey(nameof(OrderId))]
    public Order? Order { get; set; }
}