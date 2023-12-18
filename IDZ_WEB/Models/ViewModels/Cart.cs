using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace IDZ_WEB.Models.ViewModels
{
    public class Cart
    {
        public List<products> productsList { get; set; }

        public Guid purchaseID { get; set; }

        public Cart(List<products> prList, Guid purID)
        {
            productsList = prList;
            purchaseID = purID; 
        }
    }
}