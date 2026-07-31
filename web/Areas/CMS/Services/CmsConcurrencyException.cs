namespace Viper.Areas.CMS.Services
{
    /// <summary>
    /// Thrown when an update is based on a stale copy of a record (someone else saved since
    /// the editor loaded it). Controllers translate this to 409 Conflict. Shared by the
    /// content-block and file stale-edit guards.
    /// </summary>
    public class CmsConcurrencyException : InvalidOperationException
    {
        public CmsConcurrencyException(string message) : base(message) { }
    }
}
