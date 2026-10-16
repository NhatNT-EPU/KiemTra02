using System.ComponentModel;

namespace DeviceManagerApp;

// Lớp mô tả thông tin của một thiết bị điện tử.
public class Device : INotifyPropertyChanged
{
    private string _deviceId = string.Empty;
    private string _deviceName = string.Empty;
    private string _category = string.Empty;
    private decimal _price;
    private int _quantity;

    public string DeviceId
    {
        get => _deviceId;
        set
        {
            if (_deviceId != value)
            {
                _deviceId = value;
                OnPropertyChanged(nameof(DeviceId));
            }
        }
    }

    public string DeviceName
    {
        get => _deviceName;
        set
        {
            if (_deviceName != value)
            {
                _deviceName = value;
                OnPropertyChanged(nameof(DeviceName));
            }
        }
    }

    public string Category
    {
        get => _category;
        set
        {
            if (_category != value)
            {
                _category = value;
                OnPropertyChanged(nameof(Category));
            }
        }
    }

    public decimal Price
    {
        get => _price;
        set
        {
            if (_price != value)
            {
                _price = value;
                OnPropertyChanged(nameof(Price));
            }
        }
    }

    public int Quantity
    {
        get => _quantity;
        set
        {
            if (_quantity != value)
            {
                _quantity = value;
                OnPropertyChanged(nameof(Quantity));
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
