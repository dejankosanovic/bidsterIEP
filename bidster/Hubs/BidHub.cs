using System;
using System.Web;
using System.Linq;
using Microsoft.AspNet.SignalR;
namespace bidster
{
    public class BidHub : Hub
    {
        public void Send(int userID, float price, int auctionID)
        {

            if (price <= 0)
            {
                return;
            }

            using(var context = new Models.BidsterContext())
            {
                using (var transaction = context.Database.BeginTransaction())
                {

                    context.Database.ExecuteSqlCommand("SELECT TOP 1 ID FROM [User] WITH (TABLOCKX)");

                    var auction = context.Auctions.Find(auctionID);
                    var user = context.Users.Find(userID);
                    if (auction == null)
                    {
                        transaction.Commit();
                        return;
                    }
                    if (price <= auction.Price)
                    {
                        transaction.Commit();
                        return;
                    }
                    else
                    {
                        auction.Price = price;
                        if (auction.Bid.Count == 0)
                        {
                            if (user.Tokens < price)
                            {
                                transaction.Commit();
                                return;
                            }

                            user.Tokens = user.Tokens - price;
                        }
                        else if (auction.Bid.Where(m => m.UserID == user.ID).Count() == 0)
                        {
                            if (user.Tokens < price)
                            {
                                transaction.Commit();
                                return;
                            }
                            user.Tokens = user.Tokens - price;
                        }
                        else
                        {
                            var priceDiff = price - auction.Bid.Where(m => m.UserID == user.ID).OrderByDescending(m => m.Date).ThenByDescending(m => m.Time).First().Tokens;
                            if (user.Tokens < priceDiff)
                            {
                                transaction.Commit();
                                return;
                            }
                            user.Tokens = user.Tokens - priceDiff;
                        }
                    }
                    var bid = new Models.Bid
                    {
                        AuctionID = auctionID,
                        Date = DateTime.Now,
                        Time = DateTime.Now.TimeOfDay,
                        UserID = user.ID,
                        Tokens = price
                    };
                    context.Bids.Add(bid);
                    context.SaveChanges();
                    transaction.Commit();
                    Clients.All.addNewBid(user.Email, price, auctionID);
                }
            }
        }
    }
}