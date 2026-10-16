using System.ComponentModel;

namespace SupermarketMDIApp;

// Danh sach dung chung giua cac Form con trong ung dung MDI.
public static class InventoryData
{
    public static BindingList<Product> Products { get; } = new();

    static InventoryData()
    {
        // Du lieu mau de co the mo bao cao va thu nghiem ngay khi chay.
        Products.Add(new Product
        {
            ProductId = "SP001",
            ProductName = "Laptop van phong",
            Category = "Dien tu",
            Price = 14500000,
            Quantity = 8
        });

        Products.Add(new Product
        {
            ProductId = "SP002",
            ProductName = "Chuot khong day",
            Category = "Phu kien",
            Price = 320000,
            Quantity = 25
        });

        Products.Add(new Product
        {
            ProductId = "SP003",
            ProductName = "Bo phat Wi-Fi",
            Category = "Thiet bi mang",
            Price = 890000,
            Quantity = 12
        });
    }

    public static string GenerateNextId()
    {
        int maxNumber = 0;

        foreach (Product product in Products)
        {
            string numberPart = product.ProductId.StartsWith("SP", StringComparison.OrdinalIgnoreCase)
                ? product.ProductId[2..]
                : product.ProductId;

            if (int.TryParse(numberPart, out int number) && number > maxNumber)
                maxNumber = number;
        }

        return $"SP{maxNumber + 1:000}";
    }
}
