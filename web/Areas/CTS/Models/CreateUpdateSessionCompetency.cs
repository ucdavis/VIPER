﻿namespace Viper.Areas.CTS.Models
{
    public class CreateUpdateSessionCompetency
    {
        public int? SessionCompetencyId { get; set; }

        public int SessionId { get; set; }

        public int CompetencyId { get; set; }

        public List<int> LevelIds { get; set; } = new List<int>();

        public int? RoleId { get; set; }

        public int? Order { get; set; }
    }
}
