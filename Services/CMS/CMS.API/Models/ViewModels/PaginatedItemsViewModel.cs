using System.Collections.Generic;

namespace CMS.API.Models.ViewModels;

public class PaginatedItemsViewModel<T> where T : class
{
    public PaginatedItemsViewModel(int pageIndex, int pageSize, long count, IEnumerable<T> data)
    {
        PageIndex = pageIndex;
        PageSize = pageSize;
        Count = count;
        Data = data;
    }

    public int PageIndex { get; private set; }
    public int PageSize { get; private set; }
    public long Count { get; private set; }
    public IEnumerable<T> Data { get; private set; }
}