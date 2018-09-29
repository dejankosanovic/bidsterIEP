using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.IO;
using PagedList;

namespace bidster.Controllers
{
    public class AuctionsController : Controller
    {
        private Models.BidsterContext _context;

        public AuctionsController()
        {
            _context = new Models.BidsterContext();
        }

        protected override void Dispose(bool disposing)
        {
            _context.Dispose();
            base.Dispose(disposing);
        }

        private bool hasAccess(string userType)
        {
            if(Session["userType"] != null && Session["userType"].ToString() == userType)
            {
                return true;
            }
            return false;
        }


        private bool IsImage(Stream stream)
        {
            stream.Seek(0, SeekOrigin.Begin);

            List<string> jpg = new List<string> { "FF", "D8" };
            List<string> bmp = new List<string> { "42", "4D" };
            List<string> png = new List<string> { "89", "50", "4E", "47", "0D", "0A", "1A", "0A" };
            List<List<string>> imgTypes = new List<List<string>> { jpg, bmp, png };

            List<string> bytesIterated = new List<string>();

            for (int i = 0; i < 8; i++)
            {
                string bit = stream.ReadByte().ToString("X2");
                bytesIterated.Add(bit);

                bool isImage = imgTypes.Any(img => !img.Except(bytesIterated).Any());
                if (isImage)
                {
                    return true;
                }
            }

            return false;
        }

        private System.Drawing.Image byteArrayToImage(byte[] byteArrayIn)
        {
            MemoryStream ms = new MemoryStream(byteArrayIn);
            System.Drawing.Image returnImage = System.Drawing.Image.FromStream(ms);
            return returnImage;
        }


        [HttpGet]
        public ActionResult CreateAuction()
        {

            if(!hasAccess("user"))
            {
                return new HttpNotFoundResult();
            }

            var parameters = _context.SystemParameters.First();

            var auction = new ViewModels.NewAuction
            {
                Duration = parameters.DefaultAuctionDuration
            };

            return View(auction);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateAuction(ViewModels.NewAuction auction)
        {
            if(!IsImage(auction.ImageFile.InputStream))
            {
                ModelState.AddModelError("ImageFile", "You have to provide the correct image file.");
                return View();
            }

            if(auction.Price < 1 || auction.Duration < 1)
            {
                ModelState.AddModelError("", "Value of some field is either zero or negative.");
                return View();
            }

            auction.ImageFile.InputStream.Position = 0;
            byte[] imageData = null;
            using (var binaryReader = new BinaryReader(auction.ImageFile.InputStream))
            {
                imageData = binaryReader.ReadBytes(auction.ImageFile.ContentLength);
            }
            var userID = Convert.ToInt32(Session["userID"].ToString());
            var user = _context.Users.Where(m => m.ID == userID).FirstOrDefault();

            var newAuction = new Models.Auction
            {
                AuctionImage = new Models.AuctionImage
                {
                    Image = imageData
                },
                DateCreated = System.DateTime.Now,
                Duration = auction.Duration,
                Name = auction.Name,
                Price = auction.Price,
                StartingPrice = auction.Price,
                TimeCreated = System.DateTime.Now.TimeOfDay,
                PostedByUser = user.ID,
                State = "Ready"
            };

            _context.Auctions.Add(newAuction);
            _context.SaveChanges();

            ViewData["success"] = true;
            return View();
        }
        
        public ActionResult Search(string priceRange, string search, string state, int? page)
        {

            ViewBag.state = String.IsNullOrEmpty(state) ? "" : state;
            ViewBag.search = String.IsNullOrEmpty(search) ? "" : search;
            ViewBag.priceRange = String.IsNullOrEmpty(priceRange) ? "" : priceRange;

            var auctions = getAuctions();

            

            if(_context.Auctions.Where(m => m.State != "Ready").Count() != 0)
            {
                ViewBag.MinPrice = _context.Auctions.Where(m => m.State != "Ready").Min(m => m.Price);
                ViewBag.MaxPrice = _context.Auctions.Where(m => m.State != "Ready").Max(m => m.Price);
            }

            if (!String.IsNullOrEmpty(state) && state != "all")
            {
                auctions = auctions.Where(m => m.State == state);
            }

            if(!String.IsNullOrEmpty(search))
            {
                char[] delimiters = { ' ' };
                var words = search.Split(delimiters);
                foreach(var word in words)
                {
                    auctions = auctions.Where(m => m.Name.ToLower().Contains(word.ToLower()));
                }
            }

            if(!String.IsNullOrEmpty(priceRange))
            {
                char[] delimiters = { ',' };
                var prices = priceRange.Split(delimiters);
                if(prices.Count() != 2)
                {
                    return new HttpNotFoundResult();
                }

                var isMinPriceNumeric = int.TryParse(prices.ElementAt(0), out int minPrice);
                var isMaxPriceNumeric = int.TryParse(prices.ElementAt(1), out int maxPrice);

                if(!(isMinPriceNumeric && isMaxPriceNumeric))
                {
                    return new HttpNotFoundResult();
                }
                auctions = auctions.Where(m => (minPrice <= m.Price && m.Price <= maxPrice));
            }

            int pageSize = _context.SystemParameters.First().NumberOfLatestAuctions;
            int pageNumber = (page ?? 1);

            return View(auctions.ToPagedList(pageNumber, pageSize));
        }

        private IEnumerable<Models.Auction> getAuctions()
        {

            var results = _context.Auctions.Where(m => m.State == "Opened" || m.State == "Finished").OrderByDescending(m => m.DateOpened).ThenByDescending(m => m.TimeOpened).ToList();

            return results;
        }

        public ActionResult GetImage(int id)
        {
            var imageData = _context.AuctionImages.Where(m => m.ID == id).FirstOrDefault().Image;

            if (System.Drawing.Imaging.ImageFormat.Jpeg.Equals(imageData))
            {
                return File(imageData, "image/jpg");
            }
            else if (System.Drawing.Imaging.ImageFormat.Png.Equals(imageData))
            {
                return File(imageData, "image/png");
            }
            else 
            {
                return File(imageData, "image/bmp");
            }
        }

        public ActionResult Ready()
        {

            if(!hasAccess("admin"))
            {
                return new HttpNotFoundResult();
            }

            var readyAuctions = getReadyAuctions();

            if(readyAuctions.Count() == 0)
            {
                ViewData["empty"] = true;
                return View();
            }

            return View(readyAuctions);
        }

        private IEnumerable<Models.Auction> getReadyAuctions()
        {
            var results = _context.Auctions.Where(m => m.State == "Ready").ToList();
            return results;
        }

        [HttpPost]
        public ActionResult Ready(FormCollection form)
        {

            if (!hasAccess("admin"))
            {
                return new HttpNotFoundResult();
            }

            var auctionID = form["id"];
            var open = _context.Auctions.Where(m => m.ID.ToString() == auctionID).First();

            if (form["button"].ToString() == "decline")
            {
                _context.AuctionImages.Remove(open.AuctionImage);
                _context.Auctions.Remove(open);
            }
            else
            {
                open.State = "Opened";
                open.TimeOpened = DateTime.Now.TimeOfDay;
                open.DateOpened = DateTime.Now;
            }
            _context.SaveChanges();

            var readyAuctions = getReadyAuctions();

            if (readyAuctions.Count() == 0)
            {
                ViewData["empty"] = true;
                return View();
            }

            return View(readyAuctions);
        }

        public ActionResult Details(int id)
        {

            var auction = getAuctions(id);

            if(auction == null || auction.State == "Ready")
            {
                ViewData["empty"] = true;
            }

            return View(auction);
        }

        private Models.Auction getAuctions(int id)
        {
            return _context.Auctions.Find(id);
        }
    }
}