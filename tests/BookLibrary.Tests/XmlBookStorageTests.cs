using BookLibrary.Domain;
using BookLibrary.Exceptions;
using BookLibrary.Infrastructure;
using FluentAssertions;

namespace BookLibrary.Tests;

public class XmlBookStorageTests : IDisposable
{
    private readonly string _tempFile;
    private readonly XmlBookStorage _storage = new();

    public XmlBookStorageTests()
    {
        _tempFile = Path.Combine(Path.GetTempPath(), $"booklibrary_test_{Guid.NewGuid():N}.xml");
    }

    public void Dispose()
    {
        if (File.Exists(_tempFile))
        {
            File.Delete(_tempFile);
        }
    }

    // ─── Round-trip ───────────────────────────────────────────────────────

    [Fact]
    public async Task RoundTrip_PreservesAllBooks()
    {
        var original = new[]
        {
            new Book("The Little Mermaid", "Andersen", 32),
            new Book("It", "King", 1138),
            new Book("Война и мир", "Толстой", 1225),
        };

        await _storage.SaveAsync(_tempFile, original);
        var loaded = await _storage.LoadAsync(_tempFile);

        loaded.Should().Equal(original);
    }

    [Fact]
    public async Task RoundTrip_EmptyCollection_ProducesValidEmptyDocument()
    {
        await _storage.SaveAsync(_tempFile, Array.Empty<Book>());
        var loaded = await _storage.LoadAsync(_tempFile);

        loaded.Should().BeEmpty();
    }

    // ─── Save ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task SaveAsync_OverwritesExistingFile()
    {
        var initial = new[] { new Book("First", "A", 100) };
        var replacement = new[] { new Book("Second", "B", 200) };

        await _storage.SaveAsync(_tempFile, initial);
        await _storage.SaveAsync(_tempFile, replacement);

        var loaded = await _storage.LoadAsync(_tempFile);
        loaded.Should().Equal(replacement);
    }

    [Fact]
    public async Task SaveAsync_WritesUtf8WithoutBom()
    {
        await _storage.SaveAsync(_tempFile, new[] { new Book("Test", "Author", 10) });

        var bytes = await File.ReadAllBytesAsync(_tempFile);

        // BOM = 0xEF 0xBB 0xBF
        bytes.Take(3).Should().NotEqual(new byte[] { 0xEF, 0xBB, 0xBF });
    }

    [Fact]
    public async Task SaveAsync_PreservesUnicodeCharacters()
    {
        var book = new Book("Война и мир", "Лев Толстой", 1225);

        await _storage.SaveAsync(_tempFile, new[] { book });
        var loaded = await _storage.LoadAsync(_tempFile);

        loaded.Should().ContainSingle().Which.Should().Be(book);
    }

    [Fact]
    public async Task SaveAsync_PreservesXmlSpecialCharacters()
    {
        var book = new Book("A & B <script>", "X \"Y\" Z", 100);

        await _storage.SaveAsync(_tempFile, new[] { book });
        var loaded = await _storage.LoadAsync(_tempFile);

        loaded.Should().ContainSingle().Which.Should().Be(book);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SaveAsync_WithInvalidPath_Throws(string? path)
    {
        var act = async () => await _storage.SaveAsync(path!, Array.Empty<Book>());

        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("path");
    }

    [Fact]
    public async Task SaveAsync_WithNullBooks_Throws()
    {
        var act = async () => await _storage.SaveAsync(_tempFile, null!);

        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("books");
    }

    [Fact]
    public async Task SaveAsync_ToNonExistentDirectory_ThrowsBookLibraryException()
    {
        var invalidPath = Path.Combine(
            Path.GetTempPath(),
            $"nonexistent_{Guid.NewGuid():N}",
            "file.xml");

        var act = async () => await _storage.SaveAsync(invalidPath, Array.Empty<Book>());

        await act.Should().ThrowAsync<BookLibraryException>()
        .WithInnerException(typeof(DirectoryNotFoundException));
    }

    // ─── Load ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task LoadAsync_FileNotFound_ThrowsBookLibraryException()
    {
        var act = async () => await _storage.LoadAsync(_tempFile);

        var exception = await act.Should().ThrowAsync<BookLibraryException>();
        exception.And.InnerException.Should().BeOfType<FileNotFoundException>();

    }

    [Fact]
    public async Task LoadAsync_MalformedXml_ThrowsBookLibraryException()
    {
        await File.WriteAllTextAsync(_tempFile, "not <valid> xml @ all");

        var act = async () => await _storage.LoadAsync(_tempFile);

        await act.Should().ThrowAsync<BookLibraryException>()
            .WithInnerException(typeof(System.Xml.XmlException));
    }

    [Fact]
    public async Task LoadAsync_WrongRootElement_ThrowsBookLibraryException()
    {
        await File.WriteAllTextAsync(_tempFile, "<wrong><book/></wrong>");

        var act = async () => await _storage.LoadAsync(_tempFile);

        await act.Should().ThrowAsync<BookLibraryException>()
            .WithMessage("*library*");
    }

    [Fact]
    public async Task LoadAsync_EmptyLibrary_ReturnsEmpty()
    {
        await File.WriteAllTextAsync(_tempFile, "<library></library>");

        var loaded = await _storage.LoadAsync(_tempFile);

        loaded.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadAsync_SelfClosingEmptyLibrary_ReturnsEmpty()
    {
        await File.WriteAllTextAsync(_tempFile, "<library/>");

        var loaded = await _storage.LoadAsync(_tempFile);

        loaded.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadAsync_BookMissingTitle_ThrowsBookLibraryException()
    {
        const string xml = """
            <library>
              <book>
                <author>Orwell</author>
                <pages>328</pages>
              </book>
            </library>
            """;
        await File.WriteAllTextAsync(_tempFile, xml);

        var act = async () => await _storage.LoadAsync(_tempFile);

        await act.Should().ThrowAsync<BookLibraryException>()
            .WithMessage("*title*");
    }

    [Fact]
    public async Task LoadAsync_DirectoryNotFound_ThrowsBookLibraryException()
    {
        var invalidPath = Path.Combine(
            Path.GetTempPath(),
            $"nonexistent_dir_{Guid.NewGuid():N}",
            "file.xml");

        var act = async () => await _storage.LoadAsync(invalidPath);

        var exception = await act.Should().ThrowAsync<BookLibraryException>();
        exception.And.InnerException.Should().BeOfType<DirectoryNotFoundException>();
    }
    [Fact]
    public async Task LoadAsync_BookMissingPages_ThrowsBookLibraryException()
    {
        const string xml = """
            <library>
              <book>
                <title>1984</title>
                <author>Orwell</author>
              </book>
            </library>
            """;
        await File.WriteAllTextAsync(_tempFile, xml);

        var act = async () => await _storage.LoadAsync(_tempFile);

        await act.Should().ThrowAsync<BookLibraryException>()
            .WithMessage("*pages*");
    }

    [Fact]
    public async Task LoadAsync_BookWithNonIntegerPages_ThrowsBookLibraryException()
    {
        const string xml = """
            <library>
              <book>
                <title>1984</title>
                <author>Orwell</author>
                <pages>not-a-number</pages>
              </book>
            </library>
            """;
        await File.WriteAllTextAsync(_tempFile, xml);

        var act = async () => await _storage.LoadAsync(_tempFile);

        await act.Should().ThrowAsync<BookLibraryException>()
            .WithMessage("*pages*");
    }

    [Fact]
    public async Task LoadAsync_BookWithNegativePages_ThrowsBookLibraryException()
    {
        const string xml = """
            <library>
              <book>
                <title>1984</title>
                <author>Orwell</author>
                <pages>-1</pages>
              </book>
            </library>
            """;
        await File.WriteAllTextAsync(_tempFile, xml);

        var act = async () => await _storage.LoadAsync(_tempFile);

        var exception = await act.Should().ThrowAsync<BookLibraryException>();
        exception.And.InnerException.Should().BeOfType<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task LoadAsync_BookWithEmptyTitle_ThrowsBookLibraryException()
    {
        const string xml = """
            <library>
              <book>
                <title></title>
                <author>Orwell</author>
                <pages>328</pages>
              </book>
            </library>
            """;
        await File.WriteAllTextAsync(_tempFile, xml);

        var act = async () => await _storage.LoadAsync(_tempFile);

        var exception = await act.Should().ThrowAsync<BookLibraryException>();
        exception.And.InnerException.Should().BeOfType<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task LoadAsync_WithInvalidPath_Throws(string? path)
    {
        var act = async () => await _storage.LoadAsync(path!);

        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("path");
    }

    [Fact]
    public async Task LoadAsync_PreservesBookOrder()
    {
        var books = new[]
        {
            new Book("Zoo", "Z Author", 100),
            new Book("Apple", "A Author", 200),
            new Book("Middle", "M Author", 150),
        };

        await _storage.SaveAsync(_tempFile, books);
        var loaded = await _storage.LoadAsync(_tempFile);

        loaded.Should().Equal(books);
    }

    // ─── Cancellation ─────────────────────────────────────────────────────

    [Fact]
    public async Task LoadAsync_WhenCancelled_ThrowsOperationCanceledException()
    {
        await _storage.SaveAsync(_tempFile, new[] { new Book("Test", "Author", 10) });
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () => await _storage.LoadAsync(_tempFile, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}