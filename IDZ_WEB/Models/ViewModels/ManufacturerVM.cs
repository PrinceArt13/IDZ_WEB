using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace IDZ_WEB.Models.ViewModels
{
    public class ManufacturerVM
    {
        public Guid category_id { get; set; }
        [Required]
        [DisplayName("Название категории")]
        public string name { get; set; }
    }
}