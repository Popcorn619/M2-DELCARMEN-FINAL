using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using M1_DELCARMEN.Data;
using M1_DELCARMEN.Models;

namespace M1_DELCARMEN.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ProductsController : ControllerBase
{
    private readonly MarketplaceContext _context;

    public ProductsController(MarketplaceContext context)
    {
        _context = context;
    }

    //GET: api/Products — LIST ALL PRODUCTS
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
    {
        if (_context.Products == null)
            return NotFound();
        return await _context.Products.ToListAsync();
    }

    //POST: api/Products — ADD NEW PRODUCT 
    [HttpPost]
    public async Task<ActionResult<Product>> PostProduct(Product product)
    {
        if (_context.Products == null)
            return NotFound();

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
    }

    // GET: api/Products/{id} — GET SINGLE PRODUCT
    [HttpGet("{id}")]
    public async Task<ActionResult<Product>> GetProduct(int id)
    {
        if (_context.Products == null)
            return NotFound();

        var product = await _context.Products.FindAsync(id);
        if (product == null)
            return NotFound();

        return product;
    }

    // DELETE: api/Products/{id} — DELETE PRODUCT
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        if (_context.Products == null)
            return NotFound();

        var product = await _context.Products.FindAsync(id);
        if (product == null)
            return NotFound();

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}