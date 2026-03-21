using Viper.Views.Shared.Components.VueTableDefault;

namespace Viper.test
{
    public class VueTableDefaultTests
    {
        // Simple model with string, int, bool properties for testing
        private class SampleRow
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
            public bool Active { get; set; }
        }

        // Model with a collection property that should be auto-skipped
        private class RowWithCollection
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
            public List<string> Tags { get; set; } = new();
        }

        private static List<object> SampleData() => new()
        {
            new SampleRow { Id = 1, Name = "Alice", Active = true },
            new SampleRow { Id = 2, Name = "Bob", Active = false }
        };

        #region GetDefaultColumnNames

        [Fact]
        public void GetDefaultColumnNames_NullData_ReturnsEmptyString()
        {
            var result = VueTableDefaultViewComponent.GetDefaultColumnNames(null);
            Assert.Equal("", result);
        }

        [Fact]
        public void GetDefaultColumnNames_ReturnsAllScalarProperties()
        {
            var result = VueTableDefaultViewComponent.GetDefaultColumnNames(SampleData());

            Assert.Contains("name:'Id'", result);
            Assert.Contains("name:'Name'", result);
            Assert.Contains("name:'Active'", result);
        }

        [Fact]
        public void GetDefaultColumnNames_SkipsCollectionProperties()
        {
            var data = new List<object>
            {
                new RowWithCollection { Id = 1, Name = "Alice", Tags = new() { "a" } }
            };

            var result = VueTableDefaultViewComponent.GetDefaultColumnNames(data);

            Assert.Contains("name:'Id'", result);
            Assert.Contains("name:'Name'", result);
            Assert.DoesNotContain("Tags", result);
        }

        [Fact]
        public void GetDefaultColumnNames_SkipColumns_ExcludesSpecifiedColumns()
        {
            var result = VueTableDefaultViewComponent.GetDefaultColumnNames(
                SampleData(), skipColumns: new[] { "Active" });

            Assert.Contains("name:'Id'", result);
            Assert.Contains("name:'Name'", result);
            Assert.DoesNotContain("Active", result);
        }

        [Fact]
        public void GetDefaultColumnNames_AltColumnNames_SubstitutesLabels()
        {
            var altNames = new List<Tuple<string, string>>
            {
                Tuple.Create("Name", "Full Name")
            };

            var result = VueTableDefaultViewComponent.GetDefaultColumnNames(
                SampleData(), altColumnNames: altNames);

            Assert.Contains("label:'Full Name'", result);
            // Id should keep its original label
            Assert.Contains("label:'Id'", result);
        }

        [Fact]
        public void GetDefaultColumnNames_NoTrailingComma()
        {
            var result = VueTableDefaultViewComponent.GetDefaultColumnNames(SampleData());

            // Should end with }] not ,]
            Assert.EndsWith("]", result);
            Assert.DoesNotContain(",]", result);
        }

        [Fact]
        public void GetDefaultColumnNames_SkipAllColumns_ReturnsEmptyArray()
        {
            var result = VueTableDefaultViewComponent.GetDefaultColumnNames(
                SampleData(), skipColumns: new[] { "Id", "Name", "Active" });

            Assert.Equal("[]", result);
        }

        #endregion

        #region GetDefaultRows

        [Fact]
        public void GetDefaultRows_NullData_ReturnsEmptyString()
        {
            var result = VueTableDefaultViewComponent.GetDefaultRows(null);
            Assert.Equal("", result);
        }

        [Fact]
        public void GetDefaultRows_StringValuesAreQuoted()
        {
            var result = VueTableDefaultViewComponent.GetDefaultRows(SampleData());

            // String values should be wrapped in single quotes
            Assert.Contains("'Name':'Alice'", result);
            Assert.Contains("'Name':'Bob'", result);
        }

        [Fact]
        public void GetDefaultRows_BoolValuesAreLowercase()
        {
            var result = VueTableDefaultViewComponent.GetDefaultRows(SampleData());

            Assert.Contains("'Active':true", result);
            Assert.Contains("'Active':false", result);
        }

        [Fact]
        public void GetDefaultRows_IntValuesAreUnquoted()
        {
            var result = VueTableDefaultViewComponent.GetDefaultRows(SampleData());

            Assert.Contains("'Id':1", result);
            Assert.Contains("'Id':2", result);
        }

        [Fact]
        public void GetDefaultRows_SkipColumns_ExcludesSpecifiedColumns()
        {
            var result = VueTableDefaultViewComponent.GetDefaultRows(
                SampleData(), skipColumns: new[] { "Active" });

            Assert.Contains("'Id':1", result);
            Assert.Contains("'Name':'Alice'", result);
            Assert.DoesNotContain("Active", result);
        }

        [Fact]
        public void GetDefaultRows_MultipleRows_CommaDelimited()
        {
            var result = VueTableDefaultViewComponent.GetDefaultRows(SampleData());

            // Two row objects separated by comma
            Assert.StartsWith("[{", result);
            Assert.EndsWith("}]", result);
            Assert.Contains("},{", result);
            Assert.DoesNotContain(",]", result);
        }

        [Fact]
        public void GetDefaultRows_SkipAllColumns_ReturnsEmptyRowObjects()
        {
            var result = VueTableDefaultViewComponent.GetDefaultRows(
                SampleData(), skipColumns: new[] { "Id", "Name", "Active" });

            // Each row should be an empty object
            Assert.Equal("[{},{}]", result);
        }

        [Fact]
        public void GetDefaultRows_SpecialCharacters_AreEscaped()
        {
            var data = new List<object>
            {
                new SampleRow { Id = 1, Name = "O'Brien", Active = true }
            };

            var result = VueTableDefaultViewComponent.GetDefaultRows(data);

            Assert.Contains("O\\'Brien", result);
        }

        #endregion

        #region GetDefaultVisibleColumns

        [Fact]
        public void GetDefaultVisibleColumns_NullData_ReturnsEmptyString()
        {
            var result = VueTableDefaultViewComponent.GetDefaultVisibleColumns(null);
            Assert.Equal("", result);
        }

        [Fact]
        public void GetDefaultVisibleColumns_ReturnsPropertyNames()
        {
            var result = VueTableDefaultViewComponent.GetDefaultVisibleColumns(SampleData());

            Assert.Contains("'Id'", result);
            Assert.Contains("'Name'", result);
            Assert.Contains("'Active'", result);
        }

        [Fact]
        public void GetDefaultVisibleColumns_SkipColumns_ExcludesSpecifiedColumns()
        {
            var result = VueTableDefaultViewComponent.GetDefaultVisibleColumns(
                SampleData(), skipColumns: new[] { "Id" });

            Assert.DoesNotContain("Id", result);
            Assert.Contains("'Name'", result);
            Assert.Contains("'Active'", result);
        }

        [Fact]
        public void GetDefaultVisibleColumns_NoTrailingComma()
        {
            var result = VueTableDefaultViewComponent.GetDefaultVisibleColumns(SampleData());

            Assert.EndsWith("]", result);
            Assert.DoesNotContain(",]", result);
        }

        [Fact]
        public void GetDefaultVisibleColumns_SkipAllColumns_ReturnsEmptyArray()
        {
            var result = VueTableDefaultViewComponent.GetDefaultVisibleColumns(
                SampleData(), skipColumns: new[] { "Id", "Name", "Active" });

            Assert.Equal("[]", result);
        }

        [Fact]
        public void GetDefaultVisibleColumns_SkipsCollectionProperties()
        {
            var data = new List<object>
            {
                new RowWithCollection { Id = 1, Name = "Alice", Tags = new() { "a" } }
            };

            var result = VueTableDefaultViewComponent.GetDefaultVisibleColumns(data);

            Assert.Contains("'Id'", result);
            Assert.Contains("'Name'", result);
            Assert.DoesNotContain("Tags", result);
        }

        #endregion
    }
}
