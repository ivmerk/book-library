using BookLibrary.Abstractions;
using BookLibrary.Domain;
using BookLibrary.Exceptions;
using BookLibrary.Services;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace BookLibrary.Tests;

public class BookRepositoryTests
{
  // ─── Helpers ──────────────────────────────────────────────────────────

  private static Book MakeBook(string title = "1984", string author = "Orwell", int pages = 328) =>
      new(title, author, pages);

  private static BookRepository CreateRepository(IBookStorage? storage = null) =>
      new(storage ?? Substitute.For<IBookStorage>());

  // ─── Constructor ──────────────────────────────────────────────────────

  [Fact]
  public void Ctor_WithNullStorage_Throws()
  {
    var act = () => new BookRepository(null!);

    act.Should().Throw<ArgumentNullException>().WithParameterName("storage");
  }

  // ─── GetAll ───────────────────────────────────────────────────────────

  [Fact]
  public void GetAll_WhenEmpty_ReturnsEmptyList()
  {
    var repo = CreateRepository();

    repo.GetAll().Should().BeEmpty();
  }

  [Fact]
  public void GetAll_AfterAdd_ReturnsAllBooksInInsertionOrder()
  {
    var repo = CreateRepository();
    var b1 = MakeBook(title: "B");
    var b2 = MakeBook(title: "A");
    var b3 = MakeBook(title: "C");

    repo.Add(b1);
    repo.Add(b2);
    repo.Add(b3);

    repo.GetAll().Should().Equal(b1, b2, b3);
  }

  [Fact]
  public void GetAll_ReturnsSnapshot_NotLiveView()
  {
    var repo = CreateRepository();
    repo.Add(MakeBook(title: "First"));

    var snapshot = repo.GetAll();
    repo.Add(MakeBook(title: "Second"));

    snapshot.Should().HaveCount(1);
    snapshot[0].Title.Should().Be("First");
  }

  [Fact]
  public void GetAll_MutatingReturnedList_DoesNotAffectRepository()
  {
    var repo = CreateRepository();
    repo.Add(MakeBook());

    var snapshot = (List<Book>)repo.GetAll();
    snapshot.Clear();

    repo.GetAll().Should().HaveCount(1);
  }

  // ─── Add ──────────────────────────────────────────────────────────────

  [Fact]
  public void Add_WithValidBook_StoresIt()
  {
    var repo = CreateRepository();
    var book = MakeBook();

    repo.Add(book);

    repo.GetAll().Should().ContainSingle().Which.Should().Be(book);
  }

  [Fact]
  public void Add_WithNull_Throws()
  {
    var repo = CreateRepository();

    var act = () => repo.Add(null!);

    act.Should().Throw<ArgumentNullException>().WithParameterName("book");
  }

  [Fact]
  public void Add_AllowsDuplicates()
  {
    var repo = CreateRepository();
    var book = MakeBook();

    repo.Add(book);
    repo.Add(book);

    repo.GetAll().Should().HaveCount(2);
  }

  // ─── SortedByAuthorThenTitle ──────────────────────────────────────────

  [Fact]
  public void SortedByAuthorThenTitle_WhenEmpty_ReturnsEmpty()
  {
    var repo = CreateRepository();

    repo.SortedByAuthorThenTitle().Should().BeEmpty();
  }

  [Fact]
  public void SortedByAuthorThenTitle_SortsByAuthorFirst()
  {
    var repo = CreateRepository();
    var king = MakeBook(title: "It", author: "King");
    var andersen = MakeBook(title: "The Ugly Duckling", author: "Andersen");

    repo.Add(king);
    repo.Add(andersen);

    repo.SortedByAuthorThenTitle().Should().Equal(andersen, king);
  }

  [Fact]
  public void SortedByAuthorThenTitle_ThenByTitleWithinSameAuthor()
  {
    var repo = CreateRepository();
    var ugly = MakeBook(title: "The Ugly Duckling", author: "Andersen");
    var mermaid = MakeBook(title: "The Little Mermaid", author: "Andersen");

    repo.Add(ugly);
    repo.Add(mermaid);

    // "The Little Mermaid" < "The Ugly Duckling" alphabetically
    repo.SortedByAuthorThenTitle().Should().Equal(mermaid, ugly);
  }

  [Fact]
  public void SortedByAuthorThenTitle_IsCaseInsensitive()
  {
    var repo = CreateRepository();
    var lowercase = MakeBook(title: "zoo", author: "andersen");
    var uppercase = MakeBook(title: "Apple", author: "Andersen");

    repo.Add(lowercase);
    repo.Add(uppercase);

    // "Apple" < "zoo" case-insensitively
    repo.SortedByAuthorThenTitle().Should().Equal(uppercase, lowercase);
  }

  [Fact]
  public void SortedByAuthorThenTitle_FullScenarioFromRequirements()
  {
    var repo = CreateRepository();

    var kingIt = MakeBook(title: "It", author: "King");
    var kingShining = MakeBook(title: "The Shining", author: "King");
    var andersenUgly = MakeBook(title: "The Ugly Duckling", author: "Andersen");
    var andersenMermaid = MakeBook(title: "The Little Mermaid", author: "Andersen");

    repo.Add(kingIt);
    repo.Add(andersenUgly);
    repo.Add(kingShining);
    repo.Add(andersenMermaid);

    repo.SortedByAuthorThenTitle().Should().Equal(
        andersenMermaid,
        andersenUgly,
        kingIt,
        kingShining);
  }

  [Fact]
  public void SortedByAuthorThenTitle_DoesNotMutateInternalState()
  {
    var repo = CreateRepository();
    var b1 = MakeBook(title: "B");
    var b2 = MakeBook(title: "A");
    repo.Add(b1);
    repo.Add(b2);
    repo.SortedByAuthorThenTitle();

    repo.GetAll().Should().Equal(b1, b2);
  }

  // ─── SearchByTitle ────────────────────────────────────────────────────

  [Fact]
  public void SearchByTitle_FindsPartialMatch()
  {
    var repo = CreateRepository();
    var mermaid = MakeBook(title: "The Little Mermaid");
    var duckling = MakeBook(title: "The Ugly Duckling");
    repo.Add(mermaid);
    repo.Add(duckling);

    repo.SearchByTitle("Mermaid").Should().ContainSingle().Which.Should().Be(mermaid);
  }

  [Fact]
  public void SearchByTitle_IsCaseInsensitive()
  {
    var repo = CreateRepository();
    var mermaid = MakeBook(title: "The Little Mermaid");
    repo.Add(mermaid);

    repo.SearchByTitle("MERMAID").Should().ContainSingle().Which.Should().Be(mermaid);
    repo.SearchByTitle("mermaid").Should().ContainSingle().Which.Should().Be(mermaid);
    repo.SearchByTitle("mErMaId").Should().ContainSingle().Which.Should().Be(mermaid);
  }

  [Fact]
  public void SearchByTitle_NoMatch_ReturnsEmpty()
  {
    var repo = CreateRepository();
    repo.Add(MakeBook(title: "The Little Mermaid"));

    repo.SearchByTitle("Hamlet").Should().BeEmpty();
  }

  [Theory]
  [InlineData("")]
  [InlineData("   ")]
  [InlineData("\t\n")]
  public void SearchByTitle_WithEmptyOrWhitespace_ReturnsEmpty(string titlePart)
  {
    var repo = CreateRepository();
    repo.Add(MakeBook(title: "Anything"));

    repo.SearchByTitle(titlePart).Should().BeEmpty();
  }

  [Fact]
  public void SearchByTitle_WithNull_Throws()
  {
    var repo = CreateRepository();

    var act = () => repo.SearchByTitle(null!);

    act.Should().Throw<ArgumentNullException>().WithParameterName("titlePart");
  }

  [Fact]
  public void SearchByTitle_MultipleMatches_ReturnsAllInInsertionOrder()
  {
    var repo = CreateRepository();
    var mermaid = MakeBook(title: "The Little Mermaid");
    var hamlet = MakeBook(title: "Hamlet");
    var anotherMermaid = MakeBook(title: "Mermaid Lagoon");

    repo.Add(mermaid);
    repo.Add(hamlet);
    repo.Add(anotherMermaid);

    repo.SearchByTitle("mermaid").Should().Equal(mermaid, anotherMermaid);
  }

  // ─── LoadFromAsync ────────────────────────────────────────────────────

  [Fact]
  public async Task LoadFromAsync_PopulatesRepositoryFromStorage()
  {
    var storage = Substitute.For<IBookStorage>();
    var books = new[] { MakeBook(title: "A"), MakeBook(title: "B") };
    storage.LoadAsync("books.xml", Arg.Any<CancellationToken>()).Returns(books);

    var repo = CreateRepository(storage);
    await repo.LoadFromAsync("books.xml");

    repo.GetAll().Should().Equal(books);
  }

  [Fact]
  public async Task LoadFromAsync_ReplacesExistingContent()
  {
    var storage = Substitute.For<IBookStorage>();
    var loaded = new[] { MakeBook(title: "Loaded") };
    storage.LoadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(loaded);

    var repo = CreateRepository(storage);
    repo.Add(MakeBook(title: "Existing"));

    await repo.LoadFromAsync("books.xml");

    repo.GetAll().Should().ContainSingle().Which.Title.Should().Be("Loaded");
  }

  [Fact]
  public async Task LoadFromAsync_WhenStorageFails_RepositoryStateUnchanged()
  {
    var storage = Substitute.For<IBookStorage>();
    storage.LoadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
        .Throws(new BookLibraryException("corrupted"));

    var repo = CreateRepository(storage);
    var existing = MakeBook(title: "Existing");
    repo.Add(existing);

    var act = async () => await repo.LoadFromAsync("books.xml");

    await act.Should().ThrowAsync<BookLibraryException>();
    repo.GetAll().Should().ContainSingle().Which.Should().Be(existing);
  }

  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("   ")]
  public async Task LoadFromAsync_WithInvalidPath_Throws(string? path)
  {
    var repo = CreateRepository();

    var act = async () => await repo.LoadFromAsync(path!);

    await act.Should().ThrowAsync<ArgumentException>().WithParameterName("path");
  }

  [Fact]
  public async Task LoadFromAsync_PassesCancellationTokenToStorage()
  {
    var storage = Substitute.For<IBookStorage>();
    storage.LoadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
        .Returns(Array.Empty<Book>());

    var repo = CreateRepository(storage);
    using var cts = new CancellationTokenSource();

    await repo.LoadFromAsync("books.xml", cts.Token);

    await storage.Received(1).LoadAsync("books.xml", cts.Token);
  }

  // ─── SaveToAsync ──────────────────────────────────────────────────────

  [Fact]
  public async Task SaveToAsync_PassesCurrentSnapshotToStorage()
  {
    var storage = Substitute.For<IBookStorage>();
    var repo = CreateRepository(storage);
    var b1 = MakeBook(title: "A");
    var b2 = MakeBook(title: "B");
    repo.Add(b1);
    repo.Add(b2);

    await repo.SaveToAsync("books.xml");

    await storage.Received(1).SaveAsync(
        "books.xml",
        Arg.Is<IEnumerable<Book>>(books => books.SequenceEqual(new[] { b1, b2 })),
        Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task SaveToAsync_WithEmptyRepository_PassesEmptySequence()
  {
    var storage = Substitute.For<IBookStorage>();
    var repo = CreateRepository(storage);

    await repo.SaveToAsync("books.xml");

    await storage.Received(1).SaveAsync(
        "books.xml",
        Arg.Is<IEnumerable<Book>>(books => !books.Any()),
        Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task SaveToAsync_PassesSnapshot_NotLiveReference()
  {
    // Защита от race: если storage будет лениво enumerate'ить переданную коллекцию,
    // мутации репозитория во время записи не должны её испортить.
    var storage = Substitute.For<IBookStorage>();
    IEnumerable<Book>? captured = null;
    storage.SaveAsync(Arg.Any<string>(), Arg.Do<IEnumerable<Book>>(b => captured = b), Arg.Any<CancellationToken>())
        .Returns(Task.CompletedTask);

    var repo = CreateRepository(storage);
    var original = MakeBook(title: "Original");
    repo.Add(original);

    await repo.SaveToAsync("books.xml");

    // Изменяем репозиторий ПОСЛЕ вызова Save
    repo.Add(MakeBook(title: "After Save"));

    // Снимок, переданный в storage, должен остаться с одной книгой
    captured.Should().NotBeNull();
    captured!.Should().ContainSingle().Which.Should().Be(original);
  }

  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("   ")]
  public async Task SaveToAsync_WithInvalidPath_Throws(string? path)
  {
    var repo = CreateRepository();

    var act = async () => await repo.SaveToAsync(path!);

    await act.Should().ThrowAsync<ArgumentException>().WithParameterName("path");
  }

  [Fact]
  public async Task SaveToAsync_WithInvalidPath_DoesNotCallStorage()
  {
    var storage = Substitute.For<IBookStorage>();
    var repo = CreateRepository(storage);

    var act = async () => await repo.SaveToAsync("");

    await act.Should().ThrowAsync<ArgumentException>();
    await storage.DidNotReceive().SaveAsync(
        Arg.Any<string>(), Arg.Any<IEnumerable<Book>>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task SaveToAsync_PropagatesStorageException()
  {
    var storage = Substitute.For<IBookStorage>();
    storage.SaveAsync(Arg.Any<string>(), Arg.Any<IEnumerable<Book>>(), Arg.Any<CancellationToken>())
        .Throws(new BookLibraryException("disk full"));

    var repo = CreateRepository(storage);
    repo.Add(MakeBook());

    var act = async () => await repo.SaveToAsync("books.xml");

    await act.Should().ThrowAsync<BookLibraryException>().WithMessage("disk full");
  }

  [Fact]
  public async Task SaveToAsync_PassesCancellationTokenToStorage()
  {
    var storage = Substitute.For<IBookStorage>();
    var repo = CreateRepository(storage);
    using var cts = new CancellationTokenSource();

    await repo.SaveToAsync("books.xml", cts.Token);

    await storage.Received(1).SaveAsync(
        "books.xml", Arg.Any<IEnumerable<Book>>(), cts.Token);
  }
}