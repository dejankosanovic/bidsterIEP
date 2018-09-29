namespace bidster.Models
{
    using System;
    using System.Data.Entity;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Linq;

    public partial class BidsterContext : DbContext
    {
        public BidsterContext()
            : base("name=Bidster")
        {
        }

        public virtual DbSet<Auction> Auctions { get; set; }
        public virtual DbSet<AuctionImage> AuctionImages { get; set; }
        public virtual DbSet<Bid> Bids { get; set; }
        public virtual DbSet<Orders> Orders { get; set; }
        public virtual DbSet<SystemParameters> SystemParameters { get; set; }
        public virtual DbSet<User> Users { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Auction>()
                .Property(e => e.Name)
                .IsUnicode(false);

            modelBuilder.Entity<Auction>()
                .Property(e => e.State)
                .IsUnicode(false);

            modelBuilder.Entity<Auction>()
                .HasOptional(e => e.AuctionImage)
                .WithRequired(e => e.Auction);

            modelBuilder.Entity<Auction>()
                .HasMany(e => e.Bid)
                .WithRequired(e => e.Auction)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Orders>()
                .Property(e => e.State)
                .IsUnicode(false);

            modelBuilder.Entity<User>()
                .Property(e => e.FirstName)
                .IsUnicode(false);

            modelBuilder.Entity<User>()
                .Property(e => e.LastName)
                .IsUnicode(false);

            modelBuilder.Entity<User>()
                .Property(e => e.Email)
                .IsUnicode(false);

            modelBuilder.Entity<User>()
                .Property(e => e.PasswordHash)
                .IsUnicode(false);

            modelBuilder.Entity<User>()
                .Property(e => e.Salt)
                .IsUnicode(false);

            modelBuilder.Entity<User>()
                .Property(e => e.AccountType)
                .IsUnicode(false);

            modelBuilder.Entity<User>()
                .HasMany(e => e.Bid)
                .WithRequired(e => e.User)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<User>()
                .HasMany(e => e.Orders)
                .WithRequired(e => e.User)
                .WillCascadeOnDelete(false);
        }
    }
}
