using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using NSubstitute;
using System.Net;
using Viper.Classes;

namespace Test.Classes
{
    public class CustomAntiforgeryFilterTests
    {
        private readonly IAntiforgery _mockAntiforgery;
        private readonly CustomAntiforgeryFilter _filter;

        public CustomAntiforgeryFilterTests()
        {
            _mockAntiforgery = Substitute.For<IAntiforgery>();
            _filter = new CustomAntiforgeryFilter(_mockAntiforgery);
        }

        private static AuthorizationFilterContext CreateContext(string method)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Method = method;

            var actionContext = new ActionContext(
                httpContext,
                new RouteData(),
                new ActionDescriptor());

            return new AuthorizationFilterContext(
                actionContext,
                new List<IFilterMetadata>());
        }

        [Theory]
        [InlineData("GET")]
        [InlineData("HEAD")]
        [InlineData("OPTIONS")]
        [InlineData("TRACE")]
        public async Task OnAuthorizationAsync_SafeHttpMethods_SkipsValidation(string method)
        {
            // Arrange
            var context = CreateContext(method);

            // Act
            await _filter.OnAuthorizationAsync(context);

            // Assert
            Assert.Null(context.Result);
            await _mockAntiforgery.DidNotReceive().ValidateRequestAsync(Arg.Any<HttpContext>());
        }

        [Theory]
        [InlineData("POST")]
        [InlineData("PUT")]
        [InlineData("PATCH")]
        [InlineData("DELETE")]
        public async Task OnAuthorizationAsync_UnsafeMethods_ValidatesToken(string method)
        {
            // Arrange
            var context = CreateContext(method);
            _mockAntiforgery
                .ValidateRequestAsync(Arg.Any<HttpContext>())
                .Returns(Task.CompletedTask);

            // Act
            await _filter.OnAuthorizationAsync(context);

            // Assert
            Assert.Null(context.Result);
            await _mockAntiforgery.Received(1).ValidateRequestAsync(Arg.Any<HttpContext>());
        }

        [Fact]
        public async Task OnAuthorizationAsync_InvalidToken_ReturnsBadRequestWithApiResponse()
        {
            // Arrange
            var context = CreateContext("POST");
            _mockAntiforgery
                .ValidateRequestAsync(Arg.Any<HttpContext>())
                .Returns(Task.FromException(new AntiforgeryValidationException("Token invalid")));

            // Act
            await _filter.OnAuthorizationAsync(context);

            // Assert
            Assert.NotNull(context.Result);
            var objectResult = Assert.IsType<ObjectResult>(context.Result);
            Assert.Equal((int)HttpStatusCode.BadRequest, objectResult.StatusCode);

            var apiResponse = Assert.IsType<ApiResponse>(objectResult.Value);
            Assert.False(apiResponse.Success);
            Assert.Equal((int)HttpStatusCode.BadRequest, apiResponse.StatusCode);
            Assert.Contains("Antiforgery", apiResponse.ErrorMessage);
            Assert.NotNull(apiResponse.CorrelationId);
            Assert.NotEmpty(apiResponse.CorrelationId);
        }

        [Fact]
        public async Task OnAuthorizationAsync_ValidToken_AllowsRequest()
        {
            // Arrange
            var context = CreateContext("POST");
            _mockAntiforgery
                .ValidateRequestAsync(Arg.Any<HttpContext>())
                .Returns(Task.CompletedTask);

            // Act
            await _filter.OnAuthorizationAsync(context);

            // Assert
            Assert.Null(context.Result);
        }

        [Fact]
        public void Order_ReturnsExpectedValue()
        {
            // Assert
            Assert.Equal(1000, _filter.Order);
        }

        [Fact]
        public async Task OnAuthorizationAsync_IgnoreAntiforgeryTokenAttribute_SkipsValidation()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Method = "POST";

            var endpoint = new Endpoint(
                requestDelegate: null,
                metadata: new EndpointMetadataCollection(new IgnoreAntiforgeryTokenAttribute()),
                displayName: "Test endpoint");
            httpContext.SetEndpoint(endpoint);

            var actionContext = new ActionContext(
                httpContext,
                new RouteData(),
                new ActionDescriptor());

            var context = new AuthorizationFilterContext(
                actionContext,
                []);

            // Act
            await _filter.OnAuthorizationAsync(context);

            // Assert
            Assert.Null(context.Result);
            await _mockAntiforgery.DidNotReceive().ValidateRequestAsync(Arg.Any<HttpContext>());
        }
    }
}
