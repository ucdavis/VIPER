﻿using System.ComponentModel.DataAnnotations.Schema;
using Viper.Models.VIPER;

namespace Viper.Models.Students
{
    public class StudentClassYear
    {
        public int StudentClassYearId { get; set; }
        public int PersonId { get; set; }
        public int ClassYear { get; set; }
        public bool Active { get; set; }
        public bool Graduated { get; set; }
        public bool Ross { get; set; }
        public int? LeftTerm { get; set; }
        public int? LeftReason { get; set; }
        public DateTime Added { get; set; }
        public int? AddedBy { get; set; }
        public DateTime? Updated { get; set; }
        public int? UpdatedBy { get; set; }
        public string? Comment { get; set; }

        public virtual Person? Student { get; set; }
        public virtual ClassYearLeftReason? ClassYearLeftReason { get; set; }
        public virtual Person? AddedByPerson { get; set; }
        public virtual Person? UpdatedByPerson { get; set; }

        [NotMapped]
        public string? LeftReasonText
        {
            get
            {
                return ClassYearLeftReason?.Reason;
            }
        }
    }
}
