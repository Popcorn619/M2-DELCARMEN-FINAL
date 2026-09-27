using M1_DELCARMEN.Data;
using M1_DELCARMEN.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.MaxDepth = 128;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<MarketplaceContext>(options =>
    options.UseSqlite("Data Source=MarketplaceDb.db"));

builder.Services.AddCors(p => p.AddPolicy("AllowAll", b => {
    b.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
}));

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

using var scope = app.Services.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<MarketplaceContext>();

await db.Database.EnsureCreatedAsync();

if (!db.Users.Any())
{
    db.Users.AddRange(new List<User>
    {
        new() { Username = "admin", Password = "admin123", Role = "Admin" },
        new() { Username = "staff", Password = "staff123", Role = "Staff" }
    });
    await db.SaveChangesAsync();
}

if (!db.Products.Any())
{
    db.Products.AddRange(new List<Product>
    {
        new() { Name = "Premium White Rice 1kg",
            Code = "RICE-001", Brand = "Well-Milled", UnitPrice = 55.00M,
            Category = "Groceries", Stock = 100, ReorderLevel = 20 },
        new() { Name = "Fresh Chicken Eggs (12pcs)",
            Code = "EGG-001", Brand = "PoultryFresh", UnitPrice = 90.00M,
            Category = "Groceries", Stock = 60, ReorderLevel = 15 },
        new() { Name = "Sardines in Tomato Sauce",
            Code = "SARD-001", Brand = "Ligo", UnitPrice = 18.00M,
            Category = "Groceries", Stock = 150, ReorderLevel = 30 },
        new() { Name = "Whole Chicken 1kg",
            Code = "CHKN-001", Brand = "Magnolia", UnitPrice = 165.00M,
            Category = "Meat & Poultry", Stock = 40, ReorderLevel = 10 },
        new() { Name = "Purified Drinking Water 500ml",
            Code = "WATR-001", Brand = "NatureSpring", UnitPrice = 15.00M,
            Category = "Beverages", Stock = 200, ReorderLevel = 50 },
        new() { Name = "Writing Notebook (80 leaves)",
            Code = "NOTE-001", Brand = "BestBuy", UnitPrice = 25.00M,
            Category = "School Supplies", Stock = 80, ReorderLevel = 20 },
        new() { Name = "Black Ballpen (1pc)",
            Code = "PEN-001", Brand = "Panda", UnitPrice = 8.00M,
            Category = "School Supplies", Stock = 120, ReorderLevel = 30 },
        new() { Name = "USB Flash Drive 16GB",
            Code = "USB-016", Brand = "Kingston", UnitPrice = 299.00M,
            Category = "Tech Accessories", Stock = 25, ReorderLevel = 5 },
        new() { Name = "Android Phone Charger",
            Code = "CHGR-001", Brand = "Samsung", UnitPrice = 149.00M,
            Category = "Tech Accessories", Stock = 30, ReorderLevel = 8 },
        new() { Name = "Antibacterial Bar Soap",
            Code = "SOAP-001", Brand = "Safeguard", UnitPrice = 32.00M,
            Category = "Personal Care", Stock = 90, ReorderLevel = 25 },
        new() { Name = "Fluoride Toothpaste",
            Code = "TOOTH-001", Brand = "Colgate", UnitPrice = 45.00M,
            Category = "Personal Care", Stock = 85, ReorderLevel = 20 },
        new() { Name = "Cotton Kitchen Towel",
            Code = "TOWL-001", Brand = "HomeCare", UnitPrice = 38.00M,
            Category = "Household", Stock = 50, ReorderLevel = 12 }
    });
    await db.SaveChangesAsync();
}

app.Run("http://localhost:5000");