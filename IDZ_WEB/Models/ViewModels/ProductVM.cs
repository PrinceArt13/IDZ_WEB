using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace IDZ_WEB.Models.ViewModels
{
    public class ProductVM
    {
        public Guid product_id { get; set; }
        public Guid manufacturer_id { get; set; }
        [Required]
        [DisplayName("Название товара")]
        public string name { get; set; }
        public Guid category_id { get; set; }
        [Required]
        [DisplayName("Цена")]
        public int price { get; set; }

    }
}