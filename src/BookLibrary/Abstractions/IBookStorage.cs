public interface IBookStorage
{
    Task<IReadOnlyList<Book>> LoadAsync(string path, CancellationToken ct = default);
    Task SaveAsync(string path, IEnumerable<Book> books, CancellationToken ct = default);
}


