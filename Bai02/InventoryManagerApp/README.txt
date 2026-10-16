BAI 2 - QUAN LY KHO VA XUAT BAO CAO TONG HOP

1. Yeu cau
- Windows 10/11
- Visual Studio 2022 va workload .NET desktop development
- .NET 8 SDK / Windows Forms

2. Cach chay
1) Giai nen InventoryManagerApp.zip.
2) Mo Visual Studio.
3) Chon File > Open > Project/Solution.
4) Mo file InventoryManagerApp.csproj.
5) Nhan F5 de chay.

Giao dien duoc tao bang code, khong can keo tha control trong Designer.

3. Cac file
- Program.cs: diem khoi chay chuong trinh.
- Product.cs: lop du lieu san pham, co INotifyPropertyChanged.
- frmInventoryManager.cs: giao dien, CRUD, chon anh, Import/Export CSV, JSON, TXT va StatusStrip.
- InventoryManagerApp.csproj: cau hinh Windows Forms .NET 8.
- SampleInventory.csv: du lieu mau de thu Import CSV.

4. Cach su dung
- Dien ten san pham, chon loai, nhap don gia va so luong.
- Bam Chon anh... de chon file JPG/JPEG/PNG/BMP.
- Bam Them moi de them san pham.
- Chon mot dong tren bang de sua; bam Cap nhat de luu thay doi.
- Bam Xoa va chon Yes de xoa san pham.
- Tools > Import CSV de nhap CSV; System > Open File ho tro CSV, TXT dang bang va JSON.
- Tools > Export CSV de xuat CSV.
- System > Save Report cho phep luu CSV, JSON hoac TXT dang bang (tab-separated).
- Khi dong form hoac chon Exit, chuong trinh hoi xac nhan Yes/No.

5. Cau truc file du lieu
CSV co cac cot theo thu tu:
ProductId,ProductName,Category,Price,Quantity,ImagePath

Gia duoc ghi theo so thap phan invariant (vi du 1250000.5); so luong la so nguyen.
Duong dan anh duoc luu dang chuoi, DataGridView chi hien ten file anh de tranh tai anh vao tung dong.
Du lieu dang duoc luu trong bo nho; hay xuat bao cao neu muon giu lai sau khi dong ung dung.
