using System.Drawing;
using System.Windows.Forms;

namespace SupermarketMDIApp;

public class frmMain : Form
{
    private ToolStripStatusLabel lblUser = null!;
    private ToolStripStatusLabel lblState = null!;
    private ToolStripProgressBar progressBar = null!;
    private bool _allowClose;
    private string _currentUser = "admin";

    public frmMain()
    {
        Text = "Hệ thống quản lý siêu thị - MDI";
        IsMdiContainer = true;
        StartPosition = FormStartPosition.CenterScreen;
        WindowState = FormWindowState.Maximized;
        MinimumSize = new Size(1000, 650);
        Font = new Font("Segoe UI", 9F);

        BuildMenu();
        BuildStatusStrip();

        FormClosing += MainForm_FormClosing;
    }

    private void BuildMenu()
    {
        var menu = new MenuStrip { Dock = DockStyle.Top };

        var systemMenu = new ToolStripMenuItem("Hệ thống");
        systemMenu.DropDownItems.Add("Đăng xuất", SystemIcons.Information.ToBitmap(), (_, _) => Logout());
        systemMenu.DropDownItems.Add(new ToolStripSeparator());
        systemMenu.DropDownItems.Add("Thoát", SystemIcons.Error.ToBitmap(), (_, _) => Close());

        var catalogMenu = new ToolStripMenuItem("Danh mục");
        catalogMenu.DropDownItems.Add("Quản lý Sản phẩm", SystemIcons.Application.ToBitmap(), (_, _) => OpenChild<frmProductCatalog>());
        catalogMenu.DropDownItems.Add("Quản lý Nhập xuất", SystemIcons.Information.ToBitmap(), (_, _) => OpenChild<frmProductCatalog>());

        var reportMenu = new ToolStripMenuItem("Báo cáo");
        reportMenu.DropDownItems.Add("Thống kê Doanh thu", SystemIcons.Asterisk.ToBitmap(), (_, _) => OpenChild<frmAsyncReport>());

        var windowMenu = new ToolStripMenuItem("Cửa sổ");
        windowMenu.DropDownItems.Add("Cascade", null, (_, _) => LayoutMdi(MdiLayout.Cascade));
        windowMenu.DropDownItems.Add("Tile Horizontal", null, (_, _) => LayoutMdi(MdiLayout.TileHorizontal));
        windowMenu.DropDownItems.Add("Tile Vertical", null, (_, _) => LayoutMdi(MdiLayout.TileVertical));

        menu.Items.Add(systemMenu);
        menu.Items.Add(catalogMenu);
        menu.Items.Add(reportMenu);
        menu.Items.Add(windowMenu);

        Controls.Add(menu);
        MainMenuStrip = menu;
    }

    private void BuildStatusStrip()
    {
        var status = new StatusStrip { Dock = DockStyle.Bottom, SizingGrip = false };
        lblUser = new ToolStripStatusLabel($"Người dùng: {_currentUser}");
        lblState = new ToolStripStatusLabel("Sẵn sàng") { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
        progressBar = new ToolStripProgressBar
        {
            Visible = false,
            Style = ProgressBarStyle.Marquee,
            MarqueeAnimationSpeed = 25,
            Width = 180
        };

        status.Items.Add(lblUser);
        status.Items.Add(new ToolStripStatusLabel("|") { ForeColor = SystemColors.GrayText });
        status.Items.Add(lblState);
        status.Items.Add(progressBar);
        Controls.Add(status);
    }

    // Neu Form con da mo thi Activate(), neu chua thi tao instance moi.
    private void OpenChild<T>() where T : Form, new()
    {
        foreach (Form child in MdiChildren)
        {
            if (child is T && !child.IsDisposed)
            {
                if (child.WindowState == FormWindowState.Minimized)
                    child.WindowState = FormWindowState.Normal;
                child.Activate();
                return;
            }
        }

        var form = new T
        {
            MdiParent = this,
            WindowState = FormWindowState.Maximized
        };
        form.Show();
    }

    // Duoc Form con bao de bat/tat ProgressBar khi dang tai bao cao.
    public void SetBusy(bool isBusy, string message = "Đang xử lý dữ liệu...")
    {
        if (IsDisposed || Disposing)
            return;

        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => SetBusy(isBusy, message)));
            return;
        }

        progressBar.Visible = isBusy;
        progressBar.Style = ProgressBarStyle.Marquee;
        lblState.Text = isBusy ? message : "Sẵn sàng";
    }

    private void Logout()
    {
        DialogResult result = MessageBox.Show(
            "Bạn có chắc chắn muốn đăng xuất khỏi hệ thống?",
            "Xác nhận đăng xuất",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result != DialogResult.Yes)
            return;

        foreach (Form child in MdiChildren.ToArray())
            child.Close();

        // Bai tap mo phong ten nguoi dung; chua ket noi he thong tai khoan that.
        SetBusy(false);
        _currentUser = "khách";
        lblUser.Text = $"Người dùng: {_currentUser}";
        MessageBox.Show("Đã đăng xuất (mô phỏng).", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_allowClose)
            return;

        DialogResult result = MessageBox.Show(
            "Bạn có chắc chắn muốn thoát chương trình?",
            "Xác nhận thoát",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result == DialogResult.Yes)
        {
            _allowClose = true;
        }
        else
        {
            e.Cancel = true;
        }
    }
}
