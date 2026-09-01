using Microsoft.EntityFrameworkCore;
using Viper.Classes.Utilities;

namespace Viper.test.Classes.Utilities;

/// <summary>
/// Tests for the rule that decides whether a failed write was the caller's fault. A data rejection
/// earns a 400 telling them to check their values; anything else is infrastructure and belongs on
/// the 500 path, where ApiExceptionFilterAttribute logs at Error and returns a correlation ID.
/// Getting this backwards either blames the user for an outage or hides one at Warning.
/// </summary>
public class DbUpdateExceptionExtensionsTests
{
    [Theory]
    [InlineData(2601)] // duplicate key in a unique index
    [InlineData(2627)] // unique or primary key constraint violation
    [InlineData(547)]  // foreign key or check constraint conflict
    [InlineData(8152)] // string or binary data would be truncated
    [InlineData(2628)] // the same truncation, in the newer form that names the column
    public void IsDataRejectionNumber_IsTrue_ForTheErrorsACallerCanFix(int number)
    {
        Assert.True(DbUpdateExceptionExtensions.IsDataRejectionNumber(number));
    }

    [Theory]
    [InlineData(1205)]  // deadlock victim - retryable, and nothing to do with the submitted values
    [InlineData(-2)]    // command timeout
    [InlineData(53)]    // network path not found
    [InlineData(4060)]  // cannot open database
    [InlineData(18456)] // login failed
    [InlineData(0)]
    public void IsDataRejectionNumber_IsFalse_ForInfrastructureFailures(int number)
    {
        // These reached the old catch too, and answered "please check all field values are valid"
        // at Warning. A deadlock victim is the case that prompted this split.
        Assert.False(DbUpdateExceptionExtensions.IsDataRejectionNumber(number));
    }

    [Fact]
    public void IsDataRejection_IsFalse_WhenThereIsNoInnerException()
    {
        var ex = new DbUpdateException("Something failed");

        Assert.False(ex.IsDataRejection());
    }

    [Fact]
    public void IsDataRejection_IsFalse_WhenTheInnerExceptionIsNotFromSqlServer()
    {
        // EF wraps whatever the provider threw, so the inner exception is not always a SqlException.
        var ex = new DbUpdateException("Something failed", new TimeoutException("The wait expired"));

        Assert.False(ex.IsDataRejection());
    }
}
