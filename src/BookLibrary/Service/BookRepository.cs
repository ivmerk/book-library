using BookLibrary.Abstractions;
using BookLibrary.Domain;

namespace BookLibrary.Service;

/// <summary>
/// In-memory repository of books with persistence delegated to an <see cref="IBookStorage"/>.
/// </summary>
public sealed class BookRepository : IBookRepository
{
  private static readonly StringComparer SortComparer = StringComparer.InvariantCultureIgnoreCase;
  private static readonly StringComparison SearchComparison = StringComparison.OrdinalIgnoreCase;

  private readonly IBookStorage _storage;
  private readonly List<Book> _books = new();

  /// <summary>
  /// Initializes a new repository with the given storage backend.
  /// </summary>
  /// <param name="storage">Storage used by <see cref="LoadFromAsync"/> and <see cref="SaveToAsync"/>.</param>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="storage"/> is <c>null</c>.</exception>
  public BookRepository(IBookStorage storage)
  {
    ArgumentNullException.ThrowIfNull(storage);
    _storage = storage;
  }

  /// <inheritdoc/>
  public IReadOnlyList<Book> GetAll() => _books.ToList();

  /// <inheritdoc/>
  public void Add(Book book)
  {
    ArgumentNullException.ThrowIfNull(book);
    _books.Add(book);
  }

  /// <inheritdoc/>
  public IReadOnlyList<Book> SortedByAuthorThenTitle() =>
      _books
          .OrderBy(b => b.Author, SortComparer)
          .ThenBy(b => b.Title, SortComparer)
          .ToList();

  /// <inheritdoc/>
  public IReadOnlyList<Book> SearchByTitle(string titlePart)
  {
    ArgumentNullException.ThrowIfNull(titlePart);

    if (string.IsNullOrWhiteSpace(titlePart))
    {
      return Array.Empty<Book>();
    }

    return _books
        .Where(b => b.Title.Contains(titlePart, SearchComparison))
        .ToList();
  }

  /// <inheritdoc/>
  public async Task LoadFromAsync(string path, CancellationToken ct = default)
  {
    ValidatePath(path);
    var loaded = await _storage.LoadAsync(path, ct).ConfigureAwait(false);
    _books.Clear();
    _books.AddRange(loaded);
  }

  /// <inheritdoc/>
  public Task SaveToAsync(string path, CancellationToken ct = default)
  {
    ValidatePath(path);
    var snapshot = _books.ToList();
    return _storage.SaveAsync(path, snapshot, ct);
  }

  private static void ValidatePath(string path)
  {
    if (string.IsNullOrWhiteSpace(path))
    {
      throw new ArgumentException("Path must be a non-empty, non-whitespace string.", nameof(path));
    }
  }
}