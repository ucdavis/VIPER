using System.Reflection;
using Microsoft.Data.SqlClient;

namespace Viper.test.ClinicalScheduler
{
    /// <summary>
    /// Builds <see cref="SqlException"/> instances carrying a specific SQL Server error number.
    /// SqlException/SqlError/SqlErrorCollection have no public constructors, so this reaches the
    /// library's internal members via reflection.
    /// </summary>
    internal static class SqlExceptionFactory
    {
        private const BindingFlags Members = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        public static SqlException Create(int errorNumber)
        {
            var errors = (SqlErrorCollection)Activator.CreateInstance(typeof(SqlErrorCollection), nonPublic: true)!;
            typeof(SqlErrorCollection).GetMethod("Add", Members)!.Invoke(errors, new object[] { NewError(errorNumber) });

            var createException = typeof(SqlException).GetMethod(
                "CreateException",
                BindingFlags.Static | BindingFlags.NonPublic,
                binder: null,
                types: new[] { typeof(SqlErrorCollection), typeof(string) },
                modifiers: null)!;
            return (SqlException)createException.Invoke(null, new object[] { errors, "11.0.0" })!;
        }

        // Every SqlError constructor starts with the error number (int infoNumber); pick the shortest
        // and fill the remaining parameters (only int/byte/string/Exception occur) with type defaults.
        private static SqlError NewError(int number)
        {
            var ctor = typeof(SqlError).GetConstructors(Members)
                .Where(c => c.GetParameters().FirstOrDefault()?.ParameterType == typeof(int))
                .OrderBy(c => c.GetParameters().Length)
                .First();

            var args = ctor.GetParameters()
                .Select((p, i) => i == 0 ? number : Default(p.ParameterType))
                .ToArray();
            return (SqlError)ctor.Invoke(args);
        }

        private static object? Default(Type type)
        {
            if (type == typeof(string))
            {
                return string.Empty;
            }
            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }
    }
}
