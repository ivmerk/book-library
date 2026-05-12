using BookLibrary.Domain;
using BookLibrary.Exceptions;

namespace BookLibrary.Abstractions;

/// <summary>
/// In-memory repository of books with persistence delegated to an <see cref="IBookStorage"/>.
/// </summary>
/// <remarks>
/// Read methods (<see cref="GetAll"/>, <see cref="SortedByAuthorThenTitle"/>, <see cref="SearchByTitle"/>)
/// return snapshots — subsequent modifications to the repository do not affect already returned collections.
/// <para>
/// Implementations are NOT required to be thread-safe. Concurrent access must be synchronized externally.
/// </para>
/// </remarks>
public interface IBookRepository
{
    /// <summary>
    /// Returns all books in insertion order.
    /// </summary>
    /// <returns>A snapshot of the current contents. Empty list if no books have been added or loaded.</returns>
    IReadOnlyList<Book> GetAll();

    /// <summary>
    /// Appends a book to the repository.
    /// </summary>
    /// <param name="book">The book to add. Duplicates (by value equality) are allowed.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="book"/> is <c>null</c>.</exception>
    void Add(Book book);

    /// <summary>
    /// Returns all books sorted alphabetically by author, then by title within each author.
    /// </summary>
    /// <returns>
    /// A snapshot sorted using <see cref="StringComparer.InvariantCultureIgnoreCase"/>.
    /// Empty list if the repository is empty.
    /// </returns>
    IReadOnlyList<Book> SortedByAuthorThenTitle();

    /// <summary>
    /// Finds books whose title contains the given substring (case-insensitive, ordinal comparison).
    /// </summary>
    /// <param name="titlePart">
    /// Substring to match against <see cref="Book.Title"/>.
    /// An empty or whitespace string returns an empty list — use <see cref="GetAll"/> to retrieve everything.
    /// </param>
    /// <returns>A snapshot of matching books in insertion order. Empty list if nothing matches.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="titlePart"/> is <c>null</c>.</exception>
    IReadOnlyList<Book> SearchByTitle(string titlePart);

    /// <summary>
    /// Replaces the current contents of the repository with books loaded from <paramref name="path"/>.
    /// </summary>
    /// <param name="path">Path to the source file in the format expected by the underlying <see cref="IBookStorage"/>.</param>
    /// <param name="ct">Token to cancel the I/O operation.</param>
    /// <returns>A task that completes once the repository has been fully repopulated.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="path"/> is null, empty, or whitespace.</exception>
    /// <exception cref="BookLibraryException">
    /// Thrown when the file cannot be read or its contents are invalid. The original error is preserved as <see cref="Exception.InnerException"/>.
    /// </exception>
    /// <exception cref="OperationCanceledException">Thrown when <paramref name="ct"/> is cancelled.</exception>
    /// <remarks>
    /// On failure the repository state is left unchanged — loading is all-or-nothing.
    /// </remarks>
    Task LoadFromAsync(string path, CancellationToken ct = default);

    /// <summary>
    /// Writes the current snapshot of books to <paramref name="path"/>, overwriting it if it already exists.
    /// </summary>
    /// <param name="path">Target file path. The parent directory must exist.</param>
    /// <param name="ct">Token to cancel the I/O operation.</param>
    /// <returns>A task that completes when the file has been written.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="path"/> is null, empty, or whitespace.</exception>
    /// <exception cref="BookLibraryException">
    /// Thrown when the file cannot be written (I/O error, permission denied, etc.). The original error is preserved as <see cref="Exception.InnerException"/>.
    /// </exception>
    /// <exception cref="OperationCanceledException">Thrown when <paramref name="ct"/> is cancelled.</exception>
    /// <remarks>
    /// The write is NOT atomic. If the process is interrupted mid-write, the target file may be left partially written.
    /// </remarks>
    Task SaveToAsync(string path, CancellationToken ct = default);
}