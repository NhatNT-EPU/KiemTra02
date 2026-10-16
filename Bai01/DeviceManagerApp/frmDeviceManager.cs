using System.ComponentModel;
using System.Globalization;
using System.Drawing;
using System.Windows.Forms;

namespace DeviceManagerApp;

public class frmDeviceManager : Form
{
    private readonly BindingList<Device> _devices = new();
    private readonly BindingSource _bindingSource = new();

    // BindingSource riêng cho dữ liệu đang nhập/sửa, tránh sửa trực tiếp danh sách gốc
    // trước khi người dùng nhấn nút Cập nhật.
    private readonly BindingList<Device> _editingDevices = new();
    private readonly BindingSource _editingBindingSource = new();
    private readonly ErrorProvider _errorProvider = new();

    private bool _isCleared = true;
    private string? _editingOriginalId;
    private int _nextId = 1;

    private readonly TextBox txtDeviceId = new();
    private readonly TextBox txtDeviceName = new();
    private readonly ComboBox cboCategory = new();
    private readonly TextBox txtPrice = new();
    private readonly NumericUpDown numQuantity = new();
    private readonly DataGridView dgvDevices = new();

    private readonly Button btnAdd = new();
    private readonly Button btnUpdate = new();
    private readonly Button btnDelete = new();
    private readonly Button btnClear = new();

    private const string CategoryPlaceholder = "-- Chọn loại thiết bị --";

    public frmDeviceManager()
    {
        BuildInterface();
        ConfigureGridColumns();
        ConfigureDataBinding();
        RegisterEvents();

        _errorProvider.ContainerControl = this;
        _errorProvider.BlinkStyle = ErrorBlinkStyle.NeverBlink;

        ClearInputs();
        UpdateButtonState();
    }

    // Tạo giao diện bằng code, không cần file Designer.
    private void BuildInterface()
    {
        Text = "Quản lý danh mục thiết bị điện tử";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(900, 620);
        Size = new Size(1100, 720);
        Font = new Font("Segoe UI", 10F, FontStyle.Regular);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(10)
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 190F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58F));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        Controls.Add(root);

        var groupInput = new GroupBox
        {
            Text = "Thông tin thiết bị",
            Dock = DockStyle.Fill,
            Padding = new Padding(8)
        };

        var inputLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 3,
            Padding = new Padding(8)
        };
        inputLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15F));
        inputLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));
        inputLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15F));
        inputLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));
        for (int i = 0; i < 3; i++)
        {
            inputLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33F));
        }

        txtDeviceId.ReadOnly = true;
        txtDeviceId.BackColor = SystemColors.Control;

        cboCategory.DropDownStyle = ComboBoxStyle.DropDownList;
        cboCategory.DataSource = new List<string>
        {
            CategoryPlaceholder,
            "Điện thoại",
            "Máy tính",
            "Máy tính bảng",
            "Thiết bị âm thanh",
            "Thiết bị gia dụng",
            "Phụ kiện",
            "Khác"
        };

        txtPrice.PlaceholderText = "Nhập đơn giá";
        numQuantity.Minimum = 0;
        numQuantity.Maximum = 1000000;
        numQuantity.ThousandsSeparator = true;

        AddField(inputLayout, "Mã thiết bị:", txtDeviceId, 0, 0);
        AddField(inputLayout, "Tên thiết bị:", txtDeviceName, 2, 0);
        AddField(inputLayout, "Loại thiết bị:", cboCategory, 0, 1);
        AddField(inputLayout, "Đơn giá:", txtPrice, 2, 1);
        AddField(inputLayout, "Số lượng tồn:", numQuantity, 0, 2);

        var note = new Label
        {
            Text = "Mã thiết bị được tạo tự động khi thêm mới.",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoSize = false,
            ForeColor = SystemColors.GrayText,
            Margin = new Padding(3, 4, 3, 4)
        };
        inputLayout.Controls.Add(note, 2, 2);
        inputLayout.SetColumnSpan(note, 2);

        groupInput.Controls.Add(inputLayout);
        root.Controls.Add(groupInput, 0, 0);

        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Padding = new Padding(0, 8, 4, 4)
        };

        ConfigureButton(btnAdd, "Thêm mới");
        ConfigureButton(btnUpdate, "Cập nhật");
        ConfigureButton(btnDelete, "Xóa");
        ConfigureButton(btnClear, "Làm mới / Hủy");

        buttonPanel.Controls.Add(btnAdd);
        buttonPanel.Controls.Add(btnUpdate);
        buttonPanel.Controls.Add(btnDelete);
        buttonPanel.Controls.Add(btnClear);
        root.Controls.Add(buttonPanel, 0, 1);

        var groupGrid = new GroupBox
        {
            Text = "Danh sách thiết bị",
            Dock = DockStyle.Fill,
            Padding = new Padding(8)
        };

        dgvDevices.Dock = DockStyle.Fill;
        dgvDevices.AutoGenerateColumns = false;
        dgvDevices.ReadOnly = true;
        dgvDevices.AllowUserToAddRows = false;
        dgvDevices.AllowUserToDeleteRows = false;
        dgvDevices.AllowUserToResizeRows = false;
        dgvDevices.MultiSelect = false;
        dgvDevices.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgvDevices.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        dgvDevices.RowHeadersVisible = false;
        dgvDevices.BackgroundColor = Color.White;
        dgvDevices.BorderStyle = BorderStyle.Fixed3D;
        dgvDevices.ColumnHeadersHeight = 38;
        dgvDevices.RowTemplate.Height = 32;
        dgvDevices.EnableHeadersVisualStyles = false;
        dgvDevices.ColumnHeadersDefaultCellStyle.Font = new Font(Font, FontStyle.Bold);
        dgvDevices.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        dgvDevices.SelectionChanged += DgvDevices_SelectionChanged;

        groupGrid.Controls.Add(dgvDevices);
        root.Controls.Add(groupGrid, 0, 2);
    }

    private static void AddField(TableLayoutPanel layout, string caption, Control input, int labelColumn, int row)
    {
        var label = new Label
        {
            Text = caption,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoSize = false,
            Margin = new Padding(3, 3, 8, 3)
        };

        input.Dock = DockStyle.Fill;
        input.Margin = new Padding(3, 5, 14, 5);

        layout.Controls.Add(label, labelColumn, row);
        layout.Controls.Add(input, labelColumn + 1, row);
    }

    private static void ConfigureButton(Button button, string caption)
    {
        button.Text = caption;
        button.Width = 125;
        button.Height = 35;
        button.Margin = new Padding(5, 0, 5, 0);
        button.Cursor = Cursors.Hand;
        button.FlatStyle = FlatStyle.System;
    }

    // Khai báo thủ công các cột của DataGridView.
    private void ConfigureGridColumns()
    {
        dgvDevices.Columns.Clear();

        dgvDevices.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "colDeviceId",
            HeaderText = "Mã thiết bị",
            DataPropertyName = nameof(Device.DeviceId),
            FillWeight = 16,
            MinimumWidth = 100,
            SortMode = DataGridViewColumnSortMode.NotSortable
        });

        dgvDevices.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "colDeviceName",
            HeaderText = "Tên thiết bị",
            DataPropertyName = nameof(Device.DeviceName),
            FillWeight = 28,
            MinimumWidth = 150,
            SortMode = DataGridViewColumnSortMode.NotSortable
        });

        dgvDevices.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "colCategory",
            HeaderText = "Loại thiết bị",
            DataPropertyName = nameof(Device.Category),
            FillWeight = 22,
            MinimumWidth = 130,
            SortMode = DataGridViewColumnSortMode.NotSortable
        });

        var priceColumn = new DataGridViewTextBoxColumn
        {
            Name = "colPrice",
            HeaderText = "Đơn giá",
            DataPropertyName = nameof(Device.Price),
            FillWeight = 20,
            MinimumWidth = 120,
            SortMode = DataGridViewColumnSortMode.NotSortable
        };
        priceColumn.DefaultCellStyle.Format = "c0";
        priceColumn.DefaultCellStyle.FormatProvider = CultureInfo.GetCultureInfo("vi-VN");
        priceColumn.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        dgvDevices.Columns.Add(priceColumn);

        var quantityColumn = new DataGridViewTextBoxColumn
        {
            Name = "colQuantity",
            HeaderText = "Số lượng tồn",
            DataPropertyName = nameof(Device.Quantity),
            FillWeight = 14,
            MinimumWidth = 100,
            SortMode = DataGridViewColumnSortMode.NotSortable
        };
        quantityColumn.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        dgvDevices.Columns.Add(quantityColumn);
    }

    private void ConfigureDataBinding()
    {
        // BindingSource chính liên kết BindingList<Device> với DataGridView.
        _bindingSource.DataSource = _devices;
        dgvDevices.DataSource = _bindingSource;

        // BindingSource phụ giữ bản sao đang được chỉnh sửa trên Form.
        _editingBindingSource.DataSource = _editingDevices;

        // Data Binding hai chiều: thay đổi Control cập nhật bản sao đang sửa,
        // khi chọn dòng khác thì dữ liệu từ bản sao mới được đưa lên các Control.
        txtDeviceId.DataBindings.Add(
            "Text", _editingBindingSource, nameof(Device.DeviceId), true, DataSourceUpdateMode.OnPropertyChanged);

        txtDeviceName.DataBindings.Add(
            "Text", _editingBindingSource, nameof(Device.DeviceName), true, DataSourceUpdateMode.OnPropertyChanged);

        cboCategory.DataBindings.Add(
            "SelectedItem", _editingBindingSource, nameof(Device.Category), true, DataSourceUpdateMode.OnPropertyChanged);

        // Giá được cập nhật khi TextBox được xác thực; việc này giúp người dùng
        // có thể gõ số đang dở mà không làm phát sinh lỗi chuyển kiểu ngay lập tức.
        var priceBinding = new Binding(
            "Text", _editingBindingSource, nameof(Device.Price), true,
            DataSourceUpdateMode.OnValidation, 0m, "N0");

        priceBinding.Parse += (_, e) =>
        {
            string input = Convert.ToString(e.Value, CultureInfo.CurrentCulture) ?? string.Empty;
            if (TryParsePrice(input, out decimal parsedPrice))
            {
                e.Value = parsedPrice;
            }
            else if (_editingBindingSource.Current is Device editingDevice)
            {
                // Giữ nguyên giá trị đã có nếu người dùng đang nhập số chưa hợp lệ.
                e.Value = editingDevice.Price;
            }
            else
            {
                e.Value = 0m;
            }
        };
        txtPrice.DataBindings.Add(priceBinding);

        numQuantity.DataBindings.Add(
            "Value", _editingBindingSource, nameof(Device.Quantity), true, DataSourceUpdateMode.OnPropertyChanged);
    }

    private void RegisterEvents()
    {
        btnAdd.Click += BtnAdd_Click;
        btnUpdate.Click += BtnUpdate_Click;
        btnDelete.Click += BtnDelete_Click;
        btnClear.Click += BtnClear_Click;

        txtDeviceName.TextChanged += (_, _) => _errorProvider.SetError(txtDeviceName, string.Empty);
        cboCategory.SelectedIndexChanged += (_, _) => _errorProvider.SetError(cboCategory, string.Empty);
        txtPrice.TextChanged += (_, _) => _errorProvider.SetError(txtPrice, string.Empty);
    }

    private void DgvDevices_SelectionChanged(object? sender, EventArgs e)
    {
        if (dgvDevices.CurrentRow?.DataBoundItem is Device selectedDevice &&
            dgvDevices.SelectedRows.Count > 0)
        {
            int itemIndex = _devices.IndexOf(selectedDevice);
            if (itemIndex >= 0)
            {
                _bindingSource.Position = itemIndex;

                // Không nạp lại nếu vẫn đang chọn cùng một thiết bị, vì có thể
                // người dùng đang chỉnh sửa bản nháp của dòng đó.
                if (_editingOriginalId != selectedDevice.DeviceId)
                {
                    LoadDeviceIntoEditor(selectedDevice);
                }

                _isCleared = false;
                ClearValidationErrors();
            }
        }

        UpdateButtonState();
    }

    private void BtnAdd_Click(object? sender, EventArgs e)
    {
        if (!ValidateInput())
        {
            return;
        }

        TryParsePrice(txtPrice.Text, out decimal price);
        string category = Convert.ToString(cboCategory.SelectedItem) ?? string.Empty;

        var newDevice = new Device
        {
            DeviceId = GenerateDeviceId(),
            DeviceName = txtDeviceName.Text.Trim(),
            Category = category,
            Price = price,
            Quantity = (int)numQuantity.Value
        };

        _devices.Add(newDevice);
        int newIndex = _devices.Count - 1;
        SelectRowAt(newIndex);
        MessageBox.Show("Thêm thiết bị thành công.", "Thông báo",
            MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void BtnUpdate_Click(object? sender, EventArgs e)
    {
        if (_isCleared || _bindingSource.Current is not Device originalDevice ||
            _editingBindingSource.Current is not Device editingDevice)
        {
            MessageBox.Show("Bạn hãy chọn một thiết bị trên danh sách cần cập nhật.",
                "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (!ValidateInput())
        {
            return;
        }

        if (!TryParsePrice(txtPrice.Text, out decimal price))
        {
            return;
        }

        // Các Control đã cập nhật hai chiều vào editingDevice.
        // Chỉ sau khi xác thực hợp lệ, sao chép dữ liệu từ bản nháp vào danh sách chính.
        editingDevice.DeviceName = txtDeviceName.Text.Trim();
        editingDevice.Category = Convert.ToString(cboCategory.SelectedItem) ?? string.Empty;
        editingDevice.Price = price;
        editingDevice.Quantity = (int)numQuantity.Value;

        originalDevice.DeviceName = editingDevice.DeviceName;
        originalDevice.Category = editingDevice.Category;
        originalDevice.Price = editingDevice.Price;
        originalDevice.Quantity = editingDevice.Quantity;

        _bindingSource.EndEdit();
        _bindingSource.ResetCurrentItem();
        dgvDevices.Refresh();

        MessageBox.Show("Cập nhật thiết bị thành công.", "Thông báo",
            MessageBoxButtons.OK, MessageBoxIcon.Information);
    }


    private void BtnDelete_Click(object? sender, EventArgs e)
    {
        if (_isCleared || _bindingSource.Current is not Device selectedDevice)
        {
            MessageBox.Show("Bạn hãy chọn một thiết bị trên danh sách cần xóa.",
                "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        DialogResult result = MessageBox.Show(
            $"Bạn có chắc chắn muốn xóa thiết bị '{selectedDevice.DeviceName}' không?",
            "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Question,
            MessageBoxDefaultButton.Button2);

        if (result != DialogResult.Yes)
        {
            return;
        }

        int deletedIndex = _bindingSource.Position;
        _devices.Remove(selectedDevice);

        if (_devices.Count > 0)
        {
            int nextIndex = Math.Min(deletedIndex, _devices.Count - 1);
            SelectRowAt(nextIndex);
        }
        else
        {
            ClearInputs();
        }

        MessageBox.Show("Xóa thiết bị thành công.", "Thông báo",
            MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void BtnClear_Click(object? sender, EventArgs e)
    {
        ClearInputs();
    }

    private void ClearInputs()
    {
        _isCleared = true;
        _editingOriginalId = null;
        ClearValidationErrors();

        dgvDevices.ClearSelection();
        dgvDevices.CurrentCell = null;

        // Xóa bản nháp khỏi BindingSource phụ, không ảnh hưởng dữ liệu chính.
        _editingDevices.Clear();

        txtDeviceId.Clear();
        txtDeviceName.Clear();
        if (cboCategory.Items.Count > 0)
        {
            cboCategory.SelectedIndex = 0;
        }
        txtPrice.Clear();
        numQuantity.Value = 0;

        UpdateButtonState();
    }

    private void SelectRowAt(int index)
    {
        if (index < 0 || index >= _bindingSource.Count || index >= dgvDevices.Rows.Count)
        {
            ClearInputs();
            return;
        }

        _bindingSource.Position = index;
        dgvDevices.ClearSelection();
        dgvDevices.CurrentCell = dgvDevices.Rows[index].Cells[0];
        dgvDevices.Rows[index].Selected = true;

        LoadDeviceIntoEditor(_devices[index]);
        _isCleared = false;
        ClearValidationErrors();
        UpdateButtonState();
    }

    private void LoadDeviceIntoEditor(Device source)
    {
        _editingDevices.Clear();

        var copy = new Device
        {
            DeviceId = source.DeviceId,
            DeviceName = source.DeviceName,
            Category = source.Category,
            Price = source.Price,
            Quantity = source.Quantity
        };

        _editingDevices.Add(copy);
        _editingBindingSource.Position = 0;
        _editingOriginalId = source.DeviceId;
    }

    private bool ValidateInput()
    {
        bool isValid = true;
        ClearValidationErrors();

        if (string.IsNullOrWhiteSpace(txtDeviceName.Text))
        {
            _errorProvider.SetError(txtDeviceName, "Tên thiết bị không được để trống.");
            isValid = false;
        }

        string category = Convert.ToString(cboCategory.SelectedItem) ?? string.Empty;
        if (string.IsNullOrWhiteSpace(category) || category == CategoryPlaceholder)
        {
            _errorProvider.SetError(cboCategory, "Bạn chưa chọn loại thiết bị.");
            isValid = false;
        }

        if (!TryParsePrice(txtPrice.Text, out decimal price) || price < 0)
        {
            _errorProvider.SetError(txtPrice, "Đơn giá phải là số hợp lệ và không được nhỏ hơn 0.");
            isValid = false;
        }

        if (!isValid)
        {
            MessageBox.Show("Vui lòng kiểm tra các ô có biểu tượng cảnh báo.",
                "Dữ liệu chưa hợp lệ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        return isValid;
    }

    private static bool TryParsePrice(string text, out decimal price)
    {
        const NumberStyles styles = NumberStyles.Number;

        if (decimal.TryParse(text, styles, CultureInfo.CurrentCulture, out price))
        {
            return true;
        }

        if (decimal.TryParse(text, styles, CultureInfo.GetCultureInfo("vi-VN"), out price))
        {
            return true;
        }

        return decimal.TryParse(text, styles, CultureInfo.InvariantCulture, out price);
    }

    private string GenerateDeviceId()
    {
        string id;
        do
        {
            id = $"TB{_nextId:000}";
            _nextId++;
        }
        while (_devices.Any(device => device.DeviceId == id));

        return id;
    }

    private void ClearValidationErrors()
    {
        _errorProvider.SetError(txtDeviceName, string.Empty);
        _errorProvider.SetError(cboCategory, string.Empty);
        _errorProvider.SetError(txtPrice, string.Empty);
    }

    private void UpdateButtonState()
    {
        bool hasSelectedDevice = !_isCleared && _bindingSource.Current is Device &&
                                 _editingBindingSource.Current is Device;
        btnUpdate.Enabled = hasSelectedDevice;
        btnDelete.Enabled = hasSelectedDevice;
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _errorProvider.Dispose();
        _bindingSource.Dispose();
        _editingBindingSource.Dispose();
        base.OnFormClosed(e);
    }
}
