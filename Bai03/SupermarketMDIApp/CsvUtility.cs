using System.Globalization;
using System.Text;

namespace SupermarketMDIApp;

// Xu ly CSV don gian, co ho tro dau phay va dau nhay kep trong du lieu.
public static class CsvUtility
{
    public static string ToCsvLine(IEnumerable<string> values)
    {
        return string.Join(",", values.Select(Escape));
    }

    public static string Escape(string value)
    {
        value ??= string.Empty;
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        return value;
    }

    public static List<string> ParseLine(string line)
    {
        var values = new List<string>();
        var current = new StringBuilder();
        bool insideQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char ch = line[i];

            if (ch == '"')
            {
                if (insideQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    insideQuotes = !insideQuotes;
                }
            }
            else if (ch == ',' && !insideQuotes)
            {
                values.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(ch);
            }
        }

        values.Add(current.ToString());
        return values;
    }

    public static bool TryCreateProduct(string line, out Product? product)
    {
        product = null;
        List<string> fields = ParseLine(line);
        if (fields.Count < 6)
            return false;

        if (string.IsNullOrWhiteSpace(fields[0]) || string.IsNullOrWhiteSpace(fields[1]))
            return false;

        if (!decimal.TryParse(fields[3], NumberStyles.Number, CultureInfo.InvariantCulture, out decimal price) || price < 0)
            return false;

        if (!int.TryParse(fields[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out int quantity) || quantity < 0)
            return false;

        product = new Product
        {
            ProductId = fields[0].Trim(),
            ProductName = fields[1].Trim(),
            Category = fields[2].Trim(),
            Price = price,
            Quantity = quantity,
            ImagePath = fields[5].Trim()
        };
        return true;
    }
}
