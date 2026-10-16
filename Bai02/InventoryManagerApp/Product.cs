using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace InventoryManagerApp;

public class Product : INotifyPropertyChanged
{
    private string _productId = string.Empty;
    private string _productName = string.Empty;
    private string _category = string.Empty;
    private decimal _price;
    private int _quantity;
    private string _imagePath = string.Empty;

    public string ProductId
    {
        get => _productId;
        set => SetField(ref _productId, value);
    }

    public string ProductName
    {
        get => _productName;
        set => SetField(ref _productName, value);
    }

    public string Category
    {
        get => _category;
        set => SetField(ref _category, value);
    }

    public decimal Price
    {
        get => _price;
        set => SetField(ref _price, value);
    }

    public int Quantity
    {
        get => _quantity;
        set => SetField(ref _quantity, value);
    }

    // Luu duong dan anh, khong luu truc tiep noi dung anh vao DataGridView.
    public string ImagePath
    {
        get => _imagePath;
        set => SetField(ref _imagePath, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return;

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public Product Copy()
    {
        return new Product
        {
            ProductId = ProductId,
            ProductName = ProductName,
            Category = Category,
            Price = Price,
            Quantity = Quantity,
            ImagePath = ImagePath
        };
    }
}
