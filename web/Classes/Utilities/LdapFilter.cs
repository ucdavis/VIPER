using System.Text;

namespace Viper.Classes.Utilities
{
    public static class LdapFilter
    {
        /// <summary>
        /// Escapes a value for safe use inside an LDAP search filter per RFC 4515.
        /// Apply to individual assertion values only — never to full filter expressions,
        /// composed fragments, or operator characters.
        /// </summary>
        public static string Escape(string? input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }

            var sb = new StringBuilder(input.Length);
            foreach (var c in input)
            {
                switch (c)
                {
                    case '*': sb.Append(@"\2a"); break;
                    case '(': sb.Append(@"\28"); break;
                    case ')': sb.Append(@"\29"); break;
                    case '\\': sb.Append(@"\5c"); break;
                    case '\0': sb.Append(@"\00"); break;
                    default: sb.Append(c); break;
                }
            }
            return sb.ToString();
        }
    }
}
