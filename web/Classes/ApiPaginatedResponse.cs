﻿using Viper.Models;

namespace Viper.Classes
{
    public class ApiPaginatedResponse
    {
        public IEnumerable<object> Data { get; set; } = new List<object>();
        public int TotalRecords { get; set; } = 0;
        public ApiPagination Pagination { get; set; }

        public ApiPaginatedResponse(IEnumerable<object> data, int totalRecords, ApiPagination? pagination)
        {
            Data = data;
            TotalRecords = totalRecords;
            Pagination = pagination;
        }
    }
}
