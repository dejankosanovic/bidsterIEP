namespace bidster.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("Auction")]
    public partial class Auction
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Auction()
        {
            Bid = new HashSet<Bid>();
        }

        public int ID { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; }

        public long Duration { get; set; }

        public double StartingPrice { get; set; }

        public double Price { get; set; }

        [Column(TypeName = "date")]
        public DateTime DateCreated { get; set; }

        [Column(TypeName = "date")]
        public DateTime? DateCompleted { get; set; }

        [Column(TypeName = "date")]
        public DateTime? DateOpened { get; set; }

        public TimeSpan TimeCreated { get; set; }

        public TimeSpan? TimeCompleted { get; set; }

        public TimeSpan? TimeOpened { get; set; }

        [Required]
        [StringLength(50)]
        public string State { get; set; }

        public int? WonByUser { get; set; }

        public int PostedByUser { get; set; }

        public virtual AuctionImage AuctionImage { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Bid> Bid { get; set; }
    }
}
