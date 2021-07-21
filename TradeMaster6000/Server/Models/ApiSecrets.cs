using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace TradeMaster6000.Server.Models
{
    public class ApiSecrets
    {
        [Required]
        [Display(Name = "Api Key")]
        public string ApiKey { get; set; } = null;
        [Required]
        [Display(Name = "App Secret")]
        public string AppSecret { get; set; } = null;
    }
}
