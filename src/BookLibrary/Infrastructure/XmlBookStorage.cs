using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using BookLibrary.Abstractions;
using BookLibrary.Domain;
using BookLibrary.Exceptions;

namespace BookLibrary.Infrastructure;

/// <summary>
/// XML-backed implementation of <see cref="IBookStorage"/>.
/// </summary>
/// <remarks>
/// File format:
/// <code>
/// &lt;library&gt;
///   &lt;book&gt;
///     &lt;title&gt;The Little Mermaid&lt;/title&gt;
///     &lt;author&gt;Andersen&lt;/author&gt;
///     &lt;pages&gt;32&lt;/pages&gt;
///   &lt;/book&gt;
/// &lt;/library&gt;
/// </code>
/// Files are written as UTF-8 without BOM, with two-space indentation.
/// Page counts are serialized using the invariant culture to avoid locale-specific issues.
/// </remarks>
public sealed class XmlBookStorage : IBookStorage
{
    private const string RootElement = "library";
    private const string BookElement = "book";
    private const string TitleElement = "title";
    private const string AuthorElement = "author";
    private const string PagesElement = "pages";

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Book>> LoadAsync(string path, CancellationToken ct = default)
    {
        ValidatePath(path);

        XDocument document;
        try
        {
            await using var stream = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 4096,
                useAsync: true);

            document = await XDocument.LoadAsync(stream, LoadOptions.None, ct).ConfigureAwait(false);
        }
        catch (FileNotFoundException ex)
        {
            throw new BookLibraryException($"Book file not found: '{path}'.", ex);
        }
        catch (DirectoryNotFoundException ex)
        {
            throw new BookLibraryException($"Directory for book file not found: '{path}'.", ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new BookLibraryException($"Access denied when reading book file: '{path}'.", ex);
        }
        catch (IOException ex)
        {
            throw new BookLibraryException($"I/O error reading book file: '{path}'.", ex);
        }
        catch (XmlException ex)
        {
            throw new BookLibraryException($"Book file is not valid XML: '{path}'.", ex);
        }

        return ParseBooks(document, path);
    }

    /// <inheritdoc/>
    public async Task SaveAsync(string path, IEnumerable<Book> books, CancellationToken ct = default)
    {
        ValidatePath(path);
        ArgumentNullException.ThrowIfNull(books);

        // Materialize once: protects against multi-enumeration of yield-style sequences
        // and gives the XDocument constructor a stable view.
        var snapshot = books.ToList();
        var document = BuildDocument(snapshot);

        try
        {
            await using var stream = new FileStream(
                path,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 4096,
                useAsync: true);

            var settings = new XmlWriterSettings
            {
                Async = true,
                Indent = true,
                IndentChars = "  ",
                Encoding = new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            };

            await using var writer = XmlWriter.Create(stream, settings);
            await document.SaveAsync(writer, ct).ConfigureAwait(false);
            await writer.FlushAsync().ConfigureAwait(false);
        }
        catch (DirectoryNotFoundException ex)
        {
            throw new BookLibraryException($"Directory for book file not found: '{path}'.", ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new BookLibraryException($"Access denied when writing book file: '{path}'.", ex);
        }
        catch (IOException ex)
        {
            throw new BookLibraryException($"I/O error writing book file: '{path}'.", ex);
        }
    }

    // ─── Parsing ──────────────────────────────────────────────────────────

    private static IReadOnlyList<Book> ParseBooks(XDocument document, string path)
    {
        var root = document.Root;
        if (root is null || root.Name.LocalName != RootElement)
        {
            throw new BookLibraryException(
                $"Book file '{path}' is missing the <{RootElement}> root element.");
        }

        var books = new List<Book>();
        foreach (var bookElement in root.Elements(BookElement))
        {
            books.Add(ParseBook(bookElement, path));
        }

        return books;
    }

    private static Book ParseBook(XElement element, string path)
    {
        var title = ReadRequiredString(element, TitleElement, path);
        var author = ReadRequiredString(element, AuthorElement, path);
        var pages = ReadRequiredInt(element, PagesElement, path);

        try
        {
            return new Book(title, author, pages);
        }
        catch (Exception ex) when (ex is ArgumentException or ArgumentOutOfRangeException)
        {
            throw new BookLibraryException(
                $"Book file '{path}' contains an invalid <{BookElement}>: {ex.Message}",
                ex);
        }
    }

    private static string ReadRequiredString(XElement parent, string name, string path)
    {
        var child = parent.Element(name);
        if (child is null)
        {
            throw new BookLibraryException(
                $"Book file '{path}' has a <{BookElement}> missing required <{name}>.");
        }

        return child.Value;
    }

    private static int ReadRequiredInt(XElement parent, string name, string path)
    {
        var raw = ReadRequiredString(parent, name, path);
        if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
        {
            throw new BookLibraryException(
                $"Book file '{path}' has <{name}> with non-integer value '{raw}'.");
        }

        return value;
    }

    // ─── Building ─────────────────────────────────────────────────────────

    private static XDocument BuildDocument(IEnumerable<Book> books)
    {
        return new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement(
                RootElement,
                books.Select(book => new XElement(
                    BookElement,
                    new XElement(TitleElement, book.Title),
                    new XElement(AuthorElement, book.Author),
                    new XElement(PagesElement, book.Pages.ToString(CultureInfo.InvariantCulture))))));
    }

    // ─── Validation ───────────────────────────────────────────────────────

    private static void ValidatePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path must be a non-empty, non-whitespace string.", nameof(path));
        }
    }
}