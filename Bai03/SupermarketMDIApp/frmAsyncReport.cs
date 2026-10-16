using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Threading;
using System.Windows.Forms;

namespace SupermarketMDIApp;

public class frmAsyncReport : Form
{
    private readonly CancellationTokenSource _lifetimeCts = new();
    private readonly BindingSource _reportSource = new();
    private Button btnLoadReport = null!;
    private Label lblReportStatus = null!;
    private Label lblRevenue = null!;
    private Label lblSyncStatus = null!;
    private DataGridView dgvReport = null!;
    private DataGridView dgvSyncLog = null!;
    private int _simulatedOrderCount;
    private bool _loadingReport;

    public frmAsyncReport()
    {
        Text = "Báo cáo thống kê doanh thu và đồng bộ bất đồng bộ";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(1000, 650);
        ClientSize = new Size(1180, 760);
        Font = new Font("Segoe UI", 9F);

        BuildLayout();
        ConfigureReportGrid();
        ConfigureSyncGrid();

        Shown += (_, _) => StartBackgroundSync();
        FormClosing += (_, _) => _lifetimeCts.Cancel();
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(10),
            Margin = Padding.Empty
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 118F));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 56F));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 44F));

        var topPanel = new GroupBox { Text = "Báo cáo doanh thu (dữ liệu minh họa)", Dock = DockStyle.Fill, Padding = new Padding(8) };
        var topLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 3, Padding = new Padding(3) };
        topLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 230F));
        topLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        topLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 37F));
        topLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
        topLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));

        btnLoadReport = new Button
        {
            Text = "Tải Báo Cáo Doanh Thu",
            Dock = DockStyle.Fill,
            Height = 32,
            Margin = new Padding(2, 1, 8, 1)
        };
        btnLoadReport.Click += async (_, _) => await LoadRevenueReportAsync();
        topLayout.Controls.Add(btnLoadReport, 0, 0);
        topLayout.SetRowSpan(btnLoadReport, 2);

        lblReportStatus = new Label { Text = "Nhấn nút để tải báo cáo (mất khoảng 4 giây).", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
        lblRevenue = new Label { Text = "Tổng doanh thu mô phỏng: 0 đ", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI", 10F, FontStyle.Bold) };
        topLayout.Controls.Add(lblReportStatus, 1, 0);
        topLayout.Controls.Add(lblRevenue, 1, 1);

        lblSyncStatus = new Label { Text = "Đồng bộ nền: đang khởi tạo...", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
        topLayout.Controls.Add(lblSyncStatus, 0, 2);
        topLayout.SetColumnSpan(lblSyncStatus, 2);
        topPanel.Controls.Add(topLayout);

        var reportGroup = new GroupBox { Text = "Chi tiết báo cáo (số lượng bán được giả lập bằng 1/3 tồn kho)", Dock = DockStyle.Fill, Padding = new Padding(8) };
        dgvReport = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoGenerateColumns = false,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            MultiSelect = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            RowHeadersVisible = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            BackgroundColor = Color.White
        };
        reportGroup.Controls.Add(dgvReport);

        var syncGroup = new GroupBox { Text = "Nhật ký đồng bộ theo thời gian thực (mỗi 2 giây)", Dock = DockStyle.Fill, Padding = new Padding(8) };
        dgvSyncLog = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoGenerateColumns = false,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            RowHeadersVisible = false,
            MultiSelect = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            BackgroundColor = Color.White
        };
        syncGroup.Controls.Add(dgvSyncLog);

        root.Controls.Add(topPanel, 0, 0);
        root.Controls.Add(reportGroup, 0, 1);
        root.Controls.Add(syncGroup, 0, 2);
        Controls.Add(root);
    }

    private void ConfigureReportGrid()
    {
        dgvReport.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(ReportRow.ProductId), HeaderText = "Mã SP", FillWeight = 65 });
        dgvReport.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(ReportRow.ProductName), HeaderText = "Tên sản phẩm", FillWeight = 150 });
        dgvReport.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(ReportRow.Price), HeaderText = "Đơn giá", FillWeight = 90, DefaultCellStyle = new DataGridViewCellStyle { Format = "N0" } });
        dgvReport.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(ReportRow.StockQuantity), HeaderText = "Tồn kho", FillWeight = 65 });
        dgvReport.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(ReportRow.SimulatedSold), HeaderText = "SL bán giả lập", FillWeight = 85 });
        dgvReport.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(ReportRow.Revenue), HeaderText = "Doanh thu giả lập", FillWeight = 110, DefaultCellStyle = new DataGridViewCellStyle { Format = "N0" } });
        dgvReport.DataSource = _reportSource;
    }

    private void ConfigureSyncGrid()
    {
        dgvSyncLog.Columns.Add(new DataGridViewTextBoxColumn { Name = "Time", HeaderText = "Thời gian", FillWeight = 110 });
        dgvSyncLog.Columns.Add(new DataGridViewTextBoxColumn { Name = "Message", HeaderText = "Thông báo đồng bộ", FillWeight = 220 });
        dgvSyncLog.Columns.Add(new DataGridViewTextBoxColumn { Name = "Count", HeaderText = "Số đơn (mô phỏng)", FillWeight = 100 });
    }

    private async Task LoadRevenueReportAsync()
    {
        if (_loadingReport)
            return;

        _loadingReport = true;
        btnLoadReport.Enabled = false;
        lblReportStatus.Text = "Đang tải dữ liệu và tính toán... Giao diện vẫn có thể thao tác.";

        if (MdiParent is frmMain mainForm)
            mainForm.SetBusy(true, "Đang tải báo cáo doanh thu...");

        // Doc du lieu tren UI Thread va tao ban sao truoc khi chuyen qua Thread phu.
        List<ProductSnapshot> snapshot = InventoryData.Products
            .Select(product => new ProductSnapshot
            {
                ProductId = product.ProductId,
                ProductName = product.ProductName,
                Price = product.Price,
                Quantity = product.Quantity
            })
            .ToList();

        try
        {
            // Task.Run chay tren ThreadPool; Task.Delay mo phong truy xuat CSDL mat 4 giay.
            List<ReportRow> reportRows = await Task.Run(async () =>
            {
                await Task.Delay(4000, _lifetimeCts.Token);

                var rows = new List<ReportRow>();
                foreach (ProductSnapshot item in snapshot)
                {
                    int simulatedSold = item.Quantity / 3;
                    rows.Add(new ReportRow
                    {
                        ProductId = item.ProductId,
                        ProductName = item.ProductName,
                        Price = item.Price,
                        StockQuantity = item.Quantity,
                        SimulatedSold = simulatedSold,
                        Revenue = item.Price * simulatedSold
                    });
                }
                return rows;
            }, _lifetimeCts.Token);

            _reportSource.DataSource = new BindingList<ReportRow>(reportRows);
            decimal totalRevenue = reportRows.Sum(row => row.Revenue);
            lblRevenue.Text = "Tổng doanh thu mô phỏng: " + totalRevenue.ToString("N0", CultureInfo.GetCultureInfo("vi-VN")) + " đ";
            lblReportStatus.Text = $"Đã tải báo cáo thành công lúc {DateTime.Now:HH:mm:ss}.";
        }
        catch (OperationCanceledException)
        {
            // Form dang dong thi huy tac vu dang chay.
        }
        catch (Exception ex)
        {
            MessageBox.Show("Không thể tải báo cáo. Chi tiết: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            lblReportStatus.Text = "Tải báo cáo thất bại.";
        }
        finally
        {
            _loadingReport = false;
            if (!IsDisposed && !Disposing)
                btnLoadReport.Enabled = true;
            if (MdiParent is frmMain parent && !parent.IsDisposed)
                parent.SetBusy(false);
        }
    }

    private void StartBackgroundSync()
    {
        // Tac vu vong lap chay tren ThreadPool, khong doc/ghi Control truc tiep trong vong lap.
        _ = Task.Run(async () =>
        {
            try
            {
                while (!_lifetimeCts.Token.IsCancellationRequested)
                {
                    await Task.Delay(2000, _lifetimeCts.Token);
                    int count = Interlocked.Increment(ref _simulatedOrderCount);
                    UpdateSyncUiSafely("Đã nhận dữ liệu đơn hàng mới (mô phỏng)", count);
                }
            }
            catch (OperationCanceledException)
            {
                // Ket thuc vong lap khi Form dong.
            }
        }, _lifetimeCts.Token);
    }

    // Quy tac Safe Cross-Thread: neu dang o Thread phu thi Invoke ve UI Thread.
    private void UpdateSyncUiSafely(string message, int count)
    {
        if (IsDisposed || !IsHandleCreated)
            return;

        if (InvokeRequired)
        {
            try
            {
                this.Invoke(new Action(() => UpdateSyncUiSafely(message, count)));
            }
            catch (ObjectDisposedException)
            {
                // Bo qua cap nhat neu control da duoc dispose.
            }
            catch (InvalidOperationException)
            {
                // Form co the da dong trong luc Thread phu gui cap nhat.
            }
            return;
        }

        lblSyncStatus.Text = $"Đồng bộ nền: lần cập nhật gần nhất {DateTime.Now:HH:mm:ss} | Số đơn mô phỏng: {count}";
        dgvSyncLog.Rows.Insert(0, DateTime.Now.ToString("HH:mm:ss"), message, count);
        if (dgvSyncLog.Rows.Count > 40)
            dgvSyncLog.Rows.RemoveAt(dgvSyncLog.Rows.Count - 1);
    }

    private sealed class ProductSnapshot
    {
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
    }

    private sealed class ReportRow
    {
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public int SimulatedSold { get; set; }
        public decimal Revenue { get; set; }
    }
}
