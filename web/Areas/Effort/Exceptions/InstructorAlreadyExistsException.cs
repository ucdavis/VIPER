namespace Viper.Areas.Effort.Exceptions;

/// <summary>
/// Thrown when attempting to create an instructor that already exists for a term.
/// </summary>
public class InstructorAlreadyExistsException : Exception
{
    public int PersonId { get; }
    public int TermCode { get; }

    public InstructorAlreadyExistsException(int personId, int termCode)
        : base($"Instructor with PersonId {personId} already exists for term {termCode}")
    {
        PersonId = personId;
        TermCode = termCode;
    }
}
