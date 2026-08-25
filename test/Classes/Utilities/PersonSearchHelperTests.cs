using Microsoft.EntityFrameworkCore;
using Viper.Classes.Utilities;

namespace Viper.test.Classes.Utilities;

/// <summary>
/// Tests for PersonSearchHelper, the shared "search current people by partial name" query shape
/// used by both the CMS file/permission pickers and the Personnel phone directory. A regression
/// here affects every autocomplete built on it.
/// </summary>
public class PersonSearchHelperTests
{
    private sealed class Person
    {
        public required string LastName { get; set; }
        public required string FirstName { get; set; }
        public string LoginId { get; set; } = "";
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("a")]
    [InlineData(" a ")]
    public void Normalize_ReturnsNull_WhenBelowMinimumLength(string? search)
    {
        Assert.Null(PersonSearchHelper.Normalize(search));
    }

    [Fact]
    public void Normalize_ReturnsTrimmedValue_WhenAtOrAboveMinimumLength()
    {
        var result = PersonSearchHelper.Normalize("  ab  ");

        Assert.Equal("ab", result);
    }

    [Fact]
    public void NameMatches_MatchesLastCommaFirstForm()
    {
        var people = new[] { new Person { LastName = "Smith", FirstName = "Amy" } }.AsQueryable();
        var predicate = PersonSearchHelper.NameMatches<Person>(p => p.LastName, p => p.FirstName, "mith, A");

        var results = people.Where(predicate).ToList();

        Assert.Single(results);
    }

    [Fact]
    public void NameMatches_MatchesFirstSpaceLastForm()
    {
        var people = new[] { new Person { LastName = "Smith", FirstName = "Amy" } }.AsQueryable();
        var predicate = PersonSearchHelper.NameMatches<Person>(p => p.LastName, p => p.FirstName, "Amy Sm");

        var results = people.Where(predicate).ToList();

        Assert.Single(results);
    }

    [Fact]
    public void NameMatches_ExcludesNonMatchingPeople()
    {
        var people = new[]
        {
            new Person { LastName = "Smith", FirstName = "Amy" },
            new Person { LastName = "Jones", FirstName = "Bob" },
        }.AsQueryable();
        var predicate = PersonSearchHelper.NameMatches<Person>(p => p.LastName, p => p.FirstName, "Smith");

        var results = people.Where(predicate).ToList();

        var match = Assert.Single(results);
        Assert.Equal("Smith", match.LastName);
    }

    [Fact]
    public void OrderAndCap_OrdersByLastNameThenFirstName_AndCapsToMaxResults()
    {
        var people = Enumerable.Range(0, 30)
            .Select(i => new Person { LastName = $"Person{i:D2}", FirstName = "X" })
            .Reverse()
            .AsQueryable();

        var results = PersonSearchHelper.OrderAndCap(people, p => p.LastName, p => p.FirstName).ToList();

        Assert.Equal(PersonSearchHelper.MaxResults, results.Count);
        Assert.Equal("Person00", results[0].LastName);
        Assert.Equal("Person24", results[^1].LastName);
    }

    [Fact]
    public void Or_IncludesRecordsMatchingEitherPredicate()
    {
        var people = new[]
        {
            new Person { LastName = "Smith", FirstName = "Amy", LoginId = "asmith" },
            new Person { LastName = "Jones", FirstName = "Bob", LoginId = "bjones" },
        }.AsQueryable();
        var namePredicate = PersonSearchHelper.NameMatches<Person>(p => p.LastName, p => p.FirstName, "Smith");
        var combined = namePredicate.Or<Person>(p => p.LoginId == "bjones");

        var results = people.Where(combined).ToList();

        Assert.Equal(2, results.Count);
    }

    private sealed class SearchTestContext(DbContextOptions<SearchTestContext> options) : DbContext(options)
    {
        public DbSet<Person> People => Set<Person>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => modelBuilder.Entity<Person>().HasKey(p => p.LastName);
    }

    [Fact]
    public void NameMatches_EmitsASqlParameter_RatherThanALiteral()
    {
        // These autocompletes fire per keystroke, so a term embedded as a literal would give every
        // distinct search its own query plan. The literal form also drops the ESCAPE clause, which
        // is what stops a typed % or _ from being treated as a wildcard.
        var options = new DbContextOptionsBuilder<SearchTestContext>()
            .UseSqlServer("Server=none;Database=none;Trusted_Connection=True;")
            .Options;
        using var context = new SearchTestContext(options);

        var sql = context.People
            .Where(PersonSearchHelper.NameMatches<Person>(p => p.LastName, p => p.FirstName, "smith"))
            .ToQueryString();

        // Assert on the predicate, not the whole string: ToQueryString prefixes a DECLARE that
        // spells the value out for copy-paste even when the query itself is parameterized.
        Assert.Contains("LIKE @", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("LIKE N'", sql, StringComparison.Ordinal);
        Assert.Contains("ESCAPE", sql, StringComparison.Ordinal);
    }
}
