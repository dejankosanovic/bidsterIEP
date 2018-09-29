using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace bidster.Controllers
{
    public class AdminController : Controller
    {

        private Models.BidsterContext _context;

        public AdminController()
        {
            _context = new Models.BidsterContext();
        }

        protected override void Dispose(bool disposing)
        {
            _context.Dispose();
            base.Dispose(disposing);
        }

        private bool hasAccess()
        {
            if (Session["userType"] == null || (string)Session["userType"] == "user")
            {
                return false;
            }
            return true;
        }


        [HttpGet]
        public ActionResult ChangeSystemParameters()
        {
            if (!hasAccess())
            {
                return new HttpNotFoundResult();
            }

            var parameters = getSystemParameters();

            if (parameters == null)
                return View();
            
            return View(parameters);
        }

        private Models.SystemParameters getSystemParameters()
        {
            if(_context.SystemParameters.Any())
                return _context.SystemParameters.First();
            return null;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangeSystemParameters(Models.SystemParameters parameters)
        {

            if (!hasAccess())
            {
                return new HttpNotFoundResult();
            }

            if (!ModelState.IsValid)
            {
                return View();
            }

            if(parameters.PlatinumPackageTokens < 1 || parameters.SilverPackageTokens < 1 || parameters.GoldPackageTokens < 1
                || parameters.DefaultAuctionDuration < 1 || parameters.NumberOfLatestAuctions < 1)
            {
                ModelState.AddModelError("", "Value of some field is zero or negative.");
                return View();
            }

            if(!(parameters.SilverPackageTokens < parameters.GoldPackageTokens && parameters.GoldPackageTokens < parameters.PlatinumPackageTokens))
            {
                ModelState.AddModelError("", "Number of tokens in Token Packages is invalid.");
                return View();
            }

            if(_context.SystemParameters.Any())
            {
                var oldParameters = _context.SystemParameters.First();

                oldParameters.NumberOfLatestAuctions = parameters.NumberOfLatestAuctions;
                oldParameters.DefaultAuctionDuration = parameters.DefaultAuctionDuration;
                oldParameters.SilverPackageTokens = parameters.SilverPackageTokens;
                oldParameters.GoldPackageTokens = parameters.GoldPackageTokens;
                oldParameters.PlatinumPackageTokens = parameters.PlatinumPackageTokens;
                _context.SaveChanges();
            }
            else
            {
                _context.SystemParameters.Add(parameters);
                _context.SaveChanges();
            }

            ViewData["success"] = true;

            return View();
        }

        public ActionResult Logout()
        {
            if(!hasAccess())
            {
                return new HttpNotFoundResult();
            }
            Session.Remove("userID");
            Session.Remove("userType");

            return Redirect("/Home/Index");
        }
    }
}