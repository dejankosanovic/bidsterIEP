using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Net.Http;
using Newtonsoft.Json;
using System.Text;
using System.Security.Cryptography;

namespace bidster.Controllers
{
    public class UserController : Controller
    {

        private Models.BidsterContext _context;

        public UserController()
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
            if (Session["userType"] == null || (string)Session["userType"] == "admin")
            {
                return false;
            }
            return true;
        }

        private Models.User getUser(int userID)
        {
            var user = _context.Users.Where(m => m.ID == userID).First();
            return user;
        }

        // GET: User
        public ActionResult MyProfile()
        {
            if(!hasAccess())
            {
                return new HttpNotFoundResult();
            }
            int userID = (int)Session["userID"];
            var user = getUser(userID);

            if(user == null)
            {
                return View();
            }

            var userView = new ViewModels.ChangeUser
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Tokens = user.Tokens
            };

            return View(userView);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult MyProfile(ViewModels.ChangeUser changeUser)
        {
            
            if(!hasAccess())
            {
                return new HttpNotFoundResult();
            }

            if(!ModelState.IsValid)
            {
                return View();
            }

            int userID = (int)Session["userID"];
            var user = getUser(userID);

            user.FirstName = changeUser.FirstName;
            user.LastName = changeUser.LastName;
            user.Email = changeUser.Email;
            _context.SaveChanges();

            return View();
        }

        private string hashPassword(string password, string salt)
        {
            var bytes = Encoding.UTF8.GetBytes(password + salt);
            SHA256Managed sha256 = new SHA256Managed();
            var hash = sha256.ComputeHash(bytes);
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }

        public ActionResult PasswordChange()
        {
            if (!hasAccess())
            {
                return new HttpNotFoundResult();
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult PasswordChange(ViewModels.PasswordChange passwordChange)
        {
            if(!hasAccess())
            {
                return new HttpNotFoundResult();
            }

            if(!ModelState.IsValid)
            {
                return View();
            }
            
            int userID = (int)Session["userID"];
            var user = _context.Users.Where(m => m.ID == userID).First();
            var salt = user.Salt;
            var newPassword = hashPassword(passwordChange.NewPassword, salt);

            if(hashPassword(passwordChange.OldPassword, salt) != user.PasswordHash)
            {
                ModelState.AddModelError("OldPassword", "Incorrect Old Password.");
                return View();
            }

            if(newPassword != hashPassword(passwordChange.ConfirmPassword, salt))
            {
                ModelState.AddModelError("ConfirmPassword", "New Password and Confirmation Password are different.");
                return View();
            }

            user.PasswordHash = newPassword;
            _context.SaveChanges();

            ViewData["success"] = true;

            return View();
        }

        public ActionResult Logout()
        {
            if (!hasAccess())
            {
                return new HttpNotFoundResult();
            }
            Session.Remove("userID");
            Session.Remove("userType");

            return Redirect("/Home/Index");
        }

        private void setSelectOptions()
        {
            var parameters = _context.SystemParameters.First();

            ViewData["silver"] = parameters.SilverPackageTokens;
            ViewData["gold"] = parameters.GoldPackageTokens;
            ViewData["platinum"] = parameters.PlatinumPackageTokens;
        }

        [HttpGet]
        public ActionResult BuyTokens()
        {
            if(!hasAccess())
            {
                return new HttpNotFoundResult();
            }

            setSelectOptions();

            return View();
        }

        private string countryCode = "381";
        private string apiKey = "cb2d1ab66435c578b9d35a383359c643";
        private string URI = "https://stage.centili.com/api/payment/1_4/transaction";
        private HttpResponseMessage sendPaymentRequest(int price, string mobileNumber)
        {
            var msisdn = countryCode + mobileNumber.Substring(1, mobileNumber.Length - 1);

            string data = JsonConvert.SerializeObject(new
            {
                apikey = apiKey,
                clientid = Session["userID"].ToString(),
                msisdn = msisdn,
                price = 10
            });
            HttpClient client = new HttpClient();
            HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, URI);
            requestMessage.Content = new StringContent(data, System.Text.Encoding.UTF8, "application/json");
            requestMessage.Headers.Add("Accept", "application/json");
            return client.SendAsync(requestMessage).GetAwaiter().GetResult();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult BuyTokens(FormCollection form)
        {
            if (!hasAccess())
            {
                return new HttpNotFoundResult();
            }

            if (form.AllKeys.Contains("pin"))
            {
                var pin = form["pin"];
                var transactionID = form["transactionID"];

                var newURI = URI + "/" + transactionID + "/" + pin;

                string data = JsonConvert.SerializeObject(new
                {
                    username = "dejan_kosanovic",
                    password = "dejan123"
                });

                HttpClient client = new HttpClient();
                HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, newURI);
                requestMessage.Content = new StringContent(data, System.Text.Encoding.UTF8, "application/json");
                HttpResponseMessage response = client.SendAsync(requestMessage).GetAwaiter().GetResult();
                var jsonData = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                var values = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonData);

                if (response.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    ViewData["pin_error"] = "Invalid PIN";
                    ViewData["pin"] = true;
                    ViewData["transactionID"] = form["transactionID"];
                    return View();
                }
                else
                {
                    return Redirect("/User/MyProfile");
                }
            }
            else
            {
                var parameters = _context.SystemParameters.First();

                if (String.IsNullOrEmpty(form["mobile"].ToString()))
                {
                    ViewData["error"] = "Mobile is empty";
                    return View();
                }
                int price;
                var mobileNumber = form["mobile"].ToString();
                HttpResponseMessage response;
                //POSTback to Centili web api!
                if (form["package"] == "silver")
                {
                    price = parameters.SilverPackageTokens;
                    response = sendPaymentRequest(parameters.SilverPackageTokens, mobileNumber);
                }
                else if (form["package"] == "gold")
                {
                    price = parameters.GoldPackageTokens;
                    response = sendPaymentRequest(parameters.GoldPackageTokens, mobileNumber);
                }
                else
                {
                    price = parameters.PlatinumPackageTokens;
                    response = sendPaymentRequest(parameters.PlatinumPackageTokens, mobileNumber);
                }

                var jsonData = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                var values = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonData);

                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    ViewData["pin"] = true;
                    ViewData["transactionID"] = values["transactionId"];
                    var id = Convert.ToInt64(values["transactionId"].ToString());

                    if (_context.Orders.Where(m => m.TransactionID == id).Count() == 0)
                    {
                        var order = new Models.Orders
                        {
                            Price = price,
                            State = "SUBMITTED",
                            Tokens = price,
                            TransactionID = Convert.ToInt64(values["transactionId"]),
                            UserID = Convert.ToInt32(Session["userID"].ToString())
                        };
                        _context.Orders.Add(order);
                        _context.SaveChanges();
                    }
                    else
                    {
                        ViewData["pin_error"] = "You did not complete your previous order.";
                    }
                }
                else
                {
                    ViewData["error"] = values["errorMessage"];
                }
                return View();
            }
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

        public ActionResult WonAuctions()
        {
            if(!hasAccess())
            {
                return new HttpNotFoundResult();
            }
            var auctions = getAuctions((int)Session["userID"]);
            return View(auctions);
        }

        private IEnumerable<Models.Auction> getAuctions(int id)
        {
            return _context.Auctions.Where(m => m.WonByUser == id).ToList();
        }

        public ActionResult PurchaseHistory()
        {
            if(!hasAccess())
            {
                return new HttpNotFoundResult();
            }
            var orders = getOrders((int)Session["userID"]);
            return View(orders);
        }

        private IEnumerable<Models.Orders> getOrders(int id)
        {
            return _context.Orders.Where(m => m.UserID == id && m.State == "COMPLETED").ToList();
        }
    }
}