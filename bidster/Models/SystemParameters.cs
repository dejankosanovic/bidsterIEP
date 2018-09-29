namespace bidster.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public partial class SystemParameters
    {
        public int NumberOfLatestAuctions { get; set; }

        public long DefaultAuctionDuration { get; set; }

        public int SilverPackageTokens { get; set; }

        public int GoldPackageTokens { get; set; }

        public int PlatinumPackageTokens { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int ID { get; set; }
    }
}
