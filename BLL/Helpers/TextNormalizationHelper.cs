using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace BLL.Helpers
{
    public static class TextNormalizationHelper
    {
        public static string NormalizeItemName(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;

            var result = input.Trim().ToLowerInvariant();

            // 1. Bỏ dấu Tiếng Việt
            result = RemoveVietnameseAccents(result);

            // 2. Loại bỏ các định lượng thường gặp (vd: 100ml, 1kg, 500g, 2 l, 3 lít)
            // Regex match: số + (khoảng trắng optional) + (ml|l|kg|g|lit|lít)
            result = Regex.Replace(result, @"\b\d+\s*(ml|l|kg|g|lit)\b", "", RegexOptions.IgnoreCase);

            // 3. Loại bỏ các size (vd: size l, size m)
            result = Regex.Replace(result, @"\bsize\s*(s|m|l|xl|xxl)\b", "", RegexOptions.IgnoreCase);

            // 4. Loại bỏ các ký tự đặc biệt (chỉ giữ lại chữ cái và khoảng trắng)
            // Không giữ lại số vì số trên bill thường là mã lô, mã vạch hoặc trọng lượng còn sót lại
            result = Regex.Replace(result, @"[^a-z\s]", "");

            // 5. Loại bỏ nhiều khoảng trắng thừa
            result = Regex.Replace(result, @"\s+", " ").Trim();

            return result;
        }

        private static string RemoveVietnameseAccents(string text)
        {
            // Các ký tự tiếng Việt có dấu đặc biệt không cover hết bằng String.Normalize
            text = text.Replace('đ', 'd');
            
            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }

        public static int CalculateLevenshteinDistance(string source, string target)
        {
            if (string.IsNullOrEmpty(source)) return target?.Length ?? 0;
            if (string.IsNullOrEmpty(target)) return source.Length;

            int[] costs = new int[target.Length + 1];
            for (int i = 0; i <= source.Length; i++)
            {
                int previousValue = i;
                for (int j = 0; j <= target.Length; j++)
                {
                    if (i == 0)
                    {
                        costs[j] = j;
                    }
                    else if (j > 0)
                    {
                        int currentValue = costs[j - 1];
                        if (source[i - 1] != target[j - 1])
                        {
                            currentValue = Math.Min(Math.Min(currentValue, previousValue), costs[j]) + 1;
                        }
                        costs[j - 1] = previousValue;
                        previousValue = currentValue;
                    }
                }
                if (i > 0) costs[target.Length] = previousValue;
            }
            return costs[target.Length];
        }

        public static double CalculateSimilarity(string source, string target)
        {
            if (source == target) return 1.0;
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(target)) return 0.0;

            int distance = CalculateLevenshteinDistance(source, target);
            int maxLength = Math.Max(source.Length, target.Length);
            
            return 1.0 - ((double)distance / maxLength);
        }
    }
}
