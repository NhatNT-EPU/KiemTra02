using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Windows.Forms;

namespace InventoryManagerApp;

public class frmInventoryManager : Form
{
    private readonly BindingList<Product> _products = new();
    private readonly BindingSource _productsSource = new();
    private readonly BindingSource _editSource = new();
    private readonly ErrorProvider _errorProvider;
    private Product _editingProduct = new();
    private Product? _selectedProduct;
    private bool _allowClose;
    private bool _suppressSelectionChange;
    private Image? _displayedImage;

    private TextBox txtProductId = null!;
    private TextBox txtProductName = null!;
    private ComboBox cboCategory = null!;
    private TextBox txtPrice = null!;
    private NumericUpDown numQuantity = null!;
    private TextBox txtImagePath = null!;
    private PictureBox picProductImage = null!;
    private DataGridView dgvProducts = null!;

    private ToolStripStatusLabel statusItemCount = null!;
    private ToolStripStatusLabel statusTotalQuantity = null!;
    private ToolStripStatusLabel statusInventoryValue = null!;
    private ToolStripStatusLabel statusClock = null!;
    private readonly System.Windows.Forms.Timer _clockTimer = new();

    private static readonly string[] DefaultCategories =
    {
        "Điện tử", "Thiết bị mạng", "Phụ kiện", "Linh kiện", "Khác"
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public frmInventoryManager()
    {
        Text = "Phần mềm quản lý kho và xuất báo cáo tổng hợp";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1050, 650);
        ClientSize = new Size(1250, 760);
        Font = new Font("Segoe UI", 9F);

        _errorProvider = new ErrorProvider
        {
            ContainerControl = this,
            BlinkStyle = ErrorBlinkStyle.NeverBlink
        };

        InitializeLayout();
        ConfigureGrid();
        ConfigureBindings();
        WireEvents();
        SetEditingProduct(new Product { ProductId = GenerateNextId(_products) });
        RefreshStatus();

        _clockTimer.Interval = 1000;
        _clockTimer.Tick += (_, _) => UpdateClock();
        _clockTimer.Start();
        UpdateClock();
    }

    private void InitializeLayout()
    {
        MenuStrip menuStrip = CreateMenuStrip();
        ToolStrip toolStrip = CreateToolStrip();
        StatusStrip statusStrip = CreateStatusStrip();

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 25F));

        Control content = CreateContentPanel();
        root.Controls.Add(menuStrip, 0, 0);
        root.Controls.Add(toolStrip, 0, 1);
        root.Controls.Add(content, 0, 2);
        root.Controls.Add(statusStrip, 0, 3);
        Controls.Add(root);
        MainMenuStrip = menuStrip;

        FormClosed += (_, _) =>
        {
            _clockTimer.Stop();
            _clockTimer.Dispose();
            _displayedImage?.Dispose();
            _errorProvider.Dispose();
        };
    }

    private MenuStrip CreateMenuStrip()
    {
        var menu = new MenuStrip { Dock = DockStyle.Fill };
        var systemMenu = new ToolStripMenuItem("System");
        systemMenu.DropDownItems.Add("Open File", null, (_, _) => OpenDataFile());
        systemMenu.DropDownItems.Add("Save Report", null, (_, _) => SaveReport());
        systemMenu.DropDownItems.Add(new ToolStripSeparator());
        systemMenu.DropDownItems.Add("Exit", null, (_, _) => Close());

        var toolsMenu = new ToolStripMenuItem("Tools");
        toolsMenu.DropDownItems.Add("Import CSV", null, (_, _) => ImportCsvFromDialog());
        toolsMenu.DropDownItems.Add("Export CSV", null, (_, _) => ExportCsvFromDialog());

        menu.Items.Add(systemMenu);
        menu.Items.Add(toolsMenu);
        return menu;
    }

    private ToolStrip CreateToolStrip()
    {
        var strip = new ToolStrip
        {
            Dock = DockStyle.Fill,
            GripStyle = ToolStripGripStyle.Hidden,
            ImageScalingSize = new Size(18, 18),
            Padding = new Padding(5, 2, 5, 2)
        };

        strip.Items.Add(CreateToolButton("Mở file", SystemIcons.Application.ToBitmap(), () => OpenDataFile()));
        strip.Items.Add(CreateToolButton("Lưu báo cáo", SystemIcons.Information.ToBitmap(), () => SaveReport()));
        strip.Items.Add(new ToolStripSeparator());
        strip.Items.Add(CreateToolButton("Import CSV", SystemIcons.Question.ToBitmap(), () => ImportCsvFromDialog()));
        strip.Items.Add(CreateToolButton("Export CSV", SystemIcons.Asterisk.ToBitmap(), () => ExportCsvFromDialog()));
        strip.Items.Add(new ToolStripSeparator());
        strip.Items.Add(CreateToolButton("Thêm mới", SystemIcons.Information.ToBitmap(), () => AddProduct()));
        strip.Items.Add(CreateToolButton("Cập nhật", SystemIcons.Application.ToBitmap(), () => UpdateProduct()));
        strip.Items.Add(CreateToolButton("Xóa", SystemIcons.Warning.ToBitmap(), () => DeleteProduct()));
        strip.Items.Add(CreateToolButton("Thoát", SystemIcons.Error.ToBitmap(), () => Close()));
        return strip;
    }

    private static ToolStripButton CreateToolButton(string text, Image image, Action action)
    {
        var button = new ToolStripButton(text, image)
        {
            DisplayStyle = ToolStripItemDisplayStyle.ImageAndText,
            ImageTransparentColor = Color.Magenta,
            ToolTipText = text
        };
        button.Click += (_, _) => action();
        return button;
    }

    private StatusStrip CreateStatusStrip()
    {
        var strip = new StatusStrip { Dock = DockStyle.Fill, SizingGrip = false };
        statusItemCount = new ToolStripStatusLabel("Mặt hàng: 0");
        statusTotalQuantity = new ToolStripStatusLabel("Tổng số lượng: 0");
        statusInventoryValue = new ToolStripStatusLabel("Giá trị kho: 0 đ");
        statusClock = new ToolStripStatusLabel { Spring = true, TextAlign = ContentAlignment.MiddleRight };
        strip.Items.Add(statusItemCount);
        strip.Items.Add(new ToolStripStatusLabel("|") { ForeColor = SystemColors.GrayText });
        strip.Items.Add(statusTotalQuantity);
        strip.Items.Add(new ToolStripStatusLabel("|") { ForeColor = SystemColors.GrayText });
        strip.Items.Add(statusInventoryValue);
        strip.Items.Add(statusClock);
        return strip;
    }

    private Control CreateContentPanel()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(8),
            Margin = Padding.Empty
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 385F));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        layout.Controls.Add(CreateInputGroup(), 0, 0);
        layout.Controls.Add(CreateGridGroup(), 1, 0);
        return layout;
    }

    private Control CreateInputGroup()
    {
        var group = new GroupBox
        {
            Text = "Thông tin sản phẩm",
            Dock = DockStyle.Fill,
            Padding = new Padding(8),
            Margin = new Padding(0, 0, 8, 0)
        };

        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 8,
            Padding = new Padding(3),
            Margin = Padding.Empty
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 105F));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 160F));
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));

        txtProductId = new TextBox { Name = "txtProductId", ReadOnly = true, Dock = DockStyle.Fill, BackColor = SystemColors.Control };
        txtProductName = new TextBox { Name = "txtProductName", Dock = DockStyle.Fill, MaxLength = 150 };
        cboCategory = new ComboBox { Name = "cboCategory", Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        cboCategory.Items.Add(string.Empty);
        cboCategory.Items.AddRange(DefaultCategories.Cast<object>().ToArray());
        txtPrice = new TextBox { Name = "txtPrice", Dock = DockStyle.Fill };
        numQuantity = new NumericUpDown { Name = "numQuantity", Dock = DockStyle.Left, Width = 130, Minimum = 0, Maximum = 100000000, ThousandsSeparator = true };
        txtImagePath = new TextBox { Name = "txtImagePath", Dock = DockStyle.Fill, ReadOnly = true };

        AddInputRow(table, 0, "Mã sản phẩm", txtProductId);
        AddInputRow(table, 1, "Tên sản phẩm", txtProductName);
        AddInputRow(table, 2, "Loại sản phẩm", cboCategory);
        AddInputRow(table, 3, "Đơn giá (VNĐ)", txtPrice);
        AddInputRow(table, 4, "Số lượng tồn", numQuantity);
        AddInputRow(table, 5, "Đường dẫn ảnh", txtImagePath);

        var imagePanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            Padding = new Padding(0, 2, 0, 2),
            Margin = Padding.Empty
        };
        picProductImage = new PictureBox
        {
            Name = "picProductImage",
            Size = new Size(220, 112),
            SizeMode = PictureBoxSizeMode.Zoom,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.White
        };
        var btnChooseImage = new Button
        {
            Name = "btnChooseImage",
            Text = "Chọn ảnh...",
            Width = 110,
            Height = 30,
            Margin = new Padding(0, 5, 0, 0)
        };
        btnChooseImage.Click += (_, _) => ChooseProductImage();
        imagePanel.Controls.Add(picProductImage);
        imagePanel.Controls.Add(btnChooseImage);
        table.Controls.Add(new Label { Text = "Ảnh đại diện", Dock = DockStyle.Fill, TextAlign = ContentAlignment.TopLeft, Padding = new Padding(0, 5, 0, 0) }, 0, 6);
        table.Controls.Add(imagePanel, 1, 6);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Padding = new Padding(0, 5, 0, 0),
            Margin = Padding.Empty
        };
        var btnAdd = new Button { Name = "btnAdd", Text = "Thêm mới", Width = 78, Height = 30 };
        var btnUpdate = new Button { Name = "btnUpdate", Text = "Cập nhật", Width = 78, Height = 30 };
        var btnDelete = new Button { Name = "btnDelete", Text = "Xóa", Width = 55, Height = 30 };
        var btnClear = new Button { Name = "btnClear", Text = "Làm mới", Width = 75, Height = 30 };
        btnAdd.Click += (_, _) => AddProduct();
        btnUpdate.Click += (_, _) => UpdateProduct();
        btnDelete.Click += (_, _) => DeleteProduct();
        btnClear.Click += (_, _) => ClearEditor();
        buttons.Controls.AddRange(new Control[] { btnAdd, btnUpdate, btnDelete, btnClear });
        table.Controls.Add(buttons, 0, 7);
        table.SetColumnSpan(buttons, 2);

        group.Controls.Add(table);
        return group;
    }

    private static void AddInputRow(TableLayoutPanel table, int row, string label, Control control)
    {
        table.Controls.Add(new Label
        {
            Text = label,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(0, 0, 5, 0)
        }, 0, row);
        control.Margin = new Padding(0, 5, 0, 5);
        table.Controls.Add(control, 1, row);
    }

    private Control CreateGridGroup()
    {
        var group = new GroupBox
        {
            Text = "Danh sách sản phẩm trong kho",
            Dock = DockStyle.Fill,
            Padding = new Padding(8),
            Margin = Padding.Empty
        };
        dgvProducts = new DataGridView
        {
            Name = "dgvProducts",
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AutoGenerateColumns = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            MultiSelect = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            RowHeadersVisible = false,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.Fixed3D,
            Margin = Padding.Empty
        };
        group.Controls.Add(dgvProducts);
        return group;
    }

    private void ConfigureGrid()
    {
        dgvProducts.Columns.Clear();
        dgvProducts.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "colProductId", HeaderText = "Mã SP", DataPropertyName = nameof(Product.ProductId), FillWeight = 75,
            SortMode = DataGridViewColumnSortMode.Automatic
        });
        dgvProducts.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "colProductName", HeaderText = "Tên sản phẩm", DataPropertyName = nameof(Product.ProductName), FillWeight = 155,
            SortMode = DataGridViewColumnSortMode.Automatic
        });
        dgvProducts.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "colCategory", HeaderText = "Loại", DataPropertyName = nameof(Product.Category), FillWeight = 105,
            SortMode = DataGridViewColumnSortMode.Automatic
        });
        dgvProducts.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "colPrice", HeaderText = "Đơn giá (VNĐ)", DataPropertyName = nameof(Product.Price), FillWeight = 105,
            DefaultCellStyle = new DataGridViewCellStyle { Format = "#,##0' đ'", Alignment = DataGridViewContentAlignment.MiddleRight },
            SortMode = DataGridViewColumnSortMode.Automatic
        });
        dgvProducts.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "colQuantity", HeaderText = "Số lượng", DataPropertyName = nameof(Product.Quantity), FillWeight = 70,
            DefaultCellStyle = new DataGridViewCellStyle { Format = "N0", Alignment = DataGridViewContentAlignment.MiddleRight },
            SortMode = DataGridViewColumnSortMode.Automatic
        });
        dgvProducts.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "colImagePath", HeaderText = "File ảnh", DataPropertyName = nameof(Product.ImagePath), FillWeight = 95,
            SortMode = DataGridViewColumnSortMode.NotSortable
        });
        dgvProducts.CellFormatting += DgvProducts_CellFormatting;
        dgvProducts.DataSource = _productsSource;
    }

    private void ConfigureBindings()
    {
        _productsSource.DataSource = _products;
        dgvProducts.DataSource = _productsSource;

        _editSource.DataSource = _editingProduct;
        txtProductId.DataBindings.Add(nameof(TextBox.Text), _editSource, nameof(Product.ProductId), true, DataSourceUpdateMode.OnPropertyChanged);
        txtProductName.DataBindings.Add(nameof(TextBox.Text), _editSource, nameof(Product.ProductName), true, DataSourceUpdateMode.OnPropertyChanged);
        cboCategory.DataBindings.Add(nameof(ComboBox.SelectedItem), _editSource, nameof(Product.Category), true, DataSourceUpdateMode.OnPropertyChanged);
        numQuantity.DataBindings.Add(nameof(NumericUpDown.Value), _editSource, nameof(Product.Quantity), true, DataSourceUpdateMode.OnPropertyChanged);
        txtImagePath.DataBindings.Add(nameof(TextBox.Text), _editSource, nameof(Product.ImagePath), true, DataSourceUpdateMode.OnPropertyChanged);

        _products.ListChanged += (_, _) => RefreshStatus();
    }

    private void WireEvents()
    {
        dgvProducts.SelectionChanged += DgvProducts_SelectionChanged;
        txtImagePath.TextChanged += (_, _) => DisplayProductImage(txtImagePath.Text);
        FormClosing += FrmInventoryManager_FormClosing;
    }

    private void DgvProducts_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (dgvProducts.Columns[e.ColumnIndex].Name == "colImagePath" && e.Value is string imagePath)
        {
            e.Value = string.IsNullOrWhiteSpace(imagePath) ? string.Empty : Path.GetFileName(imagePath);
            e.FormattingApplied = true;
        }
    }

    private void DgvProducts_SelectionChanged(object? sender, EventArgs e)
    {
        if (_suppressSelectionChange || dgvProducts.SelectedRows.Count == 0)
            return;

        if (dgvProducts.SelectedRows[0].DataBoundItem is Product selected)
        {
            _selectedProduct = selected;
            EnsureCategoryExists(selected.Category);
            SetEditingProduct(selected.Copy());
        }
    }

    private void SetEditingProduct(Product product)
    {
        _editingProduct = product;
        _editSource.DataSource = _editingProduct;
        _editSource.ResetBindings(false);
        txtPrice.Text = product.Price == 0 && string.IsNullOrWhiteSpace(product.ProductName)
            ? string.Empty
            : product.Price.ToString("0.##", CultureInfo.CurrentCulture);
        DisplayProductImage(product.ImagePath);
        ClearValidationErrors();
    }

    private void EnsureCategoryExists(string? category)
    {
        if (string.IsNullOrWhiteSpace(category))
            return;

        bool exists = cboCategory.Items.Cast<object>().Any(item =>
            string.Equals(item?.ToString(), category, StringComparison.OrdinalIgnoreCase));
        if (!exists)
            cboCategory.Items.Add(category);
    }

    private void ChooseProductImage()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Chọn ảnh sản phẩm",
            Filter = "File ảnh (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp|JPEG (*.jpg;*.jpeg)|*.jpg;*.jpeg|PNG (*.png)|*.png|Bitmap (*.bmp)|*.bmp",
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _editingProduct.ImagePath = dialog.FileName;
            txtImagePath.Text = dialog.FileName;
            DisplayProductImage(dialog.FileName);
        }
    }

    private void DisplayProductImage(string? imagePath)
    {
        _displayedImage?.Dispose();
        _displayedImage = null;
        picProductImage.Image = null;

        if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath))
            return;

        try
        {
            // Tao ban sao de khong khoa file anh tren dia.
            using Image sourceImage = Image.FromFile(imagePath);
            _displayedImage = new Bitmap(sourceImage);
            picProductImage.Image = _displayedImage;
        }
        catch (Exception)
        {
            // Neu file anh khong doc duoc thi de PictureBox trong.
            picProductImage.Image = null;
        }
    }

    private bool ValidateInput(out decimal price)
    {
        ClearValidationErrors();
        bool valid = true;
        price = 0;

        if (string.IsNullOrWhiteSpace(txtProductName.Text))
        {
            _errorProvider.SetError(txtProductName, "Tên sản phẩm không được để trống.");
            valid = false;
        }

        if (cboCategory.SelectedItem == null || string.IsNullOrWhiteSpace(cboCategory.SelectedItem.ToString()))
        {
            _errorProvider.SetError(cboCategory, "Vui lòng chọn loại sản phẩm.");
            valid = false;
        }

        if (!TryParsePrice(txtPrice.Text, out price))
        {
            _errorProvider.SetError(txtPrice, "Đơn giá phải là số hợp lệ và không nhỏ hơn 0.");
            valid = false;
        }
        else if (price < 0)
        {
            _errorProvider.SetError(txtPrice, "Đơn giá không được nhỏ hơn 0.");
            valid = false;
        }

        return valid;
    }

    private static bool TryParsePrice(string text, out decimal price)
    {
        return decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out price)
            || decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out price);
    }

    private void ClearValidationErrors()
    {
        _errorProvider.SetError(txtProductName, string.Empty);
        _errorProvider.SetError(cboCategory, string.Empty);
        _errorProvider.SetError(txtPrice, string.Empty);
    }

    private void AddProduct()
    {
        if (!ValidateInput(out decimal price))
        {
            MessageBox.Show(this, "Vui lòng kiểm tra lại các thông tin được đánh dấu.", "Dữ liệu chưa hợp lệ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var product = new Product
        {
            ProductId = GenerateNextId(_products),
            ProductName = txtProductName.Text.Trim(),
            Category = cboCategory.SelectedItem?.ToString() ?? string.Empty,
            Price = price,
            Quantity = (int)numQuantity.Value,
            ImagePath = _editingProduct.ImagePath ?? string.Empty
        };

        _products.Add(product);
        _selectedProduct = null;
        ClearGridSelection();
        SetEditingProduct(new Product { ProductId = GenerateNextId(_products) });
    }

    private void UpdateProduct()
    {
        if (_selectedProduct == null)
        {
            MessageBox.Show(this, "Vui lòng chọn sản phẩm cần cập nhật trên bảng.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (!ValidateInput(out decimal price))
        {
            MessageBox.Show(this, "Vui lòng kiểm tra lại các thông tin được đánh dấu.", "Dữ liệu chưa hợp lệ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _selectedProduct.ProductName = txtProductName.Text.Trim();
        _selectedProduct.Category = cboCategory.SelectedItem?.ToString() ?? string.Empty;
        _selectedProduct.Price = price;
        _selectedProduct.Quantity = (int)numQuantity.Value;
        _selectedProduct.ImagePath = _editingProduct.ImagePath ?? string.Empty;
        RefreshStatus();
        MessageBox.Show(this, "Cập nhật sản phẩm thành công.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void DeleteProduct()
    {
        if (_selectedProduct == null)
        {
            MessageBox.Show(this, "Vui lòng chọn sản phẩm cần xóa trên bảng.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        DialogResult answer = MessageBox.Show(
            this,
            $"Bạn có chắc chắn muốn xóa sản phẩm '{_selectedProduct.ProductName}' không?",
            "Xác nhận xóa",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (answer == DialogResult.Yes)
        {
            _products.Remove(_selectedProduct);
            _selectedProduct = null;
            ClearEditor();
        }
    }

    private void ClearEditor()
    {
        _selectedProduct = null;
        ClearGridSelection();
        SetEditingProduct(new Product { ProductId = GenerateNextId(_products) });
        numQuantity.Value = 0;
        txtPrice.Clear();
        ClearValidationErrors();
    }

    private void ClearGridSelection()
    {
        _suppressSelectionChange = true;
        dgvProducts.ClearSelection();
        dgvProducts.CurrentCell = null;
        _suppressSelectionChange = false;
    }

    private void RefreshStatus()
    {
        int itemCount = _products.Count;
        int totalQuantity = _products.Sum(p => p.Quantity);
        decimal totalValue = _products.Sum(p => p.Price * p.Quantity);

        statusItemCount.Text = $"Mặt hàng: {itemCount}";
        statusTotalQuantity.Text = $"Tổng số lượng: {totalQuantity:N0}";
        statusInventoryValue.Text = $"Giá trị kho: {totalValue:N0} đ";
    }

    private void UpdateClock()
    {
        statusClock.Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
    }

    private static string GenerateNextId(IEnumerable<Product> products)
    {
        int maximum = 0;
        foreach (Product product in products)
        {
            string id = product.ProductId ?? string.Empty;
            if (id.StartsWith("SP", StringComparison.OrdinalIgnoreCase)
                && int.TryParse(id.Substring(2), out int number)
                && number > maximum)
            {
                maximum = number;
            }
        }
        return $"SP{maximum + 1:000}";
    }

    private void OpenDataFile()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Mở dữ liệu kho",
            Filter = "Tệp dữ liệu (*.csv;*.txt;*.json)|*.csv;*.txt;*.json|CSV (*.csv)|*.csv|JSON (*.json)|*.json|Text (*.txt)|*.txt|Tất cả tệp (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
            ImportDataFile(dialog.FileName);
    }

    private void ImportCsvFromDialog()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Import dữ liệu CSV",
            Filter = "CSV (*.csv)|*.csv|Tất cả tệp (*.*)|*.*",
            DefaultExt = "csv",
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
            ImportDelimitedFile(dialog.FileName);
    }

    private void ImportDataFile(string filePath)
    {
        string extension = Path.GetExtension(filePath).ToLowerInvariant();
        if (extension == ".json")
        {
            try
            {
                string json = File.ReadAllText(filePath, Encoding.UTF8);
                List<Product>? loaded = JsonSerializer.Deserialize<List<Product>>(json, JsonOptions);
                if (loaded == null)
                {
                    MessageBox.Show(this, "Không đọc được danh sách sản phẩm trong tệp JSON.", "Lỗi dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                ReplaceProducts(loaded, 0, filePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Không thể đọc file JSON. Chi tiết: " + ex.Message, "Lỗi đọc file", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        else
        {
            ImportDelimitedFile(filePath);
        }
    }

    private void ImportDelimitedFile(string filePath)
    {
        try
        {
            string[] lines = File.ReadAllLines(filePath, Encoding.UTF8);
            string? firstLine = lines.FirstOrDefault(line => !string.IsNullOrWhiteSpace(line));
            if (firstLine == null)
            {
                MessageBox.Show(this, "File không có dữ liệu.", "Import dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            char delimiter = DetectDelimiter(firstLine, Path.GetExtension(filePath));
            var products = new List<Product>();
            int invalidRows = 0;
            bool firstNonEmptyLine = true;

            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                List<string> fields = ParseDelimitedLine(line.TrimStart('\uFEFF'), delimiter);
                if (firstNonEmptyLine)
                {
                    firstNonEmptyLine = false;
                    if (fields.Count > 0 && IsHeaderRow(fields[0]))
                        continue;
                }

                if (fields.Count < 5)
                {
                    invalidRows++;
                    continue;
                }

                string rawPrice = fields[3].Trim();
                string rawQuantity = fields[4].Trim();
                bool priceOK = decimal.TryParse(rawPrice, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal price)
                    || decimal.TryParse(rawPrice, NumberStyles.Number, CultureInfo.CurrentCulture, out price);
                bool quantityOK = int.TryParse(rawQuantity, NumberStyles.Integer, CultureInfo.InvariantCulture, out int quantity)
                    || int.TryParse(rawQuantity, NumberStyles.Integer, CultureInfo.CurrentCulture, out quantity);

                if (!priceOK || price < 0 || !quantityOK || quantity < 0)
                {
                    invalidRows++;
                    continue;
                }

                products.Add(new Product
                {
                    ProductId = fields[0].Trim(),
                    ProductName = fields[1].Trim(),
                    Category = fields[2].Trim(),
                    Price = price,
                    Quantity = quantity,
                    ImagePath = fields.Count >= 6 ? fields[5].Trim() : string.Empty
                });
            }

            if (products.Any(p => string.IsNullOrWhiteSpace(p.ProductName)))
            {
                invalidRows += products.RemoveAll(p => string.IsNullOrWhiteSpace(p.ProductName));
            }

            ReplaceProducts(products, invalidRows, filePath);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Không thể nhập dữ liệu. Chi tiết: " + ex.Message, "Lỗi đọc file", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ReplaceProducts(List<Product> importedProducts, int invalidRows, string filePath)
    {
        if (importedProducts.Count == 0)
        {
            MessageBox.Show(this, "Không tìm thấy bản ghi hợp lệ. Danh sách hiện tại được giữ nguyên.", "Import dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        DialogResult answer = MessageBox.Show(
            this,
            $"Đọc được {importedProducts.Count} sản phẩm từ file. Bạn có muốn thay thế danh sách hiện tại bằng dữ liệu này không?",
            "Xác nhận nhập dữ liệu",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (answer != DialogResult.Yes)
            return;

        // Neu ma san pham bi trong hoac trung, tu dong cap ma khac.
        var usedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (Product product in importedProducts)
        {
            string id = product.ProductId?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(id) || !usedIds.Add(id))
            {
                string newId = GenerateNextId(importedProducts.Where(p => !ReferenceEquals(p, product)));
                while (usedIds.Contains(newId))
                {
                    var temporary = importedProducts.Where(p => !ReferenceEquals(p, product)).ToList();
                    temporary.Add(new Product { ProductId = newId });
                    newId = GenerateNextId(temporary);
                }
                product.ProductId = newId;
                usedIds.Add(newId);
            }

            EnsureCategoryExists(product.Category);
        }

        _products.Clear();
        foreach (Product product in importedProducts)
            _products.Add(product);

        _selectedProduct = null;
        ClearGridSelection();
        SetEditingProduct(new Product { ProductId = GenerateNextId(_products) });
        RefreshStatus();

        string message = $"Đã nhập {importedProducts.Count} sản phẩm từ file:\n{filePath}";
        if (invalidRows > 0)
            message += $"\nCó {invalidRows} dòng không hợp lệ đã được bỏ qua.";
        MessageBox.Show(this, message, "Import thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private static bool IsHeaderRow(string firstField)
    {
        string value = firstField.Trim().Trim('\uFEFF');
        return value.Equals("ProductId", StringComparison.OrdinalIgnoreCase)
            || value.Equals("Mã sản phẩm", StringComparison.OrdinalIgnoreCase)
            || value.Equals("Ma san pham", StringComparison.OrdinalIgnoreCase);
    }

    private static char DetectDelimiter(string line, string extension)
    {
        char[] candidates = { ',', ';', '\t', '|' };
        char best = extension.Equals(".txt", StringComparison.OrdinalIgnoreCase) ? '\t' : ',';
        int bestCount = -1;
        foreach (char candidate in candidates)
        {
            int count = CountDelimiterOutsideQuotes(line, candidate);
            if (count > bestCount)
            {
                bestCount = count;
                best = candidate;
            }
        }
        return best;
    }

    private static int CountDelimiterOutsideQuotes(string line, char delimiter)
    {
        bool inQuotes = false;
        int count = 0;
        for (int i = 0; i < line.Length; i++)
        {
            if (line[i] == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    i++;
                else
                    inQuotes = !inQuotes;
            }
            else if (line[i] == delimiter && !inQuotes)
            {
                count++;
            }
        }
        return count;
    }

    private static List<string> ParseDelimitedLine(string line, char delimiter)
    {
        var fields = new List<string>();
        var current = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char character = line[i];
            if (character == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (character == delimiter && !inQuotes)
            {
                fields.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(character);
            }
        }

        fields.Add(current.ToString());
        return fields;
    }

    private void ExportCsvFromDialog()
    {
        using var dialog = new SaveFileDialog
        {
            Title = "Export danh sách sản phẩm ra CSV",
            Filter = "CSV (*.csv)|*.csv",
            DefaultExt = "csv",
            AddExtension = true,
            FileName = $"Inventory_{DateTime.Now:yyyyMMdd_HHmmss}.csv",
            OverwritePrompt = true
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
            ExportDelimitedFile(dialog.FileName, ',');
    }

    private void SaveReport()
    {
        using var dialog = new SaveFileDialog
        {
            Title = "Lưu báo cáo tổng hợp",
            Filter = "CSV (*.csv)|*.csv|JSON (*.json)|*.json|Text dạng bảng (*.txt)|*.txt",
            FilterIndex = 1,
            DefaultExt = "csv",
            AddExtension = true,
            FileName = $"BaoCaoKho_{DateTime.Now:yyyyMMdd_HHmmss}.csv",
            OverwritePrompt = true
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            // Dung dinh dang theo loai file nguoi dung chon trong SaveFileDialog.
            string expectedExtension = dialog.FilterIndex switch
            {
                2 => ".json",
                3 => ".txt",
                _ => ".csv"
            };
            string outputPath = Path.ChangeExtension(dialog.FileName, expectedExtension);

            if (expectedExtension == ".json")
            {
                string json = JsonSerializer.Serialize(_products.ToList(), JsonOptions);
                File.WriteAllText(outputPath, json, new UTF8Encoding(true));
            }
            else
            {
                char delimiter = expectedExtension == ".txt" ? '\t' : ',';
                ExportDelimitedFile(outputPath, delimiter, showMessage: false);
            }

            MessageBox.Show(this, $"Lưu báo cáo thành công.\n{outputPath}", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Không thể lưu báo cáo. Chi tiết: " + ex.Message, "Lỗi ghi file", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ExportDelimitedFile(string filePath, char delimiter, bool showMessage = true)
    {
        try
        {
            using var writer = new StreamWriter(filePath, false, new UTF8Encoding(true));
            writer.WriteLine(string.Join(delimiter.ToString(), new[] { "ProductId", "ProductName", "Category", "Price", "Quantity", "ImagePath" }
                .Select(value => EscapeDelimitedField(value, delimiter))));

            foreach (Product product in _products)
            {
                string[] values =
                {
                    product.ProductId,
                    product.ProductName,
                    product.Category,
                    product.Price.ToString(CultureInfo.InvariantCulture),
                    product.Quantity.ToString(CultureInfo.InvariantCulture),
                    product.ImagePath
                };
                writer.WriteLine(string.Join(delimiter.ToString(), values.Select(value => EscapeDelimitedField(value ?? string.Empty, delimiter))));
            }

            if (showMessage)
                MessageBox.Show(this, $"Đã xuất {_products.Count} sản phẩm ra file:\n{filePath}", "Export thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            if (!showMessage)
                throw;

            MessageBox.Show(this, "Không thể xuất dữ liệu. Chi tiết: " + ex.Message, "Lỗi ghi file", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static string EscapeDelimitedField(string value, char delimiter)
    {
        if (value.Contains(delimiter) || value.Contains('"') || value.Contains('\r') || value.Contains('\n'))
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        return value;
    }

    private void FrmInventoryManager_FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_allowClose)
            return;

        DialogResult answer = MessageBox.Show(
            this,
            "Bạn có chắc chắn muốn thoát chương trình quản lý kho không?",
            "Xác nhận thoát",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (answer == DialogResult.Yes)
            _allowClose = true;
        else
            e.Cancel = true;
    }
}
