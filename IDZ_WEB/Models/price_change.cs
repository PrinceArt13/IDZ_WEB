//------------------------------------------------------------------------------
// <auto-generated>
//     Этот код создан по шаблону.
//
//     Изменения, вносимые в этот файл вручную, могут привести к непредвиденной работе приложения.
//     Изменения, вносимые в этот файл вручную, будут перезаписаны при повторном создании кода.
// </auto-generated>
//------------------------------------------------------------------------------

namespace IDZ_WEB.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class price_change
    {
        public System.Guid price_id { get; set; }
        public System.Guid product_id { get; set; }
        public System.DateTime date_price_change { get; set; }
        public int price { get; set; }
    
        public virtual products products { get; set; }
    }
}
