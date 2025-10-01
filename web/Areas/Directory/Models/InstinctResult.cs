using System;
using System.Collections.Generic;

namespace Viper.Areas.Directory.Models
{
    public class InstinctResult
    {
        public bool Valid { get; set; } = false;
        public string? Id { get; set; }
        public string? Initials { get; set; }
        public string? InstinctId { get; set; }
        public bool IsActive { get; set; }
        public bool IsProtected { get; set; }
        public string? PasswordExpiresAt { get; set; }
        public string? Status { get; set; }
        public string? Username { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
    }
}