using System.Collections.Generic;
using System.Linq;

namespace McpPlugin.Utils
{
    /// <summary>
    /// Pagination helper for list results.
    /// </summary>
    public class PagedResult<T>
    {
        public List<T> Items { get; set; }
        public int Total { get; set; }
        public int Offset { get; set; }
        public int Count { get; set; }
        public bool HasMore { get; set; }
        public int? NextOffset { get; set; }
    }

    /// <summary>
    /// Pagination utilities.
    /// </summary>
    public static class Pagination
    {
        /// <summary>
        /// Creates a paged result from an enumerable.
        /// </summary>
        public static PagedResult<T> Paginate<T>(IEnumerable<T> source, int offset = 0, int count = 50)
        {
            if (offset < 0) offset = 0;
            if (count <= 0) count = 50;
            if (count > 1000) count = 1000;

            var list = source as IList<T> ?? source.ToList();
            var total = list.Count;
            var items = list.Skip(offset).Take(count).ToList();
            var hasMore = offset + items.Count < total;

            return new PagedResult<T>
            {
                Items = items,
                Total = total,
                Offset = offset,
                Count = items.Count,
                HasMore = hasMore,
                NextOffset = hasMore ? offset + count : (int?)null
            };
        }

        /// <summary>
        /// Applies a filter pattern to items.
        /// </summary>
        public static IEnumerable<T> Filter<T>(IEnumerable<T> source, string pattern, System.Func<T, string> selector)
        {
            if (string.IsNullOrWhiteSpace(pattern) || pattern == "*")
                return source;

            pattern = pattern.Trim();
            var isWildcard = pattern.Contains("*") || pattern.Contains("?");

            if (isWildcard)
            {
                // Convert glob pattern to regex
                var regex = "^" + System.Text.RegularExpressions.Regex.Escape(pattern)
                    .Replace("\\*", ".*")
                    .Replace("\\?", ".") + "$";

                var re = new System.Text.RegularExpressions.Regex(regex,
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                return source.Where(item => re.IsMatch(selector(item) ?? ""));
            }
            else
            {
                // Substring match
                return source.Where(item =>
                    (selector(item) ?? "").IndexOf(pattern,
                        System.StringComparison.OrdinalIgnoreCase) >= 0);
            }
        }
    }
}
