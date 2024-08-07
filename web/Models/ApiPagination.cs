﻿namespace Viper.Models
{
    public class ApiPagination
    {
        public int Page { get; set; }
        public int PerPage { get; set; }
        public int Pages { get; set; }
        public int TotalRecords { get; set; }
        public string? Next { get; set; }
        public string? Previous { get; set; }
    }
}
