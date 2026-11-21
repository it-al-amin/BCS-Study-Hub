namespace BCSStudyHub.Application.Common;

public sealed record PageRequest(int PageNumber = 1, int PageSize = 20)
{
    public const int MaximumPageSize = 100;

    public int EffectivePageNumber => Math.Max(PageNumber, 1);
    public int EffectivePageSize => Math.Clamp(PageSize, 1, MaximumPageSize);
    public int Offset => (int)Math.Min(
        ((long)EffectivePageNumber - 1) * EffectivePageSize,
        int.MaxValue);
}

public sealed record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int PageNumber,
    int PageSize,
    long TotalCount)
{
    public long TotalPages
    {
        get
        {
            var safePageSize = Math.Max(PageSize, 1);
            return TotalCount / safePageSize + (TotalCount % safePageSize == 0 ? 0 : 1);
        }
    }
}
