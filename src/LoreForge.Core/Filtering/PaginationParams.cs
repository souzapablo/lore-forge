namespace LoreForge.Core.Filtering;

public record PaginationParams(int Page = 1, int PageSize = 10)
{
    public int Skip => (Page - 1) * PageSize;
}
