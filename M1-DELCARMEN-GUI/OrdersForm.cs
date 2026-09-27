using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace M1_DELCARMEN_GUI;

public partial class OrdersForm : Form
{
    private readonly int _currentUserId;
    private readonly string _userRole;
    private DataGridView dgvOrders;
    private Button btnShip;

    public OrdersForm(int currentUserId, string userRole)
    {
        _currentUserId = currentUserId;
        _userRole = userRole;
        SetupForm();
        _ = LoadOrdersAsync();
    }

    private void SetupForm()
    {
        Text = "Sales / Orders";
        Size = new Size(900, 550);
        BackColor = Color.LightYellow;

        dgvOrders = new DataGridView
        {
            Dock = DockStyle.Top,
            Height = 450,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            ReadOnly = true
        };
        Controls.Add(dgvOrders);

        btnShip = new Button
        {
            Text = "Ship — Waiting for Courier",
            BackColor = Color.Green,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Dock = DockStyle.Bottom,
            Height = 50,
            Enabled = false
        };
        btnShip.Click += async (s, e) => await MarkAsShippedAsync();
        Controls.Add(btnShip);

        dgvOrders.SelectionChanged += (s, e) =>
        {
            if (dgvOrders.SelectedRows.Count == 0)
            {
                btnShip.Enabled = false;
                return;
            }
            var status = dgvOrders.SelectedRows[0].Cells["Status"].Value?.ToString();
            btnShip.Enabled = _userRole == "Admin" && status == "Pending";
        };

        if (_userRole != "Admin")
        {
            MessageBox.Show(
                "Access Denied:\nYou need to be an Admin to view this page.",
                "Restricted",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            Close();
        }
    }

    private async Task LoadOrdersAsync()
    {
        try
        {
            var orders = await ApiClient.GetAllOrdersAsync();
            dgvOrders.DataSource = null;

            
            var displayList = orders.Select(o => new
            {
                o.Id,
                BuyerUsername = o.BuyerUsername,
                Items = string.Join("; ", o.OrderItems.Select(i => $"{i.ProductName} x{i.Quantity}")),
                o.TotalAmount,
                o.PaymentMethod,
                o.ShippingMethod,
                Status = o.IsShipped ? "Waiting for courier" : "Pending",
                o.OrderDate
            }).ToList();

            dgvOrders.DataSource = displayList;
            dgvOrders.Columns["Id"].Visible = false;
        }
        catch (Exception ex)
        {
            MessageBox.Show("Error loading orders: " + ex.Message, "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task MarkAsShippedAsync()
    {
        if (dgvOrders.SelectedRows.Count == 0) return;

        var orderId = (int)dgvOrders.SelectedRows[0].Cells["Id"].Value;
        var confirm = MessageBox.Show(
            "Mark this order as Shipped?\nStatus will show: Waiting for courier",
            "Confirm Shipment",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (confirm != DialogResult.Yes) return;

        var success = await ApiClient.MarkOrderAsShippedAsync(orderId);
        if (success)
        {
            MessageBox.Show(
                "Order Shipped!\nStatus: Waiting for courier",
                "Shipped",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            await LoadOrdersAsync();
        }
        else
        {
            MessageBox.Show("Failed to update status", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}