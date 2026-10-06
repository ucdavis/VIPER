using Microsoft.EntityFrameworkCore;
using Viper.Areas.Personnel;
using Viper.Areas.Personnel.Models.Eis;

namespace Viper.test.Personnel;

/// <summary>
/// The Address page's campus directory rules: which listings count as public and how the
/// address line is put together from the directory's separate fields.
/// </summary>
public sealed class EisCampusListingsTests
{
    private static Dictionary<string, string> Attributes(params (string Name, string Value)[] values)
    {
        return values.ToDictionary(value => value.Name, value => value.Value, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_BuildsTheAddressFromItsParts()
    {
        EisCampusListing listing = EisCampusListings.Create(
            Attributes(
                ("title", " Professor "),
                ("ou", "VM: VME"),
                ("street", "1 Shields Ave"),
                ("l", "Davis"),
                ("st", "CA"),
                ("postalCode", "95616"),
                ("telephoneNumber", "530-752-1000")),
            isPrimary: true);

        Assert.Equal(
            new EisCampusListing(true, true, "Professor", "VM: VME", "1 Shields Ave, Davis, CA 95616", "530-752-1000"),
            listing);
    }

    [Fact]
    public void Create_LeavesOutBlankPartsAndEmptyAddresses()
    {
        Assert.Equal("Davis, 95616", EisCampusListings.Create(Attributes(("l", "Davis"), ("postalCode", "95616")), false).Address);
        Assert.Null(EisCampusListings.Create(Attributes(("street", " ")), false).Address);
    }

    [Theory]
    [InlineData("W", false, true)]
    [InlineData("w", false, true)]
    [InlineData("N", false, false)]
    [InlineData(null, false, false)]
    [InlineData(null, true, true)]
    public void Create_AdditionalListingsArePublicOnlyWhenPublished(string? publish, bool isPrimary, bool expected)
    {
        Dictionary<string, string> attributes = publish is null ? Attributes() : Attributes(("ucdPublish", publish));

        Assert.Equal(expected, EisCampusListings.Create(attributes, isPrimary).IsPublic);
    }

    [Theory]
    [InlineData("2", 2)]
    [InlineData("x", int.MaxValue)]
    [InlineData(null, int.MaxValue)]
    public void ListingOrder_ReadsTheNumberOrSortsLast(string? order, int expected)
    {
        Dictionary<string, string> attributes = order is null ? Attributes() : Attributes(("ucdListingOrder", order));

        Assert.Equal(expected, EisCampusListings.ListingOrder(attributes));
    }

    [Fact]
    public void Create_And_ListingOrder_RejectNull()
    {
        Assert.Throws<ArgumentNullException>(() => EisCampusListings.Create(null!, false));
        Assert.Throws<ArgumentNullException>(() => EisCampusListings.ListingOrder(null!));
    }

    [Fact]
    public void AcademicPersonnelContext_CanBeCreated()
    {
        var options = new DbContextOptionsBuilder<AcademicPersonnelContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new AcademicPersonnelContext(options);

        Assert.NotNull(context.Database);
    }
}
