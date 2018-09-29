namespace bidster.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public partial class Orders
    {
        public int ID { get; set; }

        public int UserID { get; set; }

        public int Tokens { get; set; }

        public int Price { get; set; }

        [Required]
        [StringLength(9)]
        public string State { get; set; }

        public long? TransactionID { get; set; }

        public virtual User User { get; set; }
    }
}
