using Microsoft.Data.SqlClient.DataClassification;
using Viper.Models.CTS;

namespace Viper.Areas.CTS.Models
{
	public class Session
	{
		public int SessionId { get; set; }
		public string? SessionType { get; set; }
		public string Title { get; set; } = null!;
		public int? TypeOrder { get; set; }
		public int? PaceOrder { get; set; }

		/* 
		public string? ReadingRequired { get; set; }
		public string? ReadingRecommended { get; set; }
		public string? ReadingSessionMaterial { get; set; }
		public string? KeyConcept { get; set; }
		public string? Equipment { get; set; }
		public string? Notes { get; set; }
		public string? Vocabulary { get; set; }
		public bool? Supplemental { get; set; }
		public int? SvmBlockId { get; set; }
		public int? CreatePersonId { get; set; }
		*/

		public List<Offering> Offerings { get; set; } = new();

		public Session() { }
		public Session(CourseSessionOffering cso)
		{
			SessionId = cso.SessionId;
			SessionType = cso.SessionType;
			Title = cso.Title;
			TypeOrder = cso.TypeOrder;
			PaceOrder = cso.PaceOrder;
			/*
			ReadingRecommended = cso.ReadingRecommended;
			ReadingRequired = cso.ReadingRequired;
			ReadingSessionMaterial = cso.ReadingSessionMaterial;
			KeyConcept = cso.KeyConcept;
			Equipment = cso.Equipment;
			Notes = cso.Notes;
			Vocabulary = cso.Vocabulary;
			Supplemental = cso.Supplemental == 1;
			SvmBlockId = cso.SvmBlockId;
			CreatePersonId = cso.CreatePersonId;
			*/
		}

	}
}
