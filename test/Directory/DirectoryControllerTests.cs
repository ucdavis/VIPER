using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Viper.Areas.Directory.Controllers;
using Viper.Classes.SQLContext;
using Viper.Models.AAUD;

// Not Viper.test.Directory: a namespace segment named "Directory" shadows System.IO.Directory
// for every file under Viper.test.*
// ReSharper disable once CheckNamespace
namespace Viper.test.DirectoryArea;

/// <summary>
/// Tests for the shared directory search query, run against the relational SQLite
/// provider so the predicate goes through EF's SQL translator like it does on SQL
/// Server (the InMemory provider executes LINQ in memory and skips translation).
/// SQLite string matching is case-sensitive where SQL Server's default collation is
/// not, so seed data matches the search term's exact case.
/// </summary>
public sealed class DirectoryControllerTests : IDisposable
{
    /// <summary>
    /// A trimmed AAUDContext mapping only AaudUser: the full AAUD warehouse model has
    /// computed columns and views SQLite EnsureCreated cannot build (same pattern as
    /// StudentGroupServiceQueryTests). Current is a plain column here, so tests set it
    /// directly instead of via the production computed column.
    /// </summary>
    private sealed class TestAaudContext(DbContextOptions<AAUDContext> options) : AAUDContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            foreach (var clrType in modelBuilder.Model.GetEntityTypes()
                         .Select(e => e.ClrType)
                         .Where(t => t != typeof(AaudUser))
                         .Distinct()
                         .ToList())
            {
                modelBuilder.Ignore(clrType);
            }
            modelBuilder.Entity<AaudUser>().HasKey(e => e.AaudUserId);
        }
    }

    private readonly SqliteConnection _connection;
    private readonly AAUDContext _context;

    public DirectoryControllerTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _context = new TestAaudContext(
            new DbContextOptionsBuilder<AAUDContext>().UseSqlite(_connection).Options);
        _context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task SearchCurrentAaudUsers_MatchesNameAndEveryIdentifierField()
    {
        SeedUser(1, lastName: "Delfigo", firstName: "Ann");
        SeedUser(2, lastName: "Berry", firstName: "Bo", mailId: "fig@ucdavis.edu");
        SeedUser(3, lastName: "Cherry", firstName: "Cy", loginId: "figuser");
        SeedUser(4, lastName: "Damson", firstName: "Di", spridenId: "fig123");
        SeedUser(5, lastName: "Elder", firstName: "Ed", pidm: "00fig");
        SeedUser(6, lastName: "Fennel", firstName: "Flo", mothraId: "fig999");
        SeedUser(7, lastName: "Guava", firstName: "Gil", employeeId: "emp-fig");
        SeedUser(8, lastName: "Haw", firstName: "Hal", iamId: "iam-fig");
        SeedUser(9, lastName: "Ivy", firstName: "Ira");
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var results = await DirectoryController.SearchCurrentAaudUsers(_context, "fig");

        // One match per field, none for user 9, ordered by last name
        Assert.Equal([2, 3, 4, 1, 5, 6, 7, 8], results.Select(u => u.AaudUserId));
    }

    [Fact]
    public async Task SearchCurrentAaudUsers_MatchesTermSpanningFirstAndLastName()
    {
        SeedUser(1, lastName: "Graham", firstName: "Anna");
        SeedUser(2, lastName: "Graham", firstName: "Steve");
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var results = await DirectoryController.SearchCurrentAaudUsers(_context, "na Gra");

        Assert.Equal([1], results.Select(u => u.AaudUserId));
    }

    [Fact]
    public async Task SearchCurrentAaudUsers_ExcludesUsersNoLongerCurrent()
    {
        SeedUser(1, lastName: "Figworth", firstName: "Cur");
        SeedUser(2, lastName: "Figworth", firstName: "Old", current: 0);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var results = await DirectoryController.SearchCurrentAaudUsers(_context, "Figworth");

        Assert.Equal([1], results.Select(u => u.AaudUserId));
    }

    [Fact]
    public async Task SearchCurrentAaudUsers_OrdersByLastNameThenFirstName()
    {
        SeedUser(1, lastName: "Fig", firstName: "Zoe");
        SeedUser(2, lastName: "Fig", firstName: "Al");
        SeedUser(3, lastName: "Elm", firstName: "Bea", mailId: "Fig@ucdavis.edu");
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var results = await DirectoryController.SearchCurrentAaudUsers(_context, "Fig");

        Assert.Equal([3, 2, 1], results.Select(u => u.AaudUserId));
    }

    private void SeedUser(int id, string lastName, string firstName, string? mailId = null,
        string? loginId = null, string? spridenId = null, string? pidm = null,
        string? mothraId = null, string? employeeId = null, string? iamId = null, int current = 1)
    {
        _context.AaudUsers.Add(new AaudUser
        {
            AaudUserId = id,
            ClientId = "test",
            MothraId = mothraId ?? $"m-{id}",
            LoginId = loginId,
            MailId = mailId,
            SpridenId = spridenId,
            Pidm = pidm,
            EmployeeId = employeeId,
            IamId = iamId,
            LastName = lastName,
            FirstName = firstName,
            DisplayLastName = lastName,
            DisplayFirstName = firstName,
            DisplayFullName = firstName + " " + lastName,
            Current = current
        });
    }
}
