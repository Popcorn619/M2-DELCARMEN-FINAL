using Microsoft.AspNetCore.Mvc;

namespace M1_DELCARMEN.Controllers;

[Route("api/[controller]")]
[ApiController]
public class OrdersController : ControllerBase
{
    
    [HttpPost("checkout")]
    public IActionResult Checkout([FromBody] CheckoutRequest order)
    {
        if (order == null || order.Items == null || order.Items.Count == 0)
        {
            return BadRequest(new { message = "Cart is empty!" });
        }

        
        foreach (var item in order.Items)
        {
            if (item.Product == null || item.Product.Id <= 0)
            {
                return NotFound(new { message = "Product not found" });
            }
        }

        
        return Ok(new
        {
            success = true,
            message = "Order placed successfully!",
            orderDate = order.OrderDate,
            total = order.TotalAmount,
            paymentMethod = order.PaymentMethod,
            shippingMethod = order.ShippingMethod
        });
    }
}


public class CheckoutRequest
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public List<CartItemOrder> Items { get; set; } = new();
    public string PaymentMethod { get; set; } = string.Empty;
    public string ShippingMethod { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public DateTime OrderDate { get; set; }
}

public class CartItemOrder
{
    public ProductOrder Product { get; set; }
    public int Quantity { get; set; }
    public decimal Subtotal { get; set; }
}

public class ProductOrder
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
}