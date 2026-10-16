using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace SupermarketMDIApp;

// Form con quan ly san pham, ket hop y tuong CRUD cua Bai 1 va anh/CSV cua Bai 2.
public class frmProductCatalog : Form
{
    private readonly BindingSource _productSource = new();
    private readonly ErrorProvider _errorProvider;
    private Product? _selectedProduct;
    private Image? _previewImage;
    private bool _changingSelection;

    private TextBox txtProductId = null!;
    private TextBox txtProductName = null!;
    private ComboBox cboCategory = null!;
    private TextBox txtPrice = null!;
    private NumericUpDown numQuantity = null!;
    private TextBox txtImagePath = null!;
    private PictureBox picProductImage = null!;
    private DataGridView dgvProducts = null!;

    private static readonly string[] Categories =
    {
        "Điện tử", "Thiết bị mạng", "Phụ kiện", "Linh kiện", "Đồ gia dụng", "Khác"
    };

    public frmProductCatalog()
    {
        Text = "Quản lý sản phẩm và nhập xuất kho";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(1050, 620);
        ClientSize = new Size(1250, 720);
        Font = new Font("Segoe UI", 9F);

        _errorProvider = new ErrorProvider
        {
            ContainerControl = this,
            BlinkStyle = ErrorBlinkStyle.NeverBlink
        };

        BuildLayout();
        ConfigureGrid();
        _productSource.DataSource = InventoryData.Products;
        dgvProducts.DataSource = _productSource;
        WireEvents();
        ClearInput();

        FormClosed += (_, _) =>
        {
            _previewImage?.Dispose();
            _errorProvider.Dispose();
        };
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(8),
            Margin = Padding.Empty
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 410F));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        root.Controls.Add(BuildInputGroup(), 0, 0);
        root.Controls.Add(BuildGridGroup(), 1, 0);
        Controls.Add(root);
    }

    private Control BuildInputGroup()
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
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110F));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 150F));
        table.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        txtProductId = new TextBox { ReadOnly = true, Dock = DockStyle.Fill, BackColor = SystemColors.Control };
        txtProductName = new TextBox { Dock = DockStyle.Fill, MaxLength = 150 };
        cboCategory = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        cboCategory.Items.AddRange(Categories.Cast<object>().ToArray());
        txtPrice = new TextBox { Dock = DockStyle.Fill };
        numQuantity = new NumericUpDown { Dock = DockStyle.Left, Width = 140, Minimum = 0, Maximum = 100000000, ThousandsSeparator = true };

        AddRow(table, 0, "Mã sản phẩm", txtProductId);
        AddRow(table, 1, "Tên sản phẩm", txtProductName);
        AddRow(table, 2, "Loại sản phẩm", cboCategory);
        AddRow(table, 3, "Đơn giá (VNĐ)", txtPrice);
        AddRow(table, 4, "Số lượng tồn", numQuantity);

        var imagePathPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = Padding.Empty };
        imagePathPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        imagePathPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 86F));
        txtImagePath = new TextBox { Dock = DockStyle.Fill, ReadOnly = true };
        var btnChooseImage = new Button { Text = "Chọn ảnh", Dock = DockStyle.Fill, Margin = new Padding(4, 0, 0, 0) };
        btnChooseImage.Click += (_, _) => ChooseImage();
        imagePathPanel.Controls.Add(txtImagePath, 0, 0);
        imagePathPanel.Controls.Add(btnChooseImage, 1, 0);
        AddRow(table, 5, "Ảnh sản phẩm", imagePathPanel);

        picProductImage = new PictureBox
        {
            Dock = DockStyle.Fill,
            SizeMode = PictureBoxSizeMode.Zoom,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.White,
            Margin = new Padding(0, 4, 0, 4)
        };
        table.Controls.Add(picProductImage, 1, 6);
        table.Controls.Add(new Label
        {
            Text = "Ảnh xem trước",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 6);

        var buttonsPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            AutoScroll = true,
            Padding = new Padding(0, 5, 0, 0)
        };
        AddActionButton(buttonsPanel, "Thêm mới", (_, _) => AddProduct());
        AddActionButton(buttonsPanel, "Cập nhật", (_, _) => UpdateProduct());
        AddActionButton(buttonsPanel, "Xóa", (_, _) => DeleteProduct());
        AddActionButton(buttonsPanel, "Làm mới", (_, _) => ClearInput());
        AddActionButton(buttonsPanel, "Import CSV", (_, _) => ImportCsv());
        AddActionButton(buttonsPanel, "Export CSV", (_, _) => ExportCsv());
        table.Controls.Add(buttonsPanel, 0, 7);
        table.SetColumnSpan(buttonsPanel, 2);

        group.Controls.Add(table);
        return group;
    }

    private static void AddRow(TableLayoutPanel table, int row, string label, Control control)
    {
        table.Controls.Add(new Label
        {
            Text = label,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, row);
        control.Margin = new Padding(2, 5, 2, 5);
        table.Controls.Add(control, 1, row);
    }

    private static void AddActionButton(FlowLayoutPanel panel, string text, EventHandler handler)
    {
        var button = new Button { Text = text, Width = 94, Height = 32, Margin = new Padding(3) };
        button.Click += handler;
        panel.Controls.Add(button);
    }

    private Control BuildGridGroup()
    {
        var group = new GroupBox { Text = "Danh sách sản phẩm", Dock = DockStyle.Fill, Padding = new Padding(8) };
        dgvProducts = new DataGridView
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
        group.Controls.Add(dgvProducts);
        return group;
    }

    private void ConfigureGrid()
    {
        dgvProducts.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(Product.ProductId),
            HeaderText = "Mã SP",
            FillWeight = 65
        });
        dgvProducts.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(Product.ProductName),
            HeaderText = "Tên sản phẩm",
            FillWeight = 145
        });
        dgvProducts.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(Product.Category),
            HeaderText = "Loại",
            FillWeight = 90
        });
        dgvProducts.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(Product.Price),
            HeaderText = "Đơn giá",
            FillWeight = 95,
            DefaultCellStyle = new DataGridViewCellStyle { Format = "N0" }
        });
        dgvProducts.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(Product.Quantity),
            HeaderText = "Số lượng",
            FillWeight = 65
        });
        dgvProducts.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "ImageFileColumn",
            HeaderText = "Tên file ảnh",
            FillWeight = 95
        });

        dgvProducts.CellFormatting += (_, e) =>
        {
            if (e.RowIndex < 0 || dgvProducts.Columns[e.ColumnIndex].Name != "ImageFileColumn")
                return;

            if (dgvProducts.Rows[e.RowIndex].DataBoundItem is Product product)
                e.Value = string.IsNullOrWhiteSpace(product.ImagePath) ? string.Empty : Path.GetFileName(product.ImagePath);
        };
    }

    private void WireEvents()
    {
        dgvProducts.SelectionChanged += (_, _) =>
        {
            if (_changingSelection || dgvProducts.CurrentRow?.DataBoundItem is not Product product)
                return;
            ShowProduct(product);
        };
    }

    private void ShowProduct(Product product)
    {
        _selectedProduct = product;
        txtProductId.Text = product.ProductId;
        txtProductName.Text = product.ProductName;
        cboCategory.SelectedItem = Categories.Contains(product.Category) ? product.Category : null;
        txtPrice.Text = product.Price.ToString("0.##", CultureInfo.CurrentCulture);
        numQuantity.Value = Math.Max(numQuantity.Minimum, Math.Min(numQuantity.Maximum, product.Quantity));
        txtImagePath.Text = product.ImagePath;
        LoadPreviewImage(product.ImagePath);
        _errorProvider.Clear();
    }

    private void ClearInput()
    {
        _changingSelection = true;
        dgvProducts.ClearSelection();
        _changingSelection = false;
        _selectedProduct = null;
        txtProductId.Text = InventoryData.GenerateNextId();
        txtProductName.Clear();
        cboCategory.SelectedIndex = -1;
        txtPrice.Clear();
        numQuantity.Value = 0;
        txtImagePath.Clear();
        LoadPreviewImage(string.Empty);
        _errorProvider.Clear();
        txtProductName.Focus();
    }

    private bool ValidateInput(out decimal price)
    {
        _errorProvider.Clear();
        bool valid = true;
        price = 0;

        if (string.IsNullOrWhiteSpace(txtProductName.Text))
        {
            _errorProvider.SetError(txtProductName, "Tên sản phẩm không được để trống.");
            valid = false;
        }

        if (cboCategory.SelectedIndex < 0)
        {
            _errorProvider.SetError(cboCategory, "Vui lòng chọn loại sản phẩm.");
            valid = false;
        }

        if (!decimal.TryParse(txtPrice.Text, NumberStyles.Currency, CultureInfo.CurrentCulture, out price) || price < 0)
        {
            _errorProvider.SetError(txtPrice, "Đơn giá phải là số hợp lệ và không âm.");
            valid = false;
        }

        return valid;
    }

    private void AddProduct()
    {
        if (!ValidateInput(out decimal price))
            return;

        var product = new Product
        {
            ProductId = InventoryData.GenerateNextId(),
            ProductName = txtProductName.Text.Trim(),
            Category = cboCategory.SelectedItem?.ToString() ?? "Khác",
            Price = price,
            Quantity = (int)numQuantity.Value,
            ImagePath = txtImagePath.Text.Trim()
        };

        InventoryData.Products.Add(product);
        _productSource.ResetBindings(false);
        SelectProduct(product);
    }

    private void UpdateProduct()
    {
        if (_selectedProduct == null)
        {
            MessageBox.Show("Vui lòng chọn sản phẩm cần cập nhật.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (!ValidateInput(out decimal price))
            return;

        _selectedProduct.ProductName = txtProductName.Text.Trim();
        _selectedProduct.Category = cboCategory.SelectedItem?.ToString() ?? "Khác";
        _selectedProduct.Price = price;
        _selectedProduct.Quantity = (int)numQuantity.Value;
        _selectedProduct.ImagePath = txtImagePath.Text.Trim();
        _productSource.ResetBindings(false);
        SelectProduct(_selectedProduct);
    }

    private void DeleteProduct()
    {
        if (_selectedProduct == null)
        {
            MessageBox.Show("Vui lòng chọn sản phẩm cần xóa.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        DialogResult result = MessageBox.Show(
            $"Bạn có chắc muốn xóa sản phẩm '{_selectedProduct.ProductName}'?",
            "Xác nhận xóa",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result != DialogResult.Yes)
            return;

        InventoryData.Products.Remove(_selectedProduct);
        _productSource.ResetBindings(false);
        ClearInput();
    }

    private void SelectProduct(Product product)
    {
        int rowIndex = -1;
        for (int i = 0; i < dgvProducts.Rows.Count; i++)
        {
            if (ReferenceEquals(dgvProducts.Rows[i].DataBoundItem, product))
            {
                rowIndex = i;
                break;
            }
        }

        if (rowIndex >= 0)
        {
            dgvProducts.ClearSelection();
            dgvProducts.Rows[rowIndex].Selected = true;
            dgvProducts.CurrentCell = dgvProducts.Rows[rowIndex].Cells[0];
        }
        ShowProduct(product);
    }

    private void ChooseImage()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Chọn ảnh sản phẩm",
            Filter = "Tệp ảnh (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp",
            Multiselect = false
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        txtImagePath.Text = dialog.FileName;
        LoadPreviewImage(dialog.FileName);
    }

    private void LoadPreviewImage(string path)
    {
        _previewImage?.Dispose();
        _previewImage = null;
        picProductImage.Image = null;

        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return;

        try
        {
            using Image source = Image.FromFile(path);
            _previewImage = new Bitmap(source);
            picProductImage.Image = _previewImage;
        }
        catch
        {
            picProductImage.Image = null;
        }
    }

    private void ImportCsv()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Chọn file CSV để nhập sản phẩm",
            Filter = "CSV (*.csv)|*.csv|Tất cả tệp (*.*)|*.*"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        DialogResult confirm = MessageBox.Show(
            "Nhập CSV sẽ thay thế danh sách sản phẩm hiện tại. Bạn có muốn tiếp tục?",
            "Xác nhận nhập dữ liệu",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);
        if (confirm != DialogResult.Yes)
            return;

        try
        {
            var imported = new List<Product>();
            string[] lines = File.ReadAllLines(dialog.FileName);

            for (int i = 0; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i]))
                    continue;
                if (i == 0 && lines[i].StartsWith("ProductId", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (CsvUtility.TryCreateProduct(lines[i], out Product? product) && product != null)
                    imported.Add(product);
            }

            InventoryData.Products.RaiseListChangedEvents = false;
            InventoryData.Products.Clear();
            foreach (Product product in imported)
                InventoryData.Products.Add(product);
            InventoryData.Products.RaiseListChangedEvents = true;
            _productSource.ResetBindings(false);
            ClearInput();

            MessageBox.Show($"Đã nhập {imported.Count} sản phẩm.", "Hoàn thành", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            InventoryData.Products.RaiseListChangedEvents = true;
            _productSource.ResetBindings(false);
            MessageBox.Show("Không thể đọc file CSV. Chi tiết: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ExportCsv()
    {
        using var dialog = new SaveFileDialog
        {
            Title = "Xuất danh sách sản phẩm",
            Filter = "CSV (*.csv)|*.csv",
            FileName = "Products.csv",
            DefaultExt = "csv",
            AddExtension = true
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            var lines = new List<string>
            {
                "ProductId,ProductName,Category,Price,Quantity,ImagePath"
            };

            foreach (Product product in InventoryData.Products)
            {
                lines.Add(CsvUtility.ToCsvLine(new[]
                {
                    product.ProductId,
                    product.ProductName,
                    product.Category,
                    product.Price.ToString(CultureInfo.InvariantCulture),
                    product.Quantity.ToString(CultureInfo.InvariantCulture),
                    product.ImagePath
                }));
            }

            File.WriteAllLines(dialog.FileName, lines);
            MessageBox.Show("Đã xuất danh sách sản phẩm thành công.", "Hoàn thành", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Không thể xuất CSV. Chi tiết: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
