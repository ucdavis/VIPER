using Viper.Areas.ClinicalScheduler.Services;

namespace Viper.test.ClinicalScheduler
{
    public class EvaluationPolicyServiceTest
    {
        private readonly EvaluationPolicyService _service;

        public EvaluationPolicyServiceTest()
        {
            _service = new EvaluationPolicyService();
        }

        #region Test Data Classes

        /// <summary>
        /// Test implementation of IRotationWeekInfo for testing purposes
        /// </summary>
        private class TestRotationWeekInfo : IRotationWeekInfo
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
            var result = _service.RequiresPrimaryEvaluator(1, null!);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void RequiresPrimaryEvaluator_EmptyRotationWeeks_ReturnsFalse()
        {
            // Act
            var result = _service.RequiresPrimaryEvaluator(1, new List<TestRotationWeekInfo>());

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
            var result = _service.RequiresPrimaryEvaluator(10, weeks);

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
            var result = _service.RequiresPrimaryEvaluator(6, weeks);

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
            var result = _service.RequiresPrimaryEvaluator(5, weeks, serviceWeekSize: 2, rotationClosed: true);

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
            var result = _service.RequiresPrimaryEvaluator(5, weeks, serviceWeekSize: 1);

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
            var result = _service.RequiresPrimaryEvaluator(5, weeks, serviceWeekSize: 1);

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
            var result = _service.RequiresPrimaryEvaluator(28, weeks, serviceWeekSize: 2);

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
            var result = _service.RequiresPrimaryEvaluator(29, weeks, serviceWeekSize: 2);

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
            var result = _service.RequiresPrimaryEvaluator(32, weeks, serviceWeekSize: 2);

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
            var result = _service.RequiresPrimaryEvaluator(36, weeks, serviceWeekSize: 2);

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
            var result = _service.RequiresPrimaryEvaluator(43, weeks, serviceWeekSize: 2);

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
        [InlineData(32, false)] // First week of block
        [InlineData(33, false)] // Extended week - no evaluation
        [InlineData(34, false)] // First week of block
        [InlineData(35, true)]  // Second week of block
        [InlineData(36, false)] // First week of block
        [InlineData(37, false)] // Extended week - no evaluation
        [InlineData(38, false)] // First week of block
        [InlineData(39, true)]  // Second week of block
        [InlineData(40, false)] // First week of block
        [InlineData(41, true)]  // Second week of block
        [InlineData(42, false)] // First week of block
        [InlineData(43, true)]  // Second week of block
        public void RequiresPrimaryEvaluator_Rotation603Year2026_CorrectEvaluation(int weekNum, bool shouldRequireEvaluator)
        {
            // Arrange - Recreate the rotation 603 year 2026 scenario
            // Note: This represents a single rotation schedule where weeks may overlap
            // but the logic should pick the right context based on the specific week being evaluated
            var weeks = new List<TestRotationWeekInfo>
            {
                // Block 1: Weeks 28-29 (regular 2-week)
                new() { WeekNum = 28, ExtendedRotation = false, StartWeek = true },
                new() { WeekNum = 29, ExtendedRotation = false, StartWeek = false },

                // Block 2: Weeks 30-31 (regular 2-week)
                new() { WeekNum = 30, ExtendedRotation = false, StartWeek = true },
                new() { WeekNum = 31, ExtendedRotation = false, StartWeek = false },

                // Block 3: Weeks 32-33 (2-week with week 33 extended)
                new() { WeekNum = 32, ExtendedRotation = false, StartWeek = true },
                new() { WeekNum = 33, ExtendedRotation = true, StartWeek = false },

                // Block 4: Weeks 34-35 (regular 2-week)
                new() { WeekNum = 34, ExtendedRotation = false, StartWeek = true },
                new() { WeekNum = 35, ExtendedRotation = false, StartWeek = false },

                // Block 5: Weeks 36-37 (2-week with week 37 extended)
                new() { WeekNum = 36, ExtendedRotation = false, StartWeek = true },
                new() { WeekNum = 37, ExtendedRotation = true, StartWeek = false },

                // Block 6: Weeks 38-39 (regular 2-week)
                new() { WeekNum = 38, ExtendedRotation = false, StartWeek = true },
                new() { WeekNum = 39, ExtendedRotation = false, StartWeek = false },

                // Block 7: Weeks 40-41 (regular 2-week)
                new() { WeekNum = 40, ExtendedRotation = false, StartWeek = true },
                new() { WeekNum = 41, ExtendedRotation = false, StartWeek = false },

                // Block 8: Weeks 42-43 (regular 2-week)
                new() { WeekNum = 42, ExtendedRotation = false, StartWeek = true },
                new() { WeekNum = 43, ExtendedRotation = false, StartWeek = false }
            };

            // Filter to only include the relevant weeks for the test
            var relevantWeeks = weeks.Where(w => w.WeekNum >= weekNum - 1 && w.WeekNum <= weekNum + 1).ToList();

            // Act
            var result = _service.RequiresPrimaryEvaluator(weekNum, relevantWeeks, serviceWeekSize: 2);

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
            var result = _service.RequiresPrimaryEvaluator(5, weeks);

            // Assert
            Assert.False(result); // Default behavior when no weekSize
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void RequiresPrimaryEvaluator_InvalidWeekSize_ReturnsFalse(int weekSize)
        {
            // Arrange
            var weeks = new List<TestRotationWeekInfo>
            {
                new() { WeekNum = 5, ExtendedRotation = false }
            };

            // Act
            var result = _service.RequiresPrimaryEvaluator(5, weeks, serviceWeekSize: weekSize);

            // Assert
            Assert.False(result); // Invalid weekSize (0, negative) defaults to false
        }


        [Fact]
        public void RequiresPrimaryEvaluator_WeekSize4_OnlyLastWeekRequiresEvaluation()
        {
            // Arrange - 4-week rotation blocks
            var weeks = new List<TestRotationWeekInfo>
            {
                new() { WeekNum = 10, ExtendedRotation = false, StartWeek = true },   // Week 1 of block
                new() { WeekNum = 11, ExtendedRotation = false, StartWeek = false },  // Week 2 of block
                new() { WeekNum = 12, ExtendedRotation = false, StartWeek = false },  // Week 3 of block
                new() { WeekNum = 13, ExtendedRotation = false, StartWeek = false },  // Week 4 of block (last)
                new() { WeekNum = 14, ExtendedRotation = false, StartWeek = true }    // Week 1 of next block
            };

            // Act & Assert - For 4-week blocks, only the last week (4th) should require evaluation
            Assert.False(_service.RequiresPrimaryEvaluator(10, weeks, serviceWeekSize: 4)); // First week
            Assert.False(_service.RequiresPrimaryEvaluator(11, weeks, serviceWeekSize: 4)); // Second week
            Assert.False(_service.RequiresPrimaryEvaluator(12, weeks, serviceWeekSize: 4)); // Third week
            Assert.True(_service.RequiresPrimaryEvaluator(13, weeks, serviceWeekSize: 4));  // Fourth week (last)
        }

        #endregion
    }
}
