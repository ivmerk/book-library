public interface IBookRepository
{
    IReadOnlyList<Book> GetAll();
    void Add(Book book);
    IReadOnlyList<Book> SortedByAuthorThenTitle();
    IReadOnlyList<Book> SearchByTitle(string titlePart);
    Task LoadFromAsync(string path, CancellationToken ct = default);
    Task SaveToAsync(string path, CancellationToken ct = default);
}
