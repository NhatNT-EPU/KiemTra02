BAI 1 - QUAN LY DANH MUC THIET BI DIEN TU

Cau truc thu muc:
- DeviceManagerApp.csproj : project Windows Forms .NET 8
- Program.cs             : diem khoi chay chuong trinh
- Device.cs              : lop du lieu Device, co INotifyPropertyChanged
- frmDeviceManager.cs    : giao dien va xu ly them/sua/xoa/kiem tra du lieu

CACH CHAY
1. May Windows can cai Visual Studio 2022 voi workload ".NET desktop development" va .NET 8 SDK.
2. Giai nen thu muc DeviceManagerApp.
3. Mo file DeviceManagerApp.csproj bang Visual Studio.
4. Nhan F5 hoac Ctrl+F5 de chay.

Co the mo Terminal tai thu muc project va chay:
  dotnet run

LUU Y
- Du lieu hien dang luu trong BindingList<Device> o bo nho; khi dong ung dung, du lieu se mat.
- Cac cot DataGridView duoc tao bang code, AutoGenerateColumns = false.
- BindingSource chinh lien ket BindingList<Device> voi DataGridView; BindingSource phu quan ly ban nhap de binding 2 chieu va cap nhat co kiem soat.
- Nut Cap nhat ghi cac gia tri tren Form xuong doi tuong dang chon.
