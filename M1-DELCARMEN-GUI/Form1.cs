using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace M1_DELCARMEN_GUI;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public decimal UnitPrice
    {
        get => Price;
        set => Price = value;
    }
    public string Category { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public string Email { get; set; } = string.Empty;
    public string GCashNumber { get; set; } = string.Empty;
    public string? ImagePath { get; set; }
}

public class CartItem
{
    public Product Product { get; set; }
    public int Quantity { get; set; }
    public decimal Subtotal => Product.Price * Quantity;
}

public class CheckoutRequest
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public List<CartItem> Items { get; set; } = new();
    public string PaymentMethod { get; set; } = string.Empty;
    public string ShippingMethod { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public DateTime OrderDate { get; set; }
    public CardInfo? CardDetails { get; set; }
    public string? PaymentProofPath { get; set; }
}

public class CardInfo
{
    public string CardNumber { get; set; } = string.Empty;
    public string ExpiryDate { get; set; } = string.Empty;
    public string CVV { get; set; } = string.Empty;
    public string CardholderName { get; set; } = string.Empty;
}

public class ApiClient
{
    private static readonly HttpClient http = new HttpClient { BaseAddress = new Uri("http://localhost:5000/") };
    private static readonly Random rnd = new Random();

    private static string GenerateEmail(Product p)
    {
        string[] domains = { "gmail.com", "yahoo.com", "outlook.com", "icloud.com", "protonmail.com" };
        string cleanName = Regex.Replace(p.Name.ToLower().Replace(" ", "."), "[^a-z0-9.]", "");
        string domain = domains[rnd.Next(domains.Length)];
        int num = rnd.Next(100, 9999);
        return $"{cleanName}{num}@{domain}";
    }

    private static string GenerateGCashNumber()
    {
        int part1 = rnd.Next(9000, 9999);
        int part2 = rnd.Next(100, 999);
        int part3 = rnd.Next(100, 999);
        return $"09{part1}-{part2}-{part3}";
    }

    public static void AssignContactDetails(List<Product> products)
    {
        foreach (var p in products)
        {
            p.Email = GenerateEmail(p);
            p.GCashNumber = GenerateGCashNumber();
        }
    }

    public static async Task<(User? user, string error)> LoginAsync(string username, string password)
    {
        try
        {
            var credentials = new { Username = username, Password = password };
            var res = await http.PostAsJsonAsync("api/Auth/login", credentials);
            if (res.IsSuccessStatusCode)
                return (await res.Content.ReadFromJsonAsync<User>(), string.Empty);
            return (null, $"API Error: {(int)res.StatusCode}");
        }
        catch (Exception ex) { return (null, "Error: " + ex.Message); }
    }

    public static async Task<(List<Product>? items, string error)> GetProductsAsync()
    {
        try
        {
            var data = await http.GetFromJsonAsync<List<Product>>("api/Products");
            if (data != null)
            {
                AssignContactDetails(data);
                return (data, string.Empty);
            }
            return (new List<Product>(), string.Empty);
        }
        catch (Exception ex) { return (null, "Error: " + ex.Message); }
    }

    public static async Task<(bool ok, string msg)> AddProductAsync(Product product)
    {
        try
        {
            product.Email = GenerateEmail(product);
            product.GCashNumber = GenerateGCashNumber();
            var res = await http.PostAsJsonAsync("api/Products", product);
            string msg = await res.Content.ReadAsStringAsync();
            return (res.IsSuccessStatusCode, res.IsSuccessStatusCode ? "Product Added Successfully!" : msg);
        }
        catch (Exception ex) { return (false, "Error: " + ex.Message); }
    }

    public static async Task<(bool ok, string msg)> DeleteProductAsync(int id)
    {
        try
        {
            var res = await http.DeleteAsync($"api/Products/{id}");
            string msg = await res.Content.ReadAsStringAsync();
            return (res.IsSuccessStatusCode, res.IsSuccessStatusCode ? "Product Deleted Successfully!" : msg);
        }
        catch (Exception ex) { return (false, "Error: " + ex.Message); }
    }

    public static async Task<(bool ok, string msg)> CheckoutAsync(CheckoutRequest req)
    {
        try
        {
            var res = await http.PostAsJsonAsync("api/Orders/checkout", req);
            string msg = await res.Content.ReadAsStringAsync();
            return (res.IsSuccessStatusCode, res.IsSuccessStatusCode ? "Order Placed Successfully!" : msg);
        }
        catch (Exception ex) { return (false, "Error: " + ex.Message); }
    }
}

public partial class Form1 : Form
{
    private User? currentUser;
    private List<Product> allProducts = new();
    private List<Product> filteredProducts = new();
    private List<CartItem> cart = new();
    private static readonly Color LightYellow = Color.FromArgb(255, 255, 249, 196);

    private Label lblStatus;
    private TextBox txtSearch;
    private DataGridView dgvProducts;
    private DataGridView dgvCart;
    private Label lblTotal;
    private NumericUpDown nudQty;
    private Button btnAddToCart;
    private Button btnRemoveCart;
    private Button btnCheckout;
    private Button btnAddProduct;
    private Button btnDeleteProduct;
    private PictureBox picProduct;
    private Button btnUploadImage;

    public Form1()
    {
        Text = "M1-DELCARMEN Marketplace";
        Size = new Size(1200, 800);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9F);
        BackColor = LightYellow;
        SetupControls();
        Shown += async (s, e) => await StartLogin();
    }

    private void SetupControls()
    {
        lblStatus = new Label { Text = "Not logged in", Left = 20, Top = 10, Width = 900, Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = Color.DarkRed, BackColor = Color.Transparent };
        Controls.Add(lblStatus);

        txtSearch = new TextBox { PlaceholderText = "Search by Product Name or Category...", Left = 20, Top = 40, Width = 600, BackColor = Color.White };
        txtSearch.TextChanged += (s, e) => FilterProducts();
        Controls.Add(txtSearch);

        btnAddProduct = new Button { Text = "+ Add New Product", Left = 640, Top = 35, Width = 180, Height = 35, BackColor = Color.Firebrick, ForeColor = Color.White, Font = new Font("Segoe UI", 9F, FontStyle.Bold), FlatStyle = FlatStyle.Flat };
        btnAddProduct.FlatAppearance.BorderSize = 0;
        btnAddProduct.Click += async (s, e) => await ShowAddProductDialog();
        Controls.Add(btnAddProduct);

        btnDeleteProduct = new Button { Text = "- Delete Selected Product", Left = 830, Top = 35, Width = 200, Height = 35, BackColor = Color.DarkRed, ForeColor = Color.White, Font = new Font("Segoe UI", 9F, FontStyle.Bold), FlatStyle = FlatStyle.Flat };
        btnDeleteProduct.FlatAppearance.BorderSize = 0;
        btnDeleteProduct.Click += async (s, e) => await DeleteSelectedProduct();
        Controls.Add(btnDeleteProduct);

        dgvProducts = new DataGridView
        {
            Left = 20,
            Top = 80,
            Width = 1150,
            Height = 350,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            BackgroundColor = Color.White,
            GridColor = Color.LightGray,
            EnableHeadersVisualStyles = false,
            AlternatingRowsDefaultCellStyle = { BackColor = Color.WhiteSmoke },
            RowHeadersVisible = false
        };
        dgvProducts.ColumnHeadersDefaultCellStyle.BackColor = Color.Firebrick;
        dgvProducts.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        dgvProducts.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        dgvProducts.DefaultCellStyle.SelectionBackColor = Color.LightCoral;
        dgvProducts.DefaultCellStyle.SelectionForeColor = Color.Black;
        dgvProducts.Columns.Add("Id", "ID");
        dgvProducts.Columns.Add("Name", "Product Name");
        dgvProducts.Columns.Add("Code", "Code");
        dgvProducts.Columns.Add("Brand", "Brand");
        dgvProducts.Columns.Add("Category", "Category");
        dgvProducts.Columns.Add("Price", "Price (P)");
        dgvProducts.Columns.Add("Email", "Email");
        dgvProducts.Columns.Add("GCash", "GCash Number");
        dgvProducts.Columns.Add("Stock", "Stock");
        dgvProducts.SelectionChanged += (s, e) => ShowSelectedProductImage();
        Controls.Add(dgvProducts);

        Label lblCart = new Label { Text = "Shopping Cart", Left = 20, Top = 440, Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = Color.DarkRed, BackColor = Color.Transparent };
        Controls.Add(lblCart);

        dgvCart = new DataGridView
        {
            Left = 20,
            Top = 465,
            Width = 600,
            Height = 180,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            BackgroundColor = Color.White,
            GridColor = Color.LightGray,
            EnableHeadersVisualStyles = false,
            RowHeadersVisible = false
        };
        dgvCart.ColumnHeadersDefaultCellStyle.BackColor = Color.Firebrick;
        dgvCart.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        dgvCart.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        dgvCart.DefaultCellStyle.SelectionBackColor = Color.LightCoral;
        dgvCart.DefaultCellStyle.SelectionForeColor = Color.Black;
        dgvCart.Columns.Add("Name", "Item");
        dgvCart.Columns.Add("Qty", "Qty");
        dgvCart.Columns.Add("Subtotal", "Subtotal (P)");
        Controls.Add(dgvCart);

        Label lblImgSection = new Label { Text = "Image Upload", Left = 640, Top = 445, Width = 530, Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.DarkRed, BackColor = Color.Transparent };
        Controls.Add(lblImgSection);

        picProduct = new PictureBox { Left = 640, Top = 470, Width = 530, Height = 110, BackColor = Color.WhiteSmoke, BorderStyle = BorderStyle.FixedSingle, SizeMode = PictureBoxSizeMode.Zoom, Image = null };
        Controls.Add(picProduct);

        btnUploadImage = new Button { Text = "Upload Image", Left = 640, Top = 585, Width = 530, Height = 30, BackColor = Color.Firebrick, ForeColor = Color.White, Font = new Font("Segoe UI", 9F, FontStyle.Bold), FlatStyle = FlatStyle.Flat };
        btnUploadImage.FlatAppearance.BorderSize = 0;
        btnUploadImage.Click += (s, e) => UploadProductImage();
        Controls.Add(btnUploadImage);

        lblTotal = new Label { Text = "TOTAL: P0.00", Left = 420, Top = 655, Width = 200, Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = Color.DarkRed, TextAlign = ContentAlignment.MiddleRight, BackColor = Color.Transparent };
        Controls.Add(lblTotal);

        nudQty = new NumericUpDown { Minimum = 1, Maximum = 100, Value = 1, Left = 20, Top = 655, Width = 80, BackColor = Color.White };
        Controls.Add(nudQty);

        btnAddToCart = new Button { Text = "+ Add to Cart", Left = 110, Top = 655, Width = 140, Height = 35, BackColor = Color.Firebrick, ForeColor = Color.White, Font = new Font("Segoe UI", 9F, FontStyle.Bold), FlatStyle = FlatStyle.Flat };
        btnAddToCart.FlatAppearance.BorderSize = 0;
        btnAddToCart.Click += (s, e) => AddToCart();
        Controls.Add(btnAddToCart);

        btnRemoveCart = new Button { Text = "- Remove Item", Left = 260, Top = 655, Width = 140, Height = 35, BackColor = Color.DarkRed, ForeColor = Color.White, Font = new Font("Segoe UI", 9F, FontStyle.Bold), FlatStyle = FlatStyle.Flat };
        btnRemoveCart.FlatAppearance.BorderSize = 0;
        btnRemoveCart.Click += (s, e) => RemoveFromCart();
        Controls.Add(btnRemoveCart);

        btnCheckout = new Button { Text = "CHECKOUT", Left = 640, Top = 625, Width = 530, Height = 35, BackColor = Color.Firebrick, ForeColor = Color.White, Font = new Font("Segoe UI", 10F, FontStyle.Bold), FlatStyle = FlatStyle.Flat };
        btnCheckout.FlatAppearance.BorderSize = 0;
        btnCheckout.Click += async (s, e) => await ShowCheckoutDialog();
        Controls.Add(btnCheckout);
    }

    private void ShowSelectedProductImage()
    {
        picProduct.Image = null;
        if (dgvProducts.SelectedRows.Count == 0) return;
        int id = Convert.ToInt32(dgvProducts.SelectedRows[0].Cells["Id"].Value);
        var product = allProducts.FirstOrDefault(p => p.Id == id);
        if (product == null || string.IsNullOrEmpty(product.ImagePath) || !File.Exists(product.ImagePath)) return;
        try
        {
            picProduct.Image = Image.FromFile(product.ImagePath);
        }
        catch { }
    }

    private void UploadProductImage()
    {
        if (dgvProducts.SelectedRows.Count == 0)
        {
            MessageBox.Show("Select a product from the list first!", "No Product Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        using (OpenFileDialog fd = new OpenFileDialog())
        {
            fd.Title = "Select Product Image";
            fd.Filter = "Image Files (*.jpg;*.jpeg;*.png;*.bmp;*.gif)|*.jpg;*.jpeg;*.png;*.bmp;*.gif";
            fd.RestoreDirectory = true;
            if (fd.ShowDialog() == DialogResult.OK)
            {
                int id = Convert.ToInt32(dgvProducts.SelectedRows[0].Cells["Id"].Value);
                var product = allProducts.FirstOrDefault(p => p.Id == id);
                if (product != null)
                {
                    product.ImagePath = fd.FileName;
                    ShowSelectedProductImage();
                    MessageBox.Show("Image attached to:\n" + product.Name, "Image Uploaded", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }
    }

    private void SyncCartWithProducts()
    {
        List<int> productIds = allProducts.Select(p => p.Id).ToList();
        var removedItems = cart.Where(item => !productIds.Contains(item.Product.Id)).ToList();
        if (removedItems.Any())
        {
            cart = cart.Where(item => productIds.Contains(item.Product.Id)).ToList();
            UpdateCartDisplay();
            MessageBox.Show(removedItems.Count + " item(s) were removed from your cart because the product was deleted.", "Product Removed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void FilterProducts()
    {
        dgvProducts.Rows.Clear();
        string keyword = txtSearch.Text.Trim().ToLower();
        filteredProducts = string.IsNullOrEmpty(keyword)
            ? allProducts
            : allProducts.Where(p => p.Name.ToLower().Contains(keyword) || p.Category.ToLower().Contains(keyword)).ToList();
        foreach (var p in filteredProducts)
        {
            dgvProducts.Rows.Add(p.Id, p.Name, p.Code, p.Brand, p.Category, "P" + p.Price.ToString("N2"), p.Email, p.GCashNumber, p.Stock);
        }
    }

    private async Task ShowAddProductDialog()
    {
        Form addForm = new Form { Text = "Add New Product", Size = new Size(420, 460), StartPosition = FormStartPosition.CenterParent, BackColor = LightYellow };

        Label lblName = new Label { Text = "Product Name:", Left = 30, Top = 20, Width = 300, Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.DarkRed };
        TextBox txtName = new TextBox { Left = 30, Top = 45, Width = 340, BackColor = Color.White };

        Label lblCode = new Label { Text = "Product Code:", Left = 30, Top = 80, Width = 300, Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.DarkRed };
        TextBox txtCode = new TextBox { Left = 30, Top = 105, Width = 340, BackColor = Color.White };

        Label lblBrand = new Label { Text = "Brand:", Left = 30, Top = 140, Width = 300, Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.DarkRed };
        TextBox txtBrand = new TextBox { Left = 30, Top = 165, Width = 340, BackColor = Color.White };

        Label lblCat = new Label { Text = "Category:", Left = 30, Top = 200, Width = 300, Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.DarkRed };
        TextBox txtCat = new TextBox { Left = 30, Top = 225, Width = 340, BackColor = Color.White };

        Label lblPrice = new Label { Text = "Price (P):", Left = 30, Top = 260, Width = 300, Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.DarkRed };
        TextBox txtPrice = new TextBox { Left = 30, Top = 285, Width = 340, BackColor = Color.White };

        Label lblStock = new Label { Text = "Stock Quantity:", Left = 30, Top = 320, Width = 300, Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.DarkRed };
        NumericUpDown nudStock = new NumericUpDown { Minimum = 1, Maximum = 9999, Value = 1, Left = 30, Top = 345, Width = 340, BackColor = Color.White };

        Button btnSave = new Button { Text = "Save Product", Left = 70, Top = 390, Width = 130, Height = 35, BackColor = Color.Firebrick, ForeColor = Color.White, Font = new Font("Segoe UI", 9F, FontStyle.Bold), FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.OK };
        Button btnCancel = new Button { Text = "Cancel", Left = 230, Top = 390, Width = 130, Height = 35, BackColor = Color.LightGray, Font = new Font("Segoe UI", 9F, FontStyle.Bold), FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.Cancel };

        addForm.Controls.AddRange(new Control[] { lblName, txtName, lblCode, txtCode, lblBrand, txtBrand, lblCat, txtCat, lblPrice, txtPrice, lblStock, nudStock, btnSave, btnCancel });

        if (addForm.ShowDialog() != DialogResult.OK) return;

        if (string.IsNullOrWhiteSpace(txtName.Text) || string.IsNullOrWhiteSpace(txtCode.Text) || string.IsNullOrWhiteSpace(txtBrand.Text) || string.IsNullOrWhiteSpace(txtCat.Text) || string.IsNullOrWhiteSpace(txtPrice.Text))
        {
            MessageBox.Show("Fill in ALL fields: Name, Code, Brand, Category, Price, and Stock!", "Missing Info", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (!decimal.TryParse(txtPrice.Text, out decimal price) || price <= 0)
        {
            MessageBox.Show("Enter a valid price!", "Invalid Price", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var newProduct = new Product
        {
            Name = txtName.Text.Trim(),
            Code = txtCode.Text.Trim(),
            Brand = txtBrand.Text.Trim(),
            Category = txtCat.Text.Trim(),
            Price = price,
            UnitPrice = price,
            Stock = (int)nudStock.Value
        };

        var (ok, msg) = await ApiClient.AddProductAsync(newProduct);
        if (ok)
        {
            MessageBox.Show(msg, "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            await LoadProducts();
        }
        else
        {
            MessageBox.Show("Failed:\n" + msg, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task DeleteSelectedProduct()
    {
        if (dgvProducts.SelectedRows.Count == 0)
        {
            MessageBox.Show("Select a product from the list first!", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        int id = Convert.ToInt32(dgvProducts.SelectedRows[0].Cells["Id"].Value);
        string name = dgvProducts.SelectedRows[0].Cells["Name"].Value?.ToString() ?? "";

        var confirm = MessageBox.Show(
            "Are you sure you want to DELETE:\n\n\"" + name + "\"?\n\nThis will also REMOVE it from the shopping cart!",
            "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

        if (confirm != DialogResult.Yes) return;

        var (ok, msg) = await ApiClient.DeleteProductAsync(id);
        if (ok)
        {
            MessageBox.Show(msg, "Deleted", MessageBoxButtons.OK, MessageBoxIcon.Information);
            await LoadProducts();
        }
        else
        {
            MessageBox.Show("Failed:\n" + msg, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task StartLogin()
    {
        while (true)
        {
            Form login = new Form { Text = "Login", Size = new Size(360, 300), StartPosition = FormStartPosition.CenterScreen, BackColor = LightYellow, TopMost = true };
            TextBox txtUser = new TextBox { PlaceholderText = "Username", Left = 30, Top = 40, Width = 280, Font = new Font("Segoe UI", 10F), BackColor = Color.White };
            TextBox txtPass = new TextBox { PlaceholderText = "Password", Left = 30, Top = 100, Width = 280, PasswordChar = '•', Font = new Font("Segoe UI", 10F), BackColor = Color.White };
            Label lblError = new Label { Text = "", Left = 30, Top = 160, Width = 280, ForeColor = Color.Red, Font = new Font("Segoe UI", 9F), BackColor = Color.Transparent };
            Button btnLogin = new Button { Text = "Login", Left = 110, Top = 210, Width = 140, Height = 40, BackColor = Color.Firebrick, ForeColor = Color.White, Font = new Font("Segoe UI", 10F, FontStyle.Bold), FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.OK };

            login.Controls.AddRange(new Control[] { txtUser, txtPass, lblError, btnLogin });
            login.AcceptButton = btnLogin;

            if (login.ShowDialog() != DialogResult.OK) { Close(); return; }

            string user = txtUser.Text.Trim();
            string pass = txtPass.Text.Trim();

            if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(pass))
            {
                lblError.Text = "Enter username and password!";
                continue;
            }

            lblError.Text = "Logging in...";
            var (result, error) = await ApiClient.LoginAsync(user, pass);

            if (result != null)
            {
                currentUser = result;
                lblStatus.Text = "Logged in: " + currentUser.Username + " (" + currentUser.Role + ")";
                login.Close();
                await LoadProducts();
                return;
            }

            lblError.Text = error;
        }
    }

    private async Task LoadProducts()
    {
        var (data, error) = await ApiClient.GetProductsAsync();
        if (data != null)
        {
            allProducts = data;
            SyncCartWithProducts();
            filteredProducts = allProducts;
            FilterProducts();
        }
    }

    private void AddToCart()
    {
        if (dgvProducts.SelectedRows.Count == 0) { MessageBox.Show("Select a product first!"); return; }
        int id = Convert.ToInt32(dgvProducts.SelectedRows[0].Cells["Id"].Value);
        var product = filteredProducts.FirstOrDefault(p => p.Id == id);
        if (product == null) return;

        int qty = (int)nudQty.Value;
        if (qty > product.Stock) { MessageBox.Show("Only " + product.Stock + " in stock!"); return; }

        var existing = cart.FirstOrDefault(c => c.Product.Id == id);
        if (existing != null) existing.Quantity += qty;
        else cart.Add(new CartItem { Product = product, Quantity = qty });

        UpdateCartDisplay();
    }

    private void RemoveFromCart()
    {
        int idx = -1;
        if (dgvCart.SelectedRows.Count > 0)
            idx = dgvCart.SelectedRows[0].Index;
        else if (dgvCart.CurrentRow != null)
            idx = dgvCart.CurrentRow.Index;

        if (idx < 0 || idx >= cart.Count)
        {
            MessageBox.Show("Select item in cart to remove!", "No Item Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        cart.RemoveAt(idx);
        UpdateCartDisplay();
    }

    private void UpdateCartDisplay()
    {
        dgvCart.Rows.Clear();
        decimal total = 0;
        foreach (var item in cart)
        {
            dgvCart.Rows.Add(item.Product.Name, item.Quantity, "P" + item.Subtotal.ToString("N2"));
            total += item.Subtotal;
        }
        lblTotal.Text = "TOTAL: P" + total.ToString("N2");
    }

    private CardInfo? ShowCardForm()
    {
        Form cardForm = new Form
        {
            Text = "Enter Card Details",
            Size = new Size(450, 450),
            MinimumSize = new Size(450, 450),
            MaximumSize = new Size(450, 450),
            StartPosition = FormStartPosition.CenterParent,
            BackColor = LightYellow,
            FormBorderStyle = FormBorderStyle.FixedDialog
        };

        Label lblNum = new Label { Text = "Card Number (16 digits):", Left = 30, Top = 30, Width = 380, Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.DarkRed };
        TextBox txtNum = new TextBox { Left = 30, Top = 55, Width = 380, MaxLength = 19, BackColor = Color.White };

        Label lblExp = new Label { Text = "Expiry Date (MM/YY):", Left = 30, Top = 95, Width = 380, Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.DarkRed };
        TextBox txtExp = new TextBox { Left = 30, Top = 120, Width = 380, MaxLength = 5, BackColor = Color.White };

        Label lblCVV = new Label { Text = "CVV (3 or 4 digits):", Left = 30, Top = 160, Width = 380, Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.DarkRed };
        TextBox txtCVV = new TextBox { Left = 30, Top = 185, Width = 380, MaxLength = 4, BackColor = Color.White };

        Label lblName = new Label { Text = "Cardholder Full Name:", Left = 30, Top = 225, Width = 380, Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.DarkRed };
        TextBox txtCardName = new TextBox { Left = 30, Top = 250, Width = 380, BackColor = Color.White };

        Label lblError = new Label { Text = "", Left = 30, Top = 295, Width = 380, ForeColor = Color.Red, Font = new Font("Segoe UI", 8F), BackColor = Color.Transparent };

        Button btnApprove = new Button { Text = "Approve & Pay", Left = 60, Top = 340, Width = 150, Height = 45, BackColor = Color.Firebrick, ForeColor = Color.White, Font = new Font("Segoe UI", 9F, FontStyle.Bold), FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.OK };
        Button btnCancel = new Button { Text = "Cancel", Left = 240, Top = 340, Width = 130, Height = 45, BackColor = Color.LightGray, Font = new Font("Segoe UI", 9F, FontStyle.Bold), FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.Cancel };

        cardForm.Controls.AddRange(new Control[] { lblNum, txtNum, lblExp, txtExp, lblCVV, txtCVV, lblName, txtCardName, lblError, btnApprove, btnCancel });

        while (true)
        {
            if (cardForm.ShowDialog() != DialogResult.OK) return null;

            string cardNum = Regex.Replace(txtNum.Text, @"[\s-]", "");
            string expiry = txtExp.Text.Trim();
            string cvv = txtCVV.Text.Trim();
            string cardholderName = txtCardName.Text.Trim();

            if (cardNum.Length < 13 || cardNum.Length > 19 || !cardNum.All(char.IsDigit))
            {
                lblError.Text = "Enter a valid card number (13-19 digits)!";
                continue;
            }

            if (!Regex.IsMatch(expiry, @"^(0[1-9]|1[0-2])\/\d{2}$"))
            {
                lblError.Text = "Use MM/YY format (e.g. 12/28)!";
                continue;
            }

            string[] parts = expiry.Split('/');
            int month = int.Parse(parts[0]);
            int year = int.Parse(parts[1]) + 2000;
            DateTime expiryDate = new DateTime(year, month, 1).AddMonths(1).AddDays(-1);
            if (expiryDate < DateTime.Now)
            {
                lblError.Text = "Card has expired!";
                continue;
            }

            if (cvv.Length < 3 || cvv.Length > 4 || !cvv.All(char.IsDigit))
            {
                lblError.Text = "Enter valid CVV (3-4 digits)!";
                continue;
            }

            if (cardholderName.Length < 3)
            {
                lblError.Text = "Enter cardholder full name!";
                continue;
            }

            MessageBox.Show("Card Approved Successfully!\nPayment Processed.", "Approved", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return new CardInfo
            {
                CardNumber = cardNum.Substring(cardNum.Length - 4).PadLeft(cardNum.Length, '*'),
                ExpiryDate = expiry,
                CVV = new string('*', cvv.Length),
                CardholderName = cardholderName
            };
        }
    }

    private string? ShowReceiptUploadWindow(List<CartItem> cartItems, string paymentMethod)
    {
        Form uploadForm = new Form
        {
            Text = "Upload Payment Proof - " + paymentMethod,
            Size = new Size(480, 600),
            MinimumSize = new Size(480, 600),
            MaximumSize = new Size(480, 600),
            StartPosition = FormStartPosition.CenterParent,
            BackColor = LightYellow,
            FormBorderStyle = FormBorderStyle.FixedDialog
        };

        var firstItem = cartItems.FirstOrDefault()?.Product;
        string sellerEmail = firstItem?.Email ?? "seller@marketplace.com";
        string sellerGCash = firstItem?.GCashNumber ?? "09XX-XXX-XXXX";

        Label lblSeller = new Label
        {
            Text = "SELLER PAYMENT DETAILS",
            Left = 30,
            Top = 15,
            Width = 420,
            Height = 50,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            ForeColor = Color.DarkRed,
            BackColor = Color.LightPink,
            TextAlign = ContentAlignment.MiddleCenter,
            Padding = new Padding(5)
        };

        Label lblEmail = new Label { Text = "Email: " + sellerEmail, Left = 30, Top = 75, Width = 420, Font = new Font("Segoe UI", 9F), ForeColor = Color.Black, BackColor = Color.Transparent };
        Label lblPhone = new Label { Text = "GCash: " + sellerGCash, Left = 30, Top = 100, Width = 420, Font = new Font("Segoe UI", 9F), ForeColor = Color.Black, BackColor = Color.Transparent };
        Label lblInstr = new Label { Text = "Upload your payment receipt/screenshot below:", Left = 30, Top = 135, Width = 420, Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.DarkRed, BackColor = Color.Transparent };
        Label lblFileName = new Label { Text = "No file selected", Left = 30, Top = 165, Width = 420, Height = 35, Font = new Font("Segoe UI", 9F), ForeColor = Color.Gray, BorderStyle = BorderStyle.FixedSingle, BackColor = Color.White, Padding = new Padding(8) };
        Label lblAddress = new Label { Text = "Complete Address:", Left = 30, Top = 210, Width = 420, Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.DarkRed, BackColor = Color.Transparent };
        TextBox txtAddress = new TextBox { Left = 30, Top = 235, Width = 420, Height = 60, Multiline = true, BackColor = Color.White };
        Label lblPhoneBuyer = new Label { Text = "Phone Number:", Left = 30, Top = 305, Width = 420, Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.DarkRed, BackColor = Color.Transparent };
        TextBox txtPhoneBuyer = new TextBox { Left = 30, Top = 330, Width = 420, BackColor = Color.White };
        Label lblEmailBuyer = new Label { Text = "Email Address:", Left = 30, Top = 370, Width = 420, Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.DarkRed, BackColor = Color.Transparent };
        TextBox txtEmailBuyer = new TextBox { Left = 30, Top = 395, Width = 420, BackColor = Color.White };

        Button btnUpload = new Button { Text = "Upload Receipt Image", Left = 30, Top = 440, Width = 420, Height = 50, BackColor = Color.Firebrick, ForeColor = Color.White, Font = new Font("Segoe UI", 10F, FontStyle.Bold), FlatStyle = FlatStyle.Flat };
        Button btnConfirm = new Button { Text = "Confirm Payment", Left = 60, Top = 510, Width = 160, Height = 45, BackColor = Color.Green, ForeColor = Color.White, Font = new Font("Segoe UI", 9F, FontStyle.Bold), FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.OK };
        Button btnCancel = new Button { Text = "Cancel", Left = 260, Top = 510, Width = 140, Height = 45, BackColor = Color.LightGray, Font = new Font("Segoe UI", 9F, FontStyle.Bold), FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.Cancel };

        string selectedFilePath = null;
        btnUpload.Click += (s, e) =>
        {
            using (OpenFileDialog fd = new OpenFileDialog())
            {
                fd.Title = "Select Payment Receipt Image";
                fd.Filter = "Image Files (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp";
                fd.RestoreDirectory = true;
                if (fd.ShowDialog() == DialogResult.OK)
                {
                    selectedFilePath = fd.FileName;
                    lblFileName.Text = Path.GetFileName(selectedFilePath);
                    lblFileName.ForeColor = Color.Green;
                }
            }
        };

        uploadForm.Controls.AddRange(new Control[] {
            lblSeller, lblEmail, lblPhone, lblInstr, lblFileName,
            lblAddress, txtAddress, lblPhoneBuyer, txtPhoneBuyer, lblEmailBuyer, txtEmailBuyer,
            btnUpload, btnConfirm, btnCancel
        });

        if (uploadForm.ShowDialog() == DialogResult.OK)
        {
            if (string.IsNullOrEmpty(selectedFilePath))
            {
                MessageBox.Show("Please upload a payment receipt first!", "Receipt Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }
            if (string.IsNullOrWhiteSpace(txtAddress.Text) || string.IsNullOrWhiteSpace(txtPhoneBuyer.Text) || string.IsNullOrWhiteSpace(txtEmailBuyer.Text))
            {
                MessageBox.Show("Please fill in Complete Address, Phone Number, and Email Address!", "Missing Info", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }
            return selectedFilePath;
        }
        return null;
    }

    private async Task ShowCheckoutDialog()
    {
        SyncCartWithProducts();
        if (cart.Count == 0) { MessageBox.Show("Cart is empty or items were removed!", "Cart Empty", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

        Form checkout = new Form { Text = "Checkout", Size = new Size(420, 380), StartPosition = FormStartPosition.CenterParent, BackColor = LightYellow };

        Label lblPay = new Label { Text = "Payment Method:", Left = 30, Top = 30, Width = 340, Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = Color.DarkRed };
        ComboBox cboPay = new ComboBox { Left = 30, Top = 60, Width = 340, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.White };
        cboPay.Items.AddRange(new[] { "GCash", "PayPal", "Credit Card / Debit Card" });
        cboPay.SelectedIndex = 0;

        Label lblShip = new Label { Text = "Shipping Method:", Left = 30, Top = 110, Width = 340, Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = Color.DarkRed };
        ComboBox cboShip = new ComboBox { Left = 30, Top = 140, Width = 340, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.White };
        cboShip.Items.AddRange(new[] { "Lalamove", "J&T Express" });
        cboShip.SelectedIndex = 0;

        decimal totalAmount = cart.Sum(i => i.Subtotal);
        Label lblTotalAmt = new Label { Text = "Total Amount: P" + totalAmount.ToString("N2"), Left = 30, Top = 190, Width = 340, Font = new Font("Segoe UI", 11F, FontStyle.Bold), ForeColor = Color.DarkRed };

        Button btnPlace = new Button { Text = "Place Order", Left = 50, Top = 260, Width = 140, Height = 45, BackColor = Color.Firebrick, ForeColor = Color.White, Font = new Font("Segoe UI", 10F, FontStyle.Bold), FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.OK };
        Button btnCancel = new Button { Text = "Cancel", Left = 220, Top = 260, Width = 140, Height = 45, BackColor = Color.LightGray, Font = new Font("Segoe UI", 10F, FontStyle.Bold), FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.Cancel };

        Label lblPaySel = new Label { Text = "Selected: " + cboPay.SelectedItem, Left = 30, Top = 320, Width = 340, ForeColor = Color.DarkRed, BackColor = Color.Transparent };
        cboPay.SelectedIndexChanged += (s, e) => lblPaySel.Text = "Selected: " + cboPay.SelectedItem;

        checkout.Controls.AddRange(new Control[] { lblPay, cboPay, lblShip, cboShip, lblTotalAmt, lblPaySel, btnPlace, btnCancel });

        while (true)
        {
            if (checkout.ShowDialog() != DialogResult.OK) return;

            string paymentMethod = cboPay.SelectedItem?.ToString() ?? "";
            string shippingMethod = cboShip.SelectedItem?.ToString() ?? "";
            DateTime orderDate = DateTime.Now;
            CardInfo? cardDetails = null;
            string? proofPath = null;

            if (paymentMethod == "GCash" || paymentMethod == "PayPal")
            {
                proofPath = ShowReceiptUploadWindow(cart, paymentMethod);
                if (proofPath == null) continue;
            }
            else if (paymentMethod.Contains("Credit Card") || paymentMethod.Contains("Debit Card"))
            {
                cardDetails = ShowCardForm();
                if (cardDetails == null) continue;
            }

            string confirmMsg = "Order Summary:\n\n" +
                "Payment: " + paymentMethod + "\n" +
                "Shipping: " + shippingMethod + "\n" +
                "Total: P" + totalAmount.ToString("N2") + "\n";
            if (proofPath != null) confirmMsg += "Receipt Uploaded\n";
            if (cardDetails != null) confirmMsg += "Cardholder: " + cardDetails.CardholderName + "\nCard Approved!\n";
            confirmMsg += "\nConfirm Order?";

            var confirm = MessageBox.Show(confirmMsg, "Confirm Checkout", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes) continue;

            var req = new CheckoutRequest
            {
                UserId = currentUser!.Id,
                Username = currentUser.Username,
                Items = cart,
                PaymentMethod = paymentMethod,
                ShippingMethod = shippingMethod,
                TotalAmount = totalAmount,
                OrderDate = orderDate,
                CardDetails = cardDetails,
                PaymentProofPath = proofPath
            };

            var (ok, msg) = await ApiClient.CheckoutAsync(req);
            if (ok)
            {
                foreach (var cartItem in cart)
                {
                    var product = allProducts.FirstOrDefault(p => p.Id == cartItem.Product.Id);
                    if (product != null) product.Stock -= cartItem.Quantity;
                }

                string receipt = GenerateReceipt(req);
                MessageBox.Show(receipt, "ORDER RECEIPT", MessageBoxButtons.OK, MessageBoxIcon.Information);

                cart.Clear();
                UpdateCartDisplay();
                FilterProducts();
                return;
            }
            else
            {
                MessageBox.Show("Failed:\n" + msg, "Checkout Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }
    }

    private string GenerateReceipt(CheckoutRequest order)
    {
        string lineSep = new string('-', 55);
        string itemSep = new string('-', 55);
        string itemsList = "";
        foreach (var item in order.Items)
        {
            itemsList += "  " + item.Product.Name.PadRight(25) + " x" + item.Quantity.ToString().PadLeft(3) + "  P" + item.Subtotal.ToString("N2").PadLeft(9) + "\n";
            itemsList += "     " + item.Product.Email.PadRight(35) + "\n";
            itemsList += "     " + item.Product.GCashNumber.PadRight(35) + "\n";
        }

        string cardInfo = order.CardDetails != null
            ? "  Cardholder: " + order.CardDetails.CardholderName + "\n  Card No: ****-" + order.CardDetails.CardNumber.Substring(order.CardDetails.CardNumber.Length - 4) + "\n  Expiry: " + order.CardDetails.ExpiryDate + "\n"
            : "";

        string receiptInfo = !string.IsNullOrEmpty(order.PaymentProofPath)
            ? "  Payment Proof: Uploaded Successfully\n"
            : "";

        string receipt =
            "\n M1-DELCARMEN MARKETPLACE\n" +
            "  OFFICIAL ORDER RECEIPT\n" +
            lineSep + "\n" +
            "  Date:     " + order.OrderDate.ToString("yyyy-MM-dd HH:mm:ss") + "\n" +
            "  Customer: " + order.Username + "\n" +
            itemSep + "\n" +
            "  ORDER ITEMS:\n" +
            itemsList +
            itemSep + "\n" +
            "  Payment:  " + order.PaymentMethod + "\n" +
            "  Shipping: " + order.ShippingMethod + "\n" +
            cardInfo + receiptInfo +
            lineSep + "\n" +
            "  TOTAL AMOUNT:  P" + order.TotalAmount.ToString("N2") + "\n" +
            lineSep + "\n" +
            "  Thank you for your order!\n\n";

        return receipt;
    }
}