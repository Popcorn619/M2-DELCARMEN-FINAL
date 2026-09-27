using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using M1_DELCARMEN.Data;
using M1_DELCARMEN.Models;

namespace M1_DELCARMEN.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly MarketplaceContext _context;

    public OrdersController(MarketplaceContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Order>>> GetOrders()
    {
        return await _context.Orders
            .Include(o => o.OrderItems)
            .ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Order>> GetOrder(int id)
    {
        var order = await _context.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
            return NotFound();

        return order;
    }

    [HttpPost]
    public async Task<ActionResult<Order>> CreateOrder(Order order)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            order.OrderDate = DateTime.Now;
            order.Status = "Order Placed";
            order.IsShipped = false;

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            foreach (var item in order.OrderItems)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product == null)
                {
                    await transaction.RollbackAsync();
                    return BadRequest($"Product ID {item.ProductId} not found");
                }

                if (product.Stock < item.Quantity)
                {
                    await transaction.RollbackAsync();
                    return BadRequest($"Insufficient stock for {product.Name}");
                }

                product.Stock -= item.Quantity;
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    [HttpPut("{id}/ship")]
    public async Task<IActionResult> MarkAsShipped(int id)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order == null)
            return NotFound();

        order.IsShipped = true;
        order.Status = "Shipped — Waiting for Courier";

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("buyer/{username}")]
    public async Task<ActionResult<IEnumerable<Order>>> GetOrdersByBuyer(string username)
    {
        var orders = await _context.Orders
            .Include(o => o.OrderItems)
            .Where(o => o.BuyerUsername == username)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();

        return orders;
    }
}