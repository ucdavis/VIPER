namespace Viper.Areas.Effort.Exceptions;

/// <summary>
/// Thrown when an update or delete fails due to the record being modified by another user
/// since it was loaded (optimistic concurrency conflict).
/// </summary>
public class ConcurrencyConflictException : Exception
{
    private const string DefaultMessage = "This record was modified by another user. Please refresh and try again.";

    public int RecordId { get; }

    public ConcurrencyConflictException(int recordId)
        : base(DefaultMessage)
    {
        RecordId = recordId;
    }
}
