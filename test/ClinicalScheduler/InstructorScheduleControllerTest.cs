using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Viper.Areas.ClinicalScheduler.Controllers;
using Viper.Areas.ClinicalScheduler.Models;
using Viper.Areas.ClinicalScheduler.Models.DTOs.Requests;
using Viper.Areas.ClinicalScheduler.Models.DTOs.Responses;
using Viper.Areas.ClinicalScheduler.Services;
using Viper.Areas.ClinicalScheduler.Validators;
using Viper.Models.ClinicalScheduler;

namespace Viper.test.ClinicalScheduler
{
    /// <summary>
    /// Unit tests for InstructorScheduleController
    /// </summary>
    public class InstructorScheduleControllerTest : ClinicalSchedulerTestBase
    {
        private readonly IScheduleEditService _mockScheduleEditService;
        private readonly IScheduleAuditService _mockAuditService;
        private readonly ISchedulePermissionService _mockPermissionService;
        private readonly IGradYearService _mockGradYearService;
        private readonly ILogger<InstructorScheduleController> _mockLogger;
        private readonly InstructorScheduleController _controller;

        public InstructorScheduleControllerTest()
        {
            _mockScheduleEditService = Substitute.For<IScheduleEditService>();
            _mockAuditService = Substitute.For<IScheduleAuditService>();
            _mockPermissionService = Substitute.For<ISchedulePermissionService>();
            _mockGradYearService = Substitute.For<IGradYearService>();
            _mockLogger = Substitute.For<ILogger<InstructorScheduleController>>();

            // Set up default mock for grad year service - use current year
            var currentYear = DateTime.Now.Year;
            _mockGradYearService.GetCurrentGradYearAsync()
                .Returns(currentYear);

            // Create mock user helper and validator
            var mockUserHelper = Substitute.For<IUserHelper>();
            var mockValidatorLogger = Substitute.For<ILogger<AddInstructorValidator>>();
            var validator = new AddInstructorValidator(mockValidatorLogger, _mockGradYearService);

            _controller = new InstructorScheduleController(
                _mockScheduleEditService,
                _mockAuditService,
                _mockPermissionService,
                mockUserHelper,
                _mockGradYearService,
                _mockLogger,
                validator
            );

            // Set up HTTP context for the controller
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
        }

        [Fact]
        public void AddInstructorRequest_ValidModel_PassesValidation()
        {
            // Arrange
            var request = new AddInstructorRequest
            {
                MothraId = "testuser",
                RotationId = 1,
                WeekIds = new[] { 1, 2, 3 },
                GradYear = DateTime.Now.Year,
                IsPrimaryEvaluator = false
            };

            // Act
            var validationResults = ValidateModel(request);

            // Assert
            Assert.Empty(validationResults);
        }

        [Fact]
        public void AddInstructorRequest_EmptyMothraId_FailsValidation()
        {
            // Arrange
            var request = new AddInstructorRequest
            {
                MothraId = "", // Invalid empty string
                RotationId = 1,
                WeekIds = new[] { 1, 2, 3 },
                GradYear = DateTime.Now.Year,
                IsPrimaryEvaluator = false
            };

            // Act
            var validationResults = ValidateModel(request);

            // Assert
            Assert.Contains(validationResults, v => v.ErrorMessage == "MothraId is required");
        }

        [Fact]
        public void AddInstructorRequest_DuplicateWeekIds_FailsValidation()
        {
            // Arrange
            var request = new AddInstructorRequest
            {
                MothraId = "testuser",
                RotationId = 1,
                WeekIds = new[] { 1, 2, 2 }, // Duplicate week ID
                GradYear = DateTime.Now.Year,
                IsPrimaryEvaluator = false
            };

            // Act
            var validationResults = ValidateModel(request);

            // Assert
            Assert.Contains(validationResults, v => v.ErrorMessage == "Week IDs must be unique");
        }

        [Fact]
        public void AddInstructorRequest_NegativeWeekIds_FailsValidation()
        {
            // Arrange
            var request = new AddInstructorRequest
            {
                MothraId = "testuser",
                RotationId = 1,
                WeekIds = new[] { 1, -1, 3 }, // Negative week ID
                GradYear = DateTime.Now.Year,
                IsPrimaryEvaluator = false
            };

            // Act
            var validationResults = ValidateModel(request);

            // Assert
            Assert.Contains(validationResults, v => v.ErrorMessage == "All week IDs must be greater than 0");
        }

        [Fact]
        public void SetPrimaryEvaluatorRequest_ValidModel_PassesValidation()
        {
            // Arrange
            var request = new SetPrimaryEvaluatorRequest
            {
                IsPrimary = true
            };

            // Act
            var validationResults = ValidateModel(request);

            // Assert
            Assert.Empty(validationResults);
        }

        [Fact]
        public void SetPrimaryEvaluatorRequest_NullIsPrimary_FailsValidation()
        {
            // Arrange
            var request = new SetPrimaryEvaluatorRequest
            {
                IsPrimary = null // Required field is null
            };

            // Act
            var validationResults = ValidateModel(request);

            // Assert
            Assert.Contains(validationResults, v => v.ErrorMessage == "IsPrimary flag is required");
        }

        [Fact]
        public async Task AddInstructor_ValidRequest_ReturnsOkWithResponse()
        {
            // Arrange
            var request = new AddInstructorRequest
            {
                MothraId = TestUserMothraId,
                RotationId = CardiologyRotationId,
                WeekIds = new[] { 1, 2 },
                GradYear = DateTime.Now.Year,
                IsPrimaryEvaluator = false
            };


            // Mock all the service dependencies
            _mockPermissionService.HasEditPermissionForRotationAsync(CardiologyRotationId, Arg.Any<CancellationToken>())
                .Returns(true);

            _mockScheduleEditService.GetOtherRotationSchedulesAsync(
                    request.MothraId,
                    request.WeekIds,
                    request.GradYear.Value,
                    request.RotationId.Value,
                    Arg.Any<CancellationToken>())
                .Returns(new List<InstructorSchedule>());

            _mockScheduleEditService.AddInstructorAsync(
                    request.MothraId,
                    request.RotationId.Value,
                    request.WeekIds,
                    request.GradYear.Value,
                    request.IsPrimaryEvaluator,
                    Arg.Any<CancellationToken>())
                .Returns(new List<InstructorSchedule>
                {
                    new() { InstructorScheduleId = 1, MothraId = TestUserMothraId, WeekId = 1, RotationId = CardiologyRotationId, Evaluator = false },
                    new() { InstructorScheduleId = 2, MothraId = TestUserMothraId, WeekId = 2, RotationId = CardiologyRotationId, Evaluator = false }
                });

            _mockScheduleEditService.CanRemoveInstructorAsync(
                    Arg.Any<int>(),
                    Arg.Any<CancellationToken>())
                .Returns(true);

            // Note: Audit service is not called directly by controller in this flow

            // Act
            var result = await _controller.AddInstructor(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);

            // OkObjectResult inherently has a 200 status code, so no need to check
            Assert.Equal(200, okResult.StatusCode ?? 200);
            var actualResponse = Assert.IsType<AddInstructorResponse>(okResult.Value);

            Assert.Equal(2, actualResponse.Schedules.Count);
            Assert.Null(actualResponse.WarningMessage);

            await _mockPermissionService.Received(1).HasEditPermissionForRotationAsync(CardiologyRotationId, Arg.Any<CancellationToken>());
            await _mockScheduleEditService.Received(1).AddInstructorAsync(
                request.MothraId,
                request.RotationId.Value,
                request.WeekIds,
                request.GradYear.Value,
                request.IsPrimaryEvaluator,
                Arg.Any<CancellationToken>());
            // Note: Audit logging might be handled within the service layer rather than directly by the controller
        }

        [Fact]
        public async Task AddInstructor_PermissionDenied_ReturnsForbid()
        {
            // Arrange
            var request = new AddInstructorRequest
            {
                MothraId = TestUserMothraId,
                RotationId = CardiologyRotationId,
                WeekIds = new[] { 1 },
                GradYear = DateTime.Now.Year,
                IsPrimaryEvaluator = false
            };

            // Mock permission service to deny access
            _mockPermissionService.HasEditPermissionForRotationAsync(CardiologyRotationId, Arg.Any<CancellationToken>())
                .Returns(false);

            // Act
            var result = await _controller.AddInstructor(request);

            // Assert
            Assert.IsType<ForbidResult>(result);

            // Verify permission was checked but no other services were called
            await _mockPermissionService.Received(1).HasEditPermissionForRotationAsync(CardiologyRotationId, Arg.Any<CancellationToken>());
            await _mockScheduleEditService.DidNotReceive().AddInstructorAsync(
                Arg.Any<string>(),
                Arg.Any<int>(),
                Arg.Any<int[]>(),
                Arg.Any<int>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task AddInstructor_InvalidRequest_ReturnsBadRequest()
        {
            // Arrange - Create invalid request (empty MothraId)
            var request = new AddInstructorRequest
            {
                MothraId = "", // Invalid - empty
                RotationId = CardiologyRotationId,
                WeekIds = new[] { 1 },
                GradYear = DateTime.Now.Year,
                IsPrimaryEvaluator = false
            };

            // Act
            var result = await _controller.AddInstructor(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var errorResponse = Assert.IsType<ErrorResponse>(badRequestResult.Value);

            Assert.NotNull(errorResponse.UserMessage);
            Assert.Contains("required", errorResponse.UserMessage, StringComparison.OrdinalIgnoreCase);

            // Verify no services were called due to validation failure
            await _mockPermissionService.DidNotReceive().HasEditPermissionForRotationAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
            await _mockScheduleEditService.DidNotReceive().AddInstructorAsync(
                Arg.Any<string>(),
                Arg.Any<int>(),
                Arg.Any<int[]>(),
                Arg.Any<int>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>());
        }

        /// <summary>
        /// Helper method to validate model using data annotations
        /// </summary>
        private static List<ValidationResult> ValidateModel(object model)
        {
            var validationResults = new List<ValidationResult>();
            var context = new ValidationContext(model, null, null);
            Validator.TryValidateObject(model, context, validationResults, true);

            // Also run IValidatableObject.Validate if the model implements it
            if (model is IValidatableObject validatableModel)
            {
                validationResults.AddRange(validatableModel.Validate(context));
            }

            return validationResults;
        }
    }
}
