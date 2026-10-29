using System.Globalization;
using System.Text;

namespace quan_ly_muon_tra_sach_thu_vien.Data;

/// <summary>Hỗ trợ tìm kiếm tiếng Việt không phân biệt hoa thường và dấu ("lap trinh" khớp "Lập trình").</summary>
public static class VietnameseText
{
    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var decomposed = value.Trim()
            .Replace('đ', 'd')
            .Replace('Đ', 'D')
            .Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
    }

    /// <summary>Tách từ khóa thành các từ đã chuẩn hóa; một bản ghi khớp khi chứa đủ mọi từ.</summary>
    public static string[] SplitTerms(string? search) =>
        Normalize(search).Split(' ', StringSplitOptions.RemoveEmptyEntries);

    public static bool ContainsAllTerms(string[] terms, params string?[] fields)
    {
        if (terms.Length == 0)
        {
            return true;
        }

        var haystack = Normalize(string.Join(' ', fields.Where(field => !string.IsNullOrWhiteSpace(field))));
        return terms.All(term => haystack.Contains(term, StringComparison.Ordinal));
    }
}
