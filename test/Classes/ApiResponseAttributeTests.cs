using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using System.Text.Json;
using System.Text.Json.Serialization;
using Viper.Classes;

namespace Test.Classes
{
    public class ApiResponseAttributeTests
    {
        private readonly ApiResponseAttribute _attribute = new();

        private static ResultExecutingContext CreateContext(IActionResult result)
        {
            var httpContext = new DefaultHttpContext();
            var actionContext = new ActionContext(
                httpContext,
                new RouteData(),
                new ActionDescriptor());

            return new ResultExecutingContext(
                actionContext,
                new List<IFilterMetadata>(),
                result,
                controller: null!);
        }

        [Fact]
        public void OnResultExecuting_SetsDeclaredTypeToApiResponse()
        {
            // Arrange
            var data = new { Name = "Test" };
            var objectResult = new OkObjectResult(data);
            var context = CreateContext(objectResult);

            // Act
            _attribute.OnResultExecuting(context);

            // Assert
            Assert.Equal(typeof(ApiResponse), objectResult.DeclaredType);
        }

        [Fact]
        public void OnResultExecuting_WithJsonDerivedType_SerializesWithoutCastFailures()
        {
            // Arrange - type with JsonDerivedType attribute (like StudentAssessment)
            var derivedItem = new DerivedClass { Name = "Test", Extra = "ExtraData" };
            var objectResult = new OkObjectResult(derivedItem);
            var context = CreateContext(objectResult);

            // Act
            _attribute.OnResultExecuting(context);

            // Assert - DeclaredType must be ApiResponse to prevent serialization issues
            Assert.Equal(typeof(ApiResponse), objectResult.DeclaredType);

            // Verify the wrapped response can be serialized without cast failures
            var apiResponse = Assert.IsType<ApiResponse>(objectResult.Value);
            var json = JsonSerializer.Serialize(apiResponse);
            Assert.Contains("Test", json);
            Assert.Contains("ExtraData", json);
        }

        // Test types to simulate JsonDerivedType behavior (like StudentAssessment/StudentEpaAssessment)
        [JsonDerivedType(typeof(BaseClass), typeDiscriminator: "base")]
        [JsonDerivedType(typeof(DerivedClass), typeDiscriminator: "derived")]
        private class BaseClass
        {
            public string Name { get; set; } = string.Empty;
        }

        private class DerivedClass : BaseClass
        {
            public string Extra { get; set; } = string.Empty;
        }
    }
}
