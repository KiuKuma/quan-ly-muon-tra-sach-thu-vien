using quan_ly_muon_tra_sach_thu_vien.Models;

namespace quan_ly_muon_tra_sach_thu_vien.Models.ViewModels;

public sealed class PagedResult<T>
{
    public const int DefaultPageSize = 10;

    private PagedResult(IReadOnlyList<T> items, int pageNumber, int pageSize, int totalCount)
    {
        Items = items;
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalCount = totalCount;
        TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
    }

    public IReadOnlyList<T> Items { get; }
    public int PageNumber { get; }
    public int PageSize { get; }
    public int TotalCount { get; }
    public int TotalPages { get; }

    public static PagedResult<T> Create(IEnumerable<T> source, int pageNumber, int pageSize = DefaultPageSize)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageSize);

        var records = source.ToList();
        var totalPages = (int)Math.Ceiling(records.Count / (double)pageSize);
        var safePageNumber = Math.Clamp(pageNumber, 1, Math.Max(totalPages, 1));
        var items = records.Skip((safePageNumber - 1) * pageSize).Take(pageSize).ToList();

        return new PagedResult<T>(items, safePageNumber, pageSize, records.Count);
    }

    public PaginationViewModel CreatePagination(
        string? search = null,
        string action = "Index",
        int? routeId = null,
        int? readerId = null,
        string? status = null,
        int? categoryId = null,
        bool? availableOnly = null) =>
        new(PageNumber, TotalPages, search, action, routeId, readerId, status, categoryId, availableOnly);
}

public sealed class PaginationViewModel(
    int pageNumber,
    int totalPages,
    string? search = null,
    string action = "Index",
    int? routeId = null,
    int? readerId = null,
    string? status = null,
    int? categoryId = null,
    bool? availableOnly = null)
{
    public int PageNumber { get; } = pageNumber;
    public int TotalPages { get; } = totalPages;
    public string? Search { get; } = search;
    public string Action { get; } = action;
    public int? RouteId { get; } = routeId;
    public int? ReaderId { get; } = readerId;
    public string? Status { get; } = status;
    public int? CategoryId { get; } = categoryId;
    public bool? AvailableOnly { get; } = availableOnly;

    public IReadOnlyList<int?> VisiblePages
    {
        get
        {
            var pages = new SortedSet<int> { 1, TotalPages };
            for (var page = Math.Max(1, PageNumber - 1); page <= Math.Min(TotalPages, PageNumber + 1); page++)
            {
                pages.Add(page);
            }

            var result = new List<int?>();
            var previous = 0;
            foreach (var page in pages)
            {
                if (previous > 0 && page - previous > 1)
                {
                    result.Add(page - previous == 2 ? previous + 1 : null);
                }

                result.Add(page);
                previous = page;
            }

            return result;
        }
    }
}

public sealed class TheLoaiDetailsViewModel
{
    public required TheLoai Category { get; init; }
    public required PagedResult<Sach> Books { get; init; }
}
