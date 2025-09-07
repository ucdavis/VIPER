using Viper.Areas.ClinicalScheduler.Services;

namespace Viper.test.ClinicalScheduler
{
    public class EvaluationPolicyServiceTest
    {
        #region Test Data Classes

        /// <summary>
        /// Test implementation of IRotationWeekInfo for testing purposes
        /// </summary>
        private class TestRotationWeekInfo : EvaluationPolicyService.IRotationWeekInfo
        {
            public int WeekNum { get; set; }
            public bool ExtendedRotation { get; set; }
            public bool StartWeek { get; set; }
        }

        #endregion

        #region Basic Functionality Tests

        [Fact]
        public void RequiresPrimaryEvaluator_NullRotationWeeks_ReturnsFalse()
        {
            // Act
            var result = EvaluationPolicyService.RequiresPrimaryEvaluator(1, null!);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void RequiresPrimaryEvaluator_EmptyRotationWeeks_ReturnsFalse()
        {
            // Act
            var result = EvaluationPolicyService.RequiresPrimaryEvaluator(1, new List<TestRotationWeekInfo>());

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void RequiresPrimaryEvaluator_WeekNotFound_ReturnsFalse()
        {
            // Arrange
            var weeks = new List<TestRotationWeekInfo>
            {
                new() { WeekNum = 5, ExtendedRotation = false },
                new() { WeekNum = 6, ExtendedRotation = false }
            };

            // Act
            var result = EvaluationPolicyService.RequiresPrimaryEvaluator(10, weeks);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void RequiresPrimaryEvaluator_ExtendedRotationWeek_ReturnsFalse()
        {
            // Arrange
            var weeks = new List<TestRotationWeekInfo>
            {
                new() { WeekNum = 5, ExtendedRotation = false },
                new() { WeekNum = 6, ExtendedRotation = true } // Extended rotation
            };

            // Act
            var result = EvaluationPolicyService.RequiresPrimaryEvaluator(6, weeks);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void RequiresPrimaryEvaluator_RotationClosed_ReturnsFalse()
        {
            // Arrange
            var weeks = new List<TestRotationWeekInfo>
            {
                new() { WeekNum = 5, ExtendedRotation = false, StartWeek = false }
            };

            // Act - Pass rotationClosed = true
            var result = EvaluationPolicyService.RequiresPrimaryEvaluator(5, weeks, serviceWeekSize: 2, rotationClosed: true);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region WeekSize = 1 Tests

        [Fact]
        public void RequiresPrimaryEvaluator_WeekSize1_NonExtendedWeek_ReturnsTrue()
        {
            // Arrange
            var weeks = new List<TestRotationWeekInfo>
            {
                new() { WeekNum = 5, ExtendedRotation = false }
            };

            // Act
            var result = EvaluationPolicyService.RequiresPrimaryEvaluator(5, weeks, serviceWeekSize: 1);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void RequiresPrimaryEvaluator_WeekSize1_ExtendedWeek_ReturnsFalse()
        {
            // Arrange
            var weeks = new List<TestRotationWeekInfo>
            {
                new() { WeekNum = 5, ExtendedRotation = true }
            };

            // Act
            var result = EvaluationPolicyService.RequiresPrimaryEvaluator(5, weeks, serviceWeekSize: 1);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region WeekSize = 2 Tests

        [Fact]
        public void RequiresPrimaryEvaluator_WeekSize2_StartWeek_ReturnsFalse()
        {
            // Arrange - First week of a 2-week block
            var weeks = new List<TestRotationWeekInfo>
            {
                new() { WeekNum = 28, ExtendedRotation = false, StartWeek = true },
                new() { WeekNum = 29, ExtendedRotation = false, StartWeek = false }
            };

            // Act
            var result = EvaluationPolicyService.RequiresPrimaryEvaluator(28, weeks, serviceWeekSize: 2);

            // Assert
            Assert.False(result); // First week doesn't need evaluator
        }

        [Fact]
        public void RequiresPrimaryEvaluator_WeekSize2_SecondWeek_ReturnsTrue()
        {
            // Arrange - Second week of a 2-week block
            var weeks = new List<TestRotationWeekInfo>
            {
                new() { WeekNum = 28, ExtendedRotation = false, StartWeek = true },
                new() { WeekNum = 29, ExtendedRotation = false, StartWeek = false }
            };

            // Act
            var result = EvaluationPolicyService.RequiresPrimaryEvaluator(29, weeks, serviceWeekSize: 2);

            // Assert
            Assert.True(result); // Second week needs evaluator
        }

        [Fact]
        public void RequiresPrimaryEvaluator_WeekSize2_ThreeWeekRotationWithExtended_ReturnsFalse()
        {
            // Arrange - 3-week rotation where week 3 is extended
            var weeks = new List<TestRotationWeekInfo>
            {
                new() { WeekNum = 31, ExtendedRotation = false, StartWeek = true },
                new() { WeekNum = 32, ExtendedRotation = false, StartWeek = false },
                new() { WeekNum = 33, ExtendedRotation = true, StartWeek = false } // Extended week 3
            };

            // Act - Check week 32 (would normally be evaluation week, but week 33 is extended)
            var result = EvaluationPolicyService.RequiresPrimaryEvaluator(32, weeks, serviceWeekSize: 2);

            // Assert
            Assert.False(result); // No evaluation needed because week 3 is extended
        }

        [Fact]
        public void RequiresPrimaryEvaluator_WeekSize2_RegularTwoWeekBlock_SecondWeekTrue()
        {
            // Arrange - Regular 2-week block
            var weeks = new List<TestRotationWeekInfo>
            {
                new() { WeekNum = 35, ExtendedRotation = false, StartWeek = true },
                new() { WeekNum = 36, ExtendedRotation = false, StartWeek = false },
                new() { WeekNum = 37, ExtendedRotation = false, StartWeek = true } // Next block starts
            };

            // Act - Check week 36 (second week of block)
            var result = EvaluationPolicyService.RequiresPrimaryEvaluator(36, weeks, serviceWeekSize: 2);

            // Assert
            Assert.True(result); // Second week needs evaluator
        }

        [Fact]
        public void RequiresPrimaryEvaluator_WeekSize2_LastWeekOfYear_ReturnsTrue()
        {
            // Arrange - Last week with no next week
            var weeks = new List<TestRotationWeekInfo>
            {
                new() { WeekNum = 42, ExtendedRotation = false, StartWeek = true },
                new() { WeekNum = 43, ExtendedRotation = false, StartWeek = false }
            };

            // Act - Check week 43 (last week, no next week)
            var result = EvaluationPolicyService.RequiresPrimaryEvaluator(43, weeks, serviceWeekSize: 2);

            // Assert
            Assert.True(result); // Last week needs evaluator
        }

        #endregion

        #region Rotation 603 Specific Tests

        [Theory]
        [InlineData(28, false)] // First week of block
        [InlineData(29, true)]  // Second week of block
        [InlineData(30, false)] // First week of block
        [InlineData(31, true)]  // Second week of block
        [InlineData(32, false)] // Week 2 of 3-week block with extended week 3
        [InlineData(33, false)] // First week of new block
        [InlineData(34, true)]  // Second week of block
        [InlineData(35, false)] // First week of block
        [InlineData(36, true)]  // Second week of block
        [InlineData(37, false)] // Week 2 of 3-week block with extended week 3
        [InlineData(38, false)] // First week of new block
        [InlineData(39, true)]  // Second week of block
        [InlineData(40, false)] // First week of block
        [InlineData(41, true)]  // Second week of block
        [InlineData(42, false)] // First week of block
        [InlineData(43, true)]  // Second week of block
        public void RequiresPrimaryEvaluator_Rotation603Year2026_CorrectEvaluation(int weekNum, bool shouldRequireEvaluator)
        {
            // Arrange - Recreate the rotation 603 year 2026 scenario
            var weeks = new List<TestRotationWeekInfo>
            {
                // Block 1: Weeks 28-29 (regular 2-week)
                new() { WeekNum = 28, ExtendedRotation = false, StartWeek = true },
                new() { WeekNum = 29, ExtendedRotation = false, StartWeek = false },
                
                // Block 2: Weeks 30-31 (regular 2-week)
                new() { WeekNum = 30, ExtendedRotation = false, StartWeek = true },
                new() { WeekNum = 31, ExtendedRotation = false, StartWeek = false },
                
                // Block 3: Weeks 31-33 (3-week with extended)
                new() { WeekNum = 31, ExtendedRotation = false, StartWeek = true },
                new() { WeekNum = 32, ExtendedRotation = false, StartWeek = false },
                new() { WeekNum = 33, ExtendedRotation = true, StartWeek = false },
                
                // Block 4: Weeks 33-34 (regular 2-week)
                new() { WeekNum = 33, ExtendedRotation = false, StartWeek = true },
                new() { WeekNum = 34, ExtendedRotation = false, StartWeek = false },
                
                // Block 5: Weeks 35-36 (regular 2-week)
                new() { WeekNum = 35, ExtendedRotation = false, StartWeek = true },
                new() { WeekNum = 36, ExtendedRotation = false, StartWeek = false },
                
                // Block 6: Weeks 36-38 (3-week with extended)
                new() { WeekNum = 36, ExtendedRotation = false, StartWeek = true },
                new() { WeekNum = 37, ExtendedRotation = false, StartWeek = false },
                new() { WeekNum = 38, ExtendedRotation = true, StartWeek = false },
                
                // Block 7: Weeks 38-39 (regular 2-week)
                new() { WeekNum = 38, ExtendedRotation = false, StartWeek = true },
                new() { WeekNum = 39, ExtendedRotation = false, StartWeek = false },
                
                // Block 8: Weeks 40-41 (regular 2-week)
                new() { WeekNum = 40, ExtendedRotation = false, StartWeek = true },
                new() { WeekNum = 41, ExtendedRotation = false, StartWeek = false },
                
                // Block 9: Weeks 42-43 (regular 2-week)
                new() { WeekNum = 42, ExtendedRotation = false, StartWeek = true },
                new() { WeekNum = 43, ExtendedRotation = false, StartWeek = false }
            };

            // Filter to only include the relevant weeks for the test
            var relevantWeeks = weeks.Where(w => w.WeekNum >= weekNum - 1 && w.WeekNum <= weekNum + 1).ToList();

            // Act
            var result = EvaluationPolicyService.RequiresPrimaryEvaluator(weekNum, relevantWeeks, serviceWeekSize: 2);

            // Assert
            Assert.Equal(shouldRequireEvaluator, result);
        }

        #endregion

        #region No WeekSize Configuration Tests

        [Fact]
        public void RequiresPrimaryEvaluator_NoWeekSize_ReturnsFalse()
        {
            // Arrange
            var weeks = new List<TestRotationWeekInfo>
            {
                new() { WeekNum = 5, ExtendedRotation = false }
            };

            // Act - No serviceWeekSize provided
            var result = EvaluationPolicyService.RequiresPrimaryEvaluator(5, weeks);

            // Assert
            Assert.False(result); // Default behavior when no weekSize
        }

        [Fact]
        public void RequiresPrimaryEvaluator_WeekSizeZero_ReturnsFalse()
        {
            // Arrange
            var weeks = new List<TestRotationWeekInfo>
            {
                new() { WeekNum = 5, ExtendedRotation = false }
            };

            // Act
            var result = EvaluationPolicyService.RequiresPrimaryEvaluator(5, weeks, serviceWeekSize: 0);

            // Assert
            Assert.False(result); // Invalid weekSize defaults to false
        }

        [Fact]
        public void RequiresPrimaryEvaluator_WeekSizeOtherValue_ReturnsFalse()
        {
            // Arrange
            var weeks = new List<TestRotationWeekInfo>
            {
                new() { WeekNum = 5, ExtendedRotation = false }
            };

            // Act - WeekSize = 3 (not 1 or 2)
            var result = EvaluationPolicyService.RequiresPrimaryEvaluator(5, weeks, serviceWeekSize: 3);

            // Assert
            Assert.False(result); // Unsupported weekSize defaults to false
        }

        #endregion
    }
}
