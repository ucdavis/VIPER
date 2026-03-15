using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Text;

namespace Viper.Views.Shared.Components.VueTableDefault
{
    [ViewComponent(Name = "VueTableDefault")]

    public class VueTableDefaultViewComponent : ViewComponent
    {
        public VueTableDefaultViewComponent()
        {
        }

        public async Task<IViewComponentResult> InvokeAsync(IEnumerable<Object>? data, string keyColumnName,
             IEnumerable<string>? skipColumns = null, IEnumerable<Tuple<string, string>>? altColumnNames = null,
             IEnumerable<string>? skipColumnsVisible = null
             )
        {
            ViewData["UniquePrepend"] = User?.Identity?.Name + DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            ViewData["KeyColumnName"] = keyColumnName;
            ViewData["Columns"] = GetDefaultColumnNames(data, skipColumns, altColumnNames);
            ViewData["Rows"] = GetDefaultRows(data, skipColumns);
            ViewData["VisibleColumns"] = GetDefaultVisibleColumns(data, skipColumnsVisible);

            return await Task.Run(() => View("Default"));
        }

        #region public static string GetDefaultColumnNames(IEnumerable<Object>? data, IEnumerable<string>? skipColumns = null, IEnumerable<Tuple<string,string>>? altColumnNames = null)
        /// <summary>
        /// Get the JavaScript "columns" object string for Vue from the properties in an enumerable object
        /// </summary>
        /// <param name="data">Enumerable list of objects</param>
        /// <returns>JavaScript "columns" object string if data is not null</returns>
        public static string GetDefaultColumnNames(IEnumerable<Object>? data, IEnumerable<string>? skipColumns = null, IEnumerable<Tuple<string, string>>? altColumnNames = null)
        {
            StringBuilder output = new StringBuilder();

            if (data != null)
            {
                output.Append('[');

                var includedProperties =
                    (from property in data.First().GetType().GetProperties()
                     where property.PropertyType == typeof(string)
                        || !typeof(System.Collections.IEnumerable).IsAssignableFrom(property.PropertyType) // skip ICollection columns
                     select property)
                    .Where(p => skipColumns == null || !skipColumns.Contains(p.Name))
                    .ToList();

                var last = includedProperties.LastOrDefault();

                foreach (var propery in includedProperties)
                {
                    string propertyName = propery.Name;

                    output.Append('{');
                    output.Append(String.Format("name:'{0}',", propertyName));
                    output.Append("align:'left',");
                    output.Append("sortable:true,");
                    output.Append(String.Format("field:'{0}',", propertyName));

                    if (altColumnNames == null)
                    {
                        output.Append(String.Format("label:'{0}'", propertyName));
                    }
                    else
                    {
                        string? altName = altColumnNames.FirstOrDefault(alt => alt.Item1.ToLower().Equals(propertyName.ToLower()))?.Item2;

                        if (altName != null)
                        {
                            output.Append(String.Format("label:'{0}'", altName));
                        }
                        else
                        {
                            output.Append(String.Format("label:'{0}'", propertyName));
                        }

                    }

                    output.Append('}');

                    if (!propery.Equals(last))
                    {
                        output.Append(',');
                    }

                }

                output.Append(']');
            }

            return output.ToString();
        }
        #endregion

        #region public static string GetDefaultRows(IEnumerable<Object>? data, IEnumerable<string>? skipColumns = null)
        /// <summary>
        /// Get the JavaScript "rows" object string for Vue from the properties in an enumerable object
        /// </summary>
        /// <param name="data">Enumerable list of objects</param>
        /// <returns>JavaScript "rows" object string if data is not null</returns>
        public static string GetDefaultRows(IEnumerable<Object>? data, IEnumerable<string>? skipColumns = null)
        {
            StringBuilder output = new StringBuilder();

            if (data != null)
            {
                var includedProperties =
                    (from property in data.First().GetType().GetProperties()
                     where property.PropertyType == typeof(string)
                        || !typeof(System.Collections.IEnumerable).IsAssignableFrom(property.PropertyType) // skip ICollection columns
                     select property)
                    .Where(p => skipColumns == null || !skipColumns.Contains(p.Name))
                    .ToList();

                var last = includedProperties.LastOrDefault();

                output.Append('[');

                var lastObj = data.Last();

                foreach (var obj in data)
                {
                    output.Append('{');

                    foreach (var propery in includedProperties)
                    {
                        string? value = obj.GetType()?.GetProperty(propery.Name)?.GetValue(obj)?.ToString();

                        if (propery.PropertyType.Name.ToLower() == "string" || propery.PropertyType.Name.ToLower() == "datetime")
                        {
                            value = "'" + value?.Replace("'", "\\'").Replace("\"", "\\\"").Replace("\n", "").Replace("\r", "") + "'";
                        }
                        else if (propery.PropertyType.Name.ToLower().Contains("bool"))
                        {
                            value = value?.ToLower();
                        }

                        output.Append(String.Format("'{0}':{1}", propery.Name, value));

                        if (!propery.Equals(last))
                        {
                            output.Append(',');
                        }
                    }

                    output.Append('}');

                    if (!obj.Equals(lastObj))
                    {
                        output.Append(',');
                    }

                }

                output.Append(']');
            }

            return output.ToString();
        }
        #endregion

        #region public static string GetDefaultVisibleColumns(IEnumerable<Object>? data, IEnumerable<string>? skipColumns = null)
        /// <summary>
        /// Get the JavaScript "visibleColumns" object string for Vue from the properties in an enumerable object
        /// </summary>
        /// <param name="data">Enumerable list of objects</param>
        /// <returns>JavaScript "visibleColumns" object string if data is not null</returns>
        public static string GetDefaultVisibleColumns(IEnumerable<Object>? data, IEnumerable<string>? skipColumns = null)
        {
            StringBuilder output = new StringBuilder();

            if (data != null)
            {
                output.Append('[');

                var includedProperties =
                    (from property in data.First().GetType().GetProperties()
                     where property.PropertyType == typeof(string)
                        || !typeof(System.Collections.IEnumerable).IsAssignableFrom(property.PropertyType) // skip ICollection columns
                     select property)
                    .Where(p => skipColumns == null || !skipColumns.Contains(p.Name))
                    .ToList();

                var last = includedProperties.LastOrDefault();

                foreach (var propery in includedProperties)
                {
                    output.Append("'" + propery.Name + "'");

                    if (!propery.Equals(last))
                    {
                        output.Append(',');
                    }
                }

                output.Append(']');
            }

            return output.ToString();
        }
        #endregion

    }
}
