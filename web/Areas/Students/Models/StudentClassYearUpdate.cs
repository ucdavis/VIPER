﻿namespace Viper.Areas.Students.Models
{
	public class StudentClassYearUpdate
	{
		public int StudentClassYearId { get; set; }
		public bool? Ross { get; set; }
		public int? LeftReason { get; set; }
		public int? LeftTerm { get; set; }
		public string? Comment { get; set; }
		public bool Active { get; set; }
	}
}