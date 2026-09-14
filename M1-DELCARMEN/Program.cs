using M1_DELCARMEN.Data;
using M1_DELCARMEN.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<MarketplaceContext>(options =>
    options.UseInMemoryDatabase("MarketplaceDb"));

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

if (!db.Users.Any())
{
    db.Users.AddRange(new List<User>
    {
        new() { Username = "admin", Password = "admin123", Role = "Admin" },
        new() { Username = "staff", Password = "staff123", Role = "Staff" }
    });
}

if (!db.Products.Any())
{
    db.Products.AddRange(new List<Product>
    {
        new() { Name = "Premium White Rice 1kg",          Code = "RICE-001", Brand = "Well-Milled",   Category = "Groceries",         Price = 55.00M,  Stock = 100, ReorderLevel = 20 },
        new() { Name = "Fresh Chicken Eggs (12pcs)",      Code = "EGG-001",  Brand = "PoultryFresh", Category = "Groceries",         Price = 90.00M,  Stock = 60,  ReorderLevel = 15 },
        new() { Name = "Sardines in Tomato Sauce",        Code = "SARD-001", Brand = "Ligo",         Category = "Groceries",         Price = 18.00M,  Stock = 150, ReorderLevel = 30 },
        new() { Name = "Whole Chicken 1kg",               Code = "CHKN-001", Brand = "Magnolia",     Category = "Meat & Poultry",    Price = 165.00M, Stock = 40,  ReorderLevel = 10 },
        new() { Name = "Purified Drinking Water 500ml",  Code = "WATR-001", Brand = "NatureSpring",  Category = "Beverages",          Price = 15.00M,  Stock = 200, ReorderLevel = 50 },
        new() { Name = "Writing Notebook (80 leaves)",    Code = "NOTE-001", Brand = "BestBuy",       Category = "School Supplies",    Price = 25.00M,  Stock = 80,  ReorderLevel = 20 },
        new() { Name = "Black Ballpen (1pc)",             Code = "PEN-001",  Brand = "Panda",         Category = "School Supplies",    Price = 8.00M,   Stock = 120, ReorderLevel = 30 },
        new() { Name = "USB Flash Drive 16GB",            Code = "USB-016",  Brand = "Kingston",      Category = "Tech Accessories",   Price = 299.00M, Stock = 25,  ReorderLevel = 5 },
        new() { Name = "Android Phone Charger",           Code = "CHGR-001", Brand = "Samsung",       Category = "Tech Accessories",   Price = 149.00M, Stock = 30,  ReorderLevel = 8 },
        new() { Name = "Antibacterial Bar Soap",          Code = "SOAP-001", Brand = "Safeguard",     Category = "Personal Care",      Price = 32.00M,  Stock = 90,  ReorderLevel = 25 },
        new() { Name = "Fluoride Toothpaste",             Code = "TOOTH-001",Brand = "Colgate",       Category = "Personal Care",      Price = 45.00M,  Stock = 85,  ReorderLevel = 20 },
        new() { Name = "Cotton Kitchen Towel",            Code = "TOWL-001", Brand = "HomeCare",      Category = "Household",          Price = 38.00M,  Stock = 50,  ReorderLevel = 12 }
    });
}

await db.SaveChangesAsync();
app.Run("http://localhost:5000");