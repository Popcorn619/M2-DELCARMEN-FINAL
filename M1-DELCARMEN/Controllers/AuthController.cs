using M1_DELCARMEN.Data;
using M1_DELCARMEN.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace M1_DELCARMEN.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly MarketplaceContext _db;
    public AuthController(MarketplaceContext db) => _db = db;

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u =>
            u.Username == req.Username && u.Password == req.Password);

        if (user == null) return Unauthorized("Invalid login");

        return Ok(new { user.Id, user.Username, user.Role });
    }
}

public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}