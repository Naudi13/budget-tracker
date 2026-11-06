namespace BudTack.Transactions.Internal.Domain;

/// <summary>A valid <c>?page=&amp;limit=</c> (07-api/rest-conventions.md): page from 1, limit 1 to 100, 20 by default.</summary>
internal readonly record struct Paging(int Page, int Limit)
{
    public const int DefaultLimit = 20;
    public const int MaxLimit = 100;

    public int Offset => (Page - 1) * Limit;
}
