namespace BookLibrary.Domain;

/// <summary>
/// Represents a book as an immutable value object identified by its title, author, and page count.
/// </summary>
/// <remarks>
/// Two books are considered equal when all three properties match (value-based equality).
/// All invariants are validated at construction time and cannot be violated afterwards
/// because the type is immutable.
/// </remarks>
public sealed record Book
{
    /// <summary>Maximum allowed length for <see cref="Title"/> and <see cref="Author"/>.</summary>
    public const int MaxTextLength = 500;
    /// <summary>Upper bound for <see cref="Pages"/>. Sanity limit against malformed input.</summary>

    public const int MaxPages = 1000_000;

    /// <summary>
    /// Initializes a new <see cref="Book"/> instance.
    /// </summary>
    /// <param name="title">Book title. Must be non-null, non-whitespace, and at most <see cref="MaxTextLength"/> characters.</param>
    /// <param name="author">Book author. Must be non-null, non-whitespace, and at most <see cref="MaxTextLength"/> characters.</param>
    /// <param name="pages">Page count. Must be in the range [1, <see cref="MaxPages"/>].</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="title"/> or <paramref name="author"/> is null, empty, or whitespace, or exceeds the maximum length.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="pages"/> is outside the allowed range.</exception>
    public Book(string title, string author, int pages)
    {
        Title = NormalizeText(title, nameof(title));
        Author = NormalizeText(author, nameof(author));
        Pages = NormalizePages(pages);
    }

    /// <summary>Book title. Never null, never whitespace, trimmed.</summary>
    public string Title { get; }

    /// <summary>Book author. Never null, never whitespace, trimmed.</summary>
    public string Author { get; }
    /// <summary>
    /// Page count. Always a positive integer not exceeding <see cref="MaxPages"/>.
    /// </summary>
    public int Pages { get; }


    /// <summary>
    /// Normalizes the specified text value.
    /// </summary>
    /// <param name="value">The text value to normalize.</param>
    /// <param name="paramName">The name of the parameter.</param>
    /// <returns>The normalized text value.</returns>
    public static string NormalizeText(string value, string paramName)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(value, paramName);

        var trilmmed = value.Trim();

        if (trilmmed.Length == 0)
        {
            throw new ArgumentException("Value should not be empty", paramName);
        }
        if (trilmmed.Length > MaxTextLength)
        {
            throw new ArgumentException($"Value should not be longer than {MaxTextLength}", paramName);
        }

        return trilmmed;
    }

    /// <summary>
    /// Normalizes the specified page count value.
    /// </summary>
    /// <param name="pages"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static int NormalizePages(int pages)
    {
        if (pages < 1 || pages > MaxPages)
        {
            throw new ArgumentOutOfRangeException(
                nameof(pages),
                pages,
                $"Page count must be between 1 and {MaxPages}.");
        }

        return pages;
    }
}
