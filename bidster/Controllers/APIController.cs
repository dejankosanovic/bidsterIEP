using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace bidster.Controllers
{
    public class APIController : Controller
    {
        private Models.BidsterContext _context;

        public APIController()
        {
            _context = new Models.BidsterContext();
        }

        protected override void Dispose(bool disposing)
        {
            _context.Dispose();
            base.Dispose(disposing);
        }

        private IEnumerable<Models.Auction> getActiveAuctions()
        {
            return _context.Auctions.Where(m => m.State == "Opened").ToList();
        }


        // GET: API
        public ActionResult VerifyTransaction(string transactionid)
        {

            using(var transaction = _context.Database.BeginTransaction())
            {
                _context.Database.ExecuteSqlCommand("SELECT TOP 1 ID FROM [User] WITH (TABLOCKX)");

                var id = Convert.ToInt64(transactionid);

                var order = _context.Orders.Where(m => m.TransactionID == id).First();

                if(order.State == "COMPLETED")
                {
                    transaction.Commit();
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
                }

                var user = order.User;

                user.Tokens += order.Tokens;
                order.State = "COMPLETED";

                _context.SaveChanges();
                transaction.Commit();

                return new HttpStatusCodeResult(HttpStatusCode.OK);
            }
        }

        
        public ActionResult FinishedAuctions()
        {
            var auctions = getActiveAuctions();

            if(auctions.Count() != 0)
            {
                foreach (var auction in auctions)
                {
                    DateTime t1 = auction.DateOpened.Value.Add(auction.TimeOpened.Value.Add(new TimeSpan(0, 0, (int)auction.Duration)));
                    DateTime t2 = DateTime.Now;
                    if (DateTime.Compare(t1, t2) <= 0)
                    {
                        var bids = _context.Bids.Where(m => m.AuctionID == auction.ID).GroupBy(m => m.UserID).Select(g => g.OrderByDescending(m => m.Date).ThenByDescending(m => m.Time).FirstOrDefault()).ToList(); ;

                        if (bids.Count() != 0)
                        {
                            _context.Users.Where(m => m.ID == auction.PostedByUser).First().Tokens += auction.Price;
                            auction.WonByUser = _context.Bids.Where(m => m.AuctionID == auction.ID).OrderByDescending(m => m.Date).ThenByDescending(m => m.Time).First().UserID;

                            foreach (var bid in bids)
                            {
                                if (bid.UserID == auction.WonByUser)
                                {
                                    continue;
                                }
                                bid.User.Tokens += bid.Tokens;
                            }
                        }
                        auction.DateCompleted = t1.Date;
                        auction.TimeCompleted = t1.TimeOfDay;
                        auction.State = "Finished";
                        try
                        {
                            _context.SaveChanges();
                        }
                        catch(Exception e)
                        {
                            return Content(e.ToString());
                        }
                    }
                }
            }
            return new HttpStatusCodeResult(HttpStatusCode.OK);
        }
    }
}