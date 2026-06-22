namespace CommerceSphere.Shared.Common.Models;

public class PagedResult<T>
{
    public IEnumerable<T> Items { get; init; } = [];
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalRecords { get; init; }
    public int TotalPages => (int)Math.Ceiling((double)TotalRecords / PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;

    public static PagedResult<T> Create(IEnumerable<T> items, int totalRecords, int pageNumber, int pageSize) =>
        new() { Items = items, TotalRecords = totalRecords, PageNumber = pageNumber, PageSize = pageSize };
}

public class PagedRequest
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;

    public int Skip => (PageNumber - 1) * PageSize;
}
