using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.ComponentModel.DataAnnotations;
using Viper.Areas.ClinicalScheduler.Controllers;
using Viper.Areas.ClinicalScheduler.Models.DTOs.Requests;
using Viper.Areas.ClinicalScheduler.Services;
using Viper.Areas.ClinicalScheduler.Validators;

namespace Viper.test.ClinicalScheduler
{
    /// <summary>
    /// Unit tests for InstructorScheduleController
    /// </summary>
    public class InstructorScheduleControllerTest : ClinicalSchedulerTestBase
    {
        private readonly Mock<IScheduleEditService> _mockScheduleEditService;
        private readonly Mock<IScheduleAuditService> _mockAuditService;
        private readonly Mock<ISchedulePermissionService> _mockPermissionService;
        private readonly AddInstructorValidator _validator;
        private readonly Mock<IGradYearService> _mockGradYearService;
        private readonly Mock<ILogger<InstructorScheduleController>> _mockLogger;
        private readonly InstructorScheduleController _controller;

        public InstructorScheduleControllerTest()
        {
            _mockScheduleEditService = new Mock<IScheduleEditService>();
            _mockAuditService = new Mock<IScheduleAuditService>();
            _mockPermissionService = new Mock<ISchedulePermissionService>();
            _mockGradYearService = new Mock<IGradYearService>();
            _mockLogger = new Mock<ILogger<InstructorScheduleController>>();

            // Create real validator instance with mock logger
            var mockValidatorLogger = new Mock<ILogger<AddInstructorValidator>>();
            _validator = new AddInstructorValidator(mockValidatorLogger.Object);

            _controller = new InstructorScheduleController(
                _mockScheduleEditService.Object,
                _mockAuditService.Object,
                _mockPermissionService.Object,
                _validator,
                _mockGradYearService.Object,
                _mockLogger.Object
            );

            // Set up HTTP context for the controller
            _controller.ControllerContext = new ControllerContext()
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
                GradYear = 2024,
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
                GradYear = 2024,
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
                GradYear = 2024,
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
                GradYear = 2024,
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

        /// <summary>
        /// Helper method to validate model using data annotations
        /// </summary>
        private List<ValidationResult> ValidateModel(object model)
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
