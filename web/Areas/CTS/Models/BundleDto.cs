﻿using Viper.Models.CTS;

namespace Viper.Areas.CTS.Models
{
    public class BundleDto
    {
        public int? BundleId { get; set; }
        public string Name { get; set; } = null!;
        public bool Clinical { get; set; }
        public bool Assessment { get; set; }
        public bool Milestone { get; set; }
        public int CompetencyCount { get; set; }

        //Only including roles here - other related objects can be found separately
        public IEnumerable<RoleDto> Roles{ get; set; } = new List<RoleDto>();
    }
}
