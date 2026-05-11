using BookLibrary.Domain;
using BookLibrary.Exceptions;

namespace BookLibrary.Abstractions;

/// <summary>
/// Persists and retrieves collections of <see cref="Book"/> objects from an external medium.
/// </summary>
/// <remarks>
/// This abstraction decouples the repository from a specific storage format (XML, JSON, database, etc.).
/// Implementations are responsible for serialization, deserialization, and translating
/// low-level I/O errors into <see cref="BookLibraryException"/>.
/// <para>
/// Implementations are NOT required to be thread-safe. Concurrent reads/writes to the same path
/// must be synchronized externally.
/// </para>
/// </remarks>
public interface IBookStorage
{
  /// <summary>
  /// Reads all books from <paramref name="path"/>.
  /// </summary>
  /// <param name="path">Path to the source file in the format expected by this implementation.</param>
  /// <param name="ct">Token to cancel the I/O operation.</param>
  /// <returns>
  /// A task producing the deserialized books in the order they appear in the source.
  /// An empty file (or a file containing an empty collection) yields an empty list, never <c>null</c>.
  /// </returns>
  /// <exception cref="ArgumentException">Thrown when <paramref name="path"/> is null, empty, or whitespace.</exception>
  /// <exception cref="BookLibraryException">
  /// Thrown when the file does not exist, cannot be read, or contains malformed/invalid content.
  /// The original error is preserved as <see cref="Exception.InnerException"/>.
  /// </exception>
  /// <exception cref="OperationCanceledException">Thrown when <paramref name="ct"/> is cancelled.</exception>
  Task<IReadOnlyList<Book>> LoadAsync(string path, CancellationToken ct = default);

  /// <summary>
  /// Writes <paramref name="books"/> to <paramref name="path"/>, overwriting the file if it already exists.
  /// </summary>
  /// <param name="path">Target file path. The parent directory must exist.</param>
  /// <param name="books">
  /// Books to persist. The sequence is enumerated exactly once.
  /// An empty sequence produces a valid, empty document (not an empty file).
  /// </param>
  /// <param name="ct">Token to cancel the I/O operation.</param>
  /// <returns>A task that completes when the file has been written.</returns>
  /// <exception cref="ArgumentException">Thrown when <paramref name="path"/> is null, empty, or whitespace.</exception>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="books"/> is <c>null</c>.</exception>
  /// <exception cref="BookLibraryException">
  /// Thrown when the file cannot be written (I/O error, permission denied, etc.).
  /// The original error is preserved as <see cref="Exception.InnerException"/>.
  /// </exception>
  /// <exception cref="OperationCanceledException">Thrown when <paramref name="ct"/> is cancelled.</exception>
  /// <remarks>
  /// The write is NOT atomic. If the process is interrupted mid-write, the target file
  /// may be left partially written. Implementations that need atomic writes should write to
  /// a temporary file and rename on success.
  /// </remarks>
  Task SaveAsync(string path, IEnumerable<Book> books, CancellationToken ct = default);
}