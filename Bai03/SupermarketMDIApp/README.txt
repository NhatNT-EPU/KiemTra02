BAI 3 - HE THONG MDI QUAN LY SIEU THI & DONG BO BAT DONG BO

1. Yeu cau
- Windows 10/11
- Visual Studio 2022
- Workload: .NET desktop development
- .NET 8 SDK

2. Mo va chay
- Giai nen SupermarketMDIApp.zip.
- Mo Visual Studio.
- Chon File > Open > Project/Solution.
- Mo file SupermarketMDIApp.csproj.
- Nhan F5 de chay.

3. Cac file chinh
- Program.cs: diem khoi chay chuong trinh.
- frmMain.cs: Form cha MDI, menu dieu huong, StatusStrip va ProgressBar dung chung.
- frmProductCatalog.cs: Form con quan ly san pham, CRUD, chon anh, Import/Export CSV.
- frmAsyncReport.cs: bao cao bat dong bo, Task.Run, async/await va dong bo nen.
- Product.cs: lop du lieu san pham.
- InventoryData.cs: danh sach san pham dung chung giua cac Form.
- CsvUtility.cs: ho tro doc/ghi CSV.
- SampleProducts.csv: du lieu CSV mau.

4. Cach thu nghiem
- Vao Danh muc > Quan ly San pham de them, sua, xoa san pham hoac Import/Export CSV.
- Vao Bao cao > Thong ke Doanh thu.
- Nhan Tải Báo Cáo Doanh Thu. Trong khoang 4 giay, ban van co the di chuyen Form, su dung menu va mo Form quan ly san pham.
- Nhat ky dong bo tu dong them dong moi moi 2 giay. Code dung InvokeRequired va this.Invoke de chuyen cap nhat ve UI Thread.
- Thu menu Cua so > Cascade / Tile Horizontal / Tile Vertical.

5. Luu y
- Doanh thu trong bai nay la so lieu minh hoa: so luong ban gia lap bang 1/3 so luong ton, sau do nhan voi don gia. Day khong phai doanh thu thuc te tu he thong ban hang.
- Du lieu san pham luu trong bo nho, khong ket noi CSDL. Hay Export CSV neu muon luu danh sach ra file.
- Chuc nang Dang xuat chi mo phong doi trang thai nguoi dung, chua co he thong xac thuc tai khoan.
- Duong dan anh duoc luu trong truong ImagePath. Neu di chuyen file anh, anh co the khong hien thi nua.
