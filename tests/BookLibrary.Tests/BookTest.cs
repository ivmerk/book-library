using BookLibrary.Domain;
using FluentAssertions;

namespace BookLibrary.Tests;

public class BookTests
{
    [Fact]
    public void Ctor_WithValidData_CreatesBook()
    {
        var book = new Book("1984", "Orwell", 328);

        book.Title.Should().Be("1984");
        book.Author.Should().Be("Orwell");
        book.Pages.Should().Be(328);
    }

    [Fact]
    public void Ctor_TrimsTitleAndAuthor()
    {
        var book = new Book("  1984  ", "\tOrwell\n", 328);

        book.Title.Should().Be("1984");
        book.Author.Should().Be("Orwell");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public void Ctor_WithInvalidTitle_Throws(string? title)
    {
        var act = () => new Book(title!, "Orwell", 328);

        act.Should().Throw<ArgumentException>().WithParameterName("title");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Ctor_WithInvalidAuthor_Throws(string? author)
    {
        var act = () => new Book("1984", author!, 328);

        act.Should().Throw<ArgumentException>().WithParameterName("author");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    [InlineData(Book.MaxPages + 1)]
    public void Ctor_WithInvalidPages_Throws(int pages)
    {
        var act = () => new Book("1984", "Orwell", pages);

        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("pages");
    }

    [Fact]
    public void Ctor_WithTooLongTitle_Throws()
    {
        var longTitle = new string('a', Book.MaxTextLength + 1);

        var act = () => new Book(longTitle, "Orwell", 328);

        act.Should().Throw<ArgumentException>().WithParameterName("title");
    }

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        var a = new Book("1984", "Orwell", 328);
        var b = new Book("1984", "Orwell", 328);

        a.Should().Be(b);
        (a == b).Should().BeTrue();
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void Equality_DifferingPages_AreNotEqual()
    {
        var a = new Book("1984", "Orwell", 328);
        var b = new Book("1984", "Orwell", 329);

        a.Should().NotBe(b);
    }

    [Fact]
    public void Equality_AfterTrim_AreEqual()
    {
        var a = new Book("  1984  ", "Orwell", 328);
        var b = new Book("1984", "Orwell", 328);

        a.Should().Be(b);
    }
}
