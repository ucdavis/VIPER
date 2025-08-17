using Viper.Areas.ClinicalScheduler.Services;

namespace Viper.test.ClinicalScheduler
{
    public class EvaluationPolicyServiceTest
    {
        #region Test Data Classes

        /// <summary>
        /// Test implementation of IRotationWeekInfo for testing purposes
        /// </summary>
        private class TestRotationWeekInfo : IRotationWeekInfo
        {
            public int WeekNum { get; set; }
            public bool ExtendedRotation { get; set; }
            public int GradYear { get; set; }
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
                new() { WeekNum = 5, ExtendedRotation = false, GradYear = 2025 },
                new() { WeekNum = 6, ExtendedRotation = false, GradYear = 2025 }
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
                new() { WeekNum = 5, ExtendedRotation = false, GradYear = 2025 },
                new() { WeekNum = 6, ExtendedRotation = true, GradYear = 2025 } // Extended rotation
            };

            // Act
            var result = EvaluationPolicyService.RequiresPrimaryEvaluator(6, weeks);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region Single Grad Year Tests

        [Fact]
        public void RequiresPrimaryEvaluator_SingleWeekRotation_ReturnsTrue()
        {
            // Arrange
            var weeks = new List<TestRotationWeekInfo>
            {
                new() { WeekNum = 5, ExtendedRotation = false, GradYear = 2025 }
            };

            // Act
            var result = EvaluationPolicyService.RequiresPrimaryEvaluator(5, weeks);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void RequiresPrimaryEvaluator_MultiWeekRotation_LastWeekReturnsTrue()
        {
            // Arrange
            var weeks = new List<TestRotationWeekInfo>
            {
                new() { WeekNum = 5, ExtendedRotation = false, GradYear = 2025 },
                new() { WeekNum = 6, ExtendedRotation = false, GradYear = 2025 },
                new() { WeekNum = 7, ExtendedRotation = false, GradYear = 2025 }
            };

            // Act & Assert
            Assert.False(EvaluationPolicyService.RequiresPrimaryEvaluator(5, weeks)); // First week
            Assert.False(EvaluationPolicyService.RequiresPrimaryEvaluator(6, weeks)); // Middle week
            Assert.True(EvaluationPolicyService.RequiresPrimaryEvaluator(7, weeks));  // Last week
        }

        [Fact]
        public void RequiresPrimaryEvaluator_MultiWeekWithExtended_LastNonExtendedReturnsTrue()
        {
            // Arrange
            var weeks = new List<TestRotationWeekInfo>
            {
                new() { WeekNum = 5, ExtendedRotation = false, GradYear = 2025 },
                new() { WeekNum = 6, ExtendedRotation = false, GradYear = 2025 },
                new() { WeekNum = 7, ExtendedRotation = true, GradYear = 2025 }  // Extended
            };

            // Act & Assert
            Assert.False(EvaluationPolicyService.RequiresPrimaryEvaluator(5, weeks)); // First week
            Assert.True(EvaluationPolicyService.RequiresPrimaryEvaluator(6, weeks));  // Last non-extended week
            Assert.False(EvaluationPolicyService.RequiresPrimaryEvaluator(7, weeks)); // Extended week
        }

        [Fact]
        public void RequiresPrimaryEvaluator_AllExtendedWeeks_ReturnsFalse()
        {
            // Arrange
            var weeks = new List<TestRotationWeekInfo>
            {
                new() { WeekNum = 5, ExtendedRotation = true, GradYear = 2025 },
                new() { WeekNum = 6, ExtendedRotation = true, GradYear = 2025 }
            };

            // Act & Assert
            Assert.False(EvaluationPolicyService.RequiresPrimaryEvaluator(5, weeks));
            Assert.False(EvaluationPolicyService.RequiresPrimaryEvaluator(6, weeks));
        }

        #endregion

        #region Cross-GradYear Filtering Tests

        [Fact]
        public void RequiresPrimaryEvaluator_CrossGradYear_FiltersCorrectly()
        {
            // Arrange - Mix of 2024 and 2025 grad years with overlapping week numbers
            var weeks = new List<TestRotationWeekInfo>
            {
                // 2024 grad year weeks
                new() { WeekNum = 5, ExtendedRotation = false, GradYear = 2024 },
                new() { WeekNum = 6, ExtendedRotation = false, GradYear = 2024 },
                new() { WeekNum = 7, ExtendedRotation = false, GradYear = 2024 },
                
                // 2025 grad year weeks (overlapping week numbers)
                new() { WeekNum = 5, ExtendedRotation = false, GradYear = 2025 },
                new() { WeekNum = 6, ExtendedRotation = false, GradYear = 2025 }
            };

            // Act & Assert
            // When multiple weeks with the same number exist across grad years,
            // the method should return true if ANY of them require evaluation

            // Week 5: 
            // - 2024: First week (doesn't require evaluation)
            // - 2025: First week (doesn't require evaluation)
            // Result: false (no week 5 requires evaluation)
            Assert.False(EvaluationPolicyService.RequiresPrimaryEvaluator(5, weeks));

            // Week 6:
            // - 2024: Middle week (doesn't require evaluation, week 7 is last)
            // - 2025: Last week (DOES require evaluation)
            // Result: true (week 6 for 2025 requires evaluation)
            Assert.True(EvaluationPolicyService.RequiresPrimaryEvaluator(6, weeks));

            // Week 7:
            // - 2024: Last week (DOES require evaluation)
            // - 2025: No week 7 exists
            // Result: true (week 7 for 2024 requires evaluation)
            Assert.True(EvaluationPolicyService.RequiresPrimaryEvaluator(7, weeks));
        }

        [Fact]
        public void RequiresPrimaryEvaluator_CrossGradYear_2024_LastWeek()
        {
            // Arrange - Testing evaluation for 2024 grad year week 7
            var weeks = new List<TestRotationWeekInfo>
            {
                // 2024 grad year weeks
                new() { WeekNum = 5, ExtendedRotation = false, GradYear = 2024 },
                new() { WeekNum = 6, ExtendedRotation = false, GradYear = 2024 },
                new() { WeekNum = 7, ExtendedRotation = false, GradYear = 2024 }, // Should require evaluation
                
                // 2025 grad year weeks (should be ignored when evaluating 2024 weeks)
                new() { WeekNum = 5, ExtendedRotation = false, GradYear = 2025 },
                new() { WeekNum = 6, ExtendedRotation = false, GradYear = 2025 },
                new() { WeekNum = 8, ExtendedRotation = false, GradYear = 2025 } // Higher week number but different grad year
            };

            // Act
            var result = EvaluationPolicyService.RequiresPrimaryEvaluator(7, weeks);

            // Assert
            Assert.True(result); // Week 7 should require evaluation as it's the last week for 2024 grad year
        }

        [Fact]
        public void RequiresPrimaryEvaluator_CrossGradYear_2025_LastWeek()
        {
            // Arrange - Testing evaluation for 2025 grad year week 6
            var weeks = new List<TestRotationWeekInfo>
            {
                // 2024 grad year weeks (should be ignored when evaluating 2025 weeks)
                new() { WeekNum = 5, ExtendedRotation = false, GradYear = 2024 },
                new() { WeekNum = 6, ExtendedRotation = false, GradYear = 2024 },
                new() { WeekNum = 7, ExtendedRotation = false, GradYear = 2024 },
                
                // 2025 grad year weeks
                new() { WeekNum = 5, ExtendedRotation = false, GradYear = 2025 },
                new() { WeekNum = 6, ExtendedRotation = false, GradYear = 2025 } // Should require evaluation
            };

            // Act
            var result = EvaluationPolicyService.RequiresPrimaryEvaluator(6, weeks);

            // Assert
            Assert.True(result); // Week 6 should require evaluation as it's the last week for 2025 grad year
        }

        [Fact]
        public void RequiresPrimaryEvaluator_CrossGradYear_WithExtended()
        {
            // Arrange - Complex scenario with extended weeks across grad years
            var weeks = new List<TestRotationWeekInfo>
            {
                // 2024 grad year
                new() { WeekNum = 5, ExtendedRotation = false, GradYear = 2024 },
                new() { WeekNum = 6, ExtendedRotation = false, GradYear = 2024 },
                new() { WeekNum = 7, ExtendedRotation = true, GradYear = 2024 },  // Extended
                
                // 2025 grad year
                new() { WeekNum = 5, ExtendedRotation = false, GradYear = 2025 },
                new() { WeekNum = 6, ExtendedRotation = false, GradYear = 2025 },
                new() { WeekNum = 7, ExtendedRotation = false, GradYear = 2025 },
                new() { WeekNum = 8, ExtendedRotation = true, GradYear = 2025 }   // Extended
            };

            // Act & Assert
            // For 2024: Week 6 should require evaluation (last non-extended for 2024)
            Assert.True(EvaluationPolicyService.RequiresPrimaryEvaluator(6, weeks));

            // For 2025: Week 7 should require evaluation (last non-extended for 2025)
            Assert.True(EvaluationPolicyService.RequiresPrimaryEvaluator(7, weeks));

            // Extended weeks should not require evaluation
            Assert.False(EvaluationPolicyService.RequiresPrimaryEvaluator(7, weeks.Where(w => w.GradYear == 2024))); // Would be extended for 2024
            Assert.False(EvaluationPolicyService.RequiresPrimaryEvaluator(8, weeks)); // Extended for 2025
        }

        [Fact]
        public void RequiresPrimaryEvaluator_SameWeekNumbers_DifferentGradYears()
        {
            // Arrange - Identical week numbers across different grad years
            var weeks = new List<TestRotationWeekInfo>
            {
                // 2024 grad year - Week 5 is the only week
                new() { WeekNum = 5, ExtendedRotation = false, GradYear = 2024 },
                
                // 2025 grad year - Week 5 is also the only week
                new() { WeekNum = 5, ExtendedRotation = false, GradYear = 2025 }
            };

            // Act
            var result2024 = EvaluationPolicyService.RequiresPrimaryEvaluator(5, weeks);
            var result2025 = EvaluationPolicyService.RequiresPrimaryEvaluator(5, weeks);

            // Assert
            // Both should require evaluation since each is the last (and only) week for their respective grad year
            Assert.True(result2024);
            Assert.True(result2025);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void RequiresPrimaryEvaluator_OutOfOrderWeeks_HandlesCorrectly()
        {
            // Arrange - Weeks provided in non-sequential order
            var weeks = new List<TestRotationWeekInfo>
            {
                new() { WeekNum = 7, ExtendedRotation = false, GradYear = 2025 },
                new() { WeekNum = 5, ExtendedRotation = false, GradYear = 2025 },
                new() { WeekNum = 6, ExtendedRotation = false, GradYear = 2025 }
            };

            // Act & Assert
            Assert.False(EvaluationPolicyService.RequiresPrimaryEvaluator(5, weeks)); // First week
            Assert.False(EvaluationPolicyService.RequiresPrimaryEvaluator(6, weeks)); // Middle week
            Assert.True(EvaluationPolicyService.RequiresPrimaryEvaluator(7, weeks));  // Last week
        }

        [Fact]
        public void RequiresPrimaryEvaluator_GapsInWeekNumbers_HandlesCorrectly()
        {
            // Arrange - Non-consecutive week numbers
            var weeks = new List<TestRotationWeekInfo>
            {
                new() { WeekNum = 5, ExtendedRotation = false, GradYear = 2025 },
                new() { WeekNum = 8, ExtendedRotation = false, GradYear = 2025 },  // Gap: no weeks 6, 7
                new() { WeekNum = 10, ExtendedRotation = false, GradYear = 2025 }
            };

            // Act & Assert
            Assert.False(EvaluationPolicyService.RequiresPrimaryEvaluator(5, weeks));  // First week
            Assert.False(EvaluationPolicyService.RequiresPrimaryEvaluator(8, weeks));  // Middle week
            Assert.True(EvaluationPolicyService.RequiresPrimaryEvaluator(10, weeks));  // Last week
        }

        #endregion
    }
}