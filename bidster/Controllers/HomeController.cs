using bidster.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Security.Cryptography;
using System.Text;
using System.IO;
using System.Net.Mail;

namespace bidster.Controllers
{ 
    public class HomeController : Controller
    {
        private const int MIN_PASS_LENGTH = 8;
        private Models.BidsterContext _context;

        public HomeController()
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
            if(Session["userType"] != null)
            {
                return false;
            }
            return true;
        }
        public ActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public ActionResult Login()
        {
            if(!hasAccess())
            {
                return new HttpNotFoundResult();
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(ViewModels.UserLogin user)
        {

            if (!hasAccess())
            {
                return new HttpNotFoundResult();
            }

            if (!ModelState.IsValid)
            {
                return View();
            }

            var checkUser = getUser(user.Email);

            if (checkUser == null)
            {
                ModelState.AddModelError("Email", "Email does not exist.");
                return View();
            }

            if (hashPassword(user.Password, checkUser.Salt) != checkUser.PasswordHash)
            {
                ModelState.AddModelError("Password", "Incorrect password.");
                return View();
            }

            Session["userType"] = checkUser.AccountType;
            Session["userID"] = checkUser.ID;

            if(Session["userType"].ToString() == "admin")
            {
                return Redirect("/Admin/ChangeSystemParameters");
            }
            return Redirect("/User/MyProfile");
        }

        [HttpGet]
        public ActionResult Register()
        {
            if (!hasAccess())
            {
                return new HttpNotFoundResult();
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(ViewModels.User user)
        {

            if (!hasAccess())
            {
                return new HttpNotFoundResult();
            }

            if (!ModelState.IsValid)
                return View();
        
            if(emailExists(user.Email))
            {
                ModelState.AddModelError("Email", "Email already exists.");
                return View();
            }

            if(user.Password.Length < MIN_PASS_LENGTH)
            {
                ModelState.AddModelError("Password", "Password should be at least " + MIN_PASS_LENGTH + " characters long.");
                return View();
            }

            if (user.Password != user.Confirm)
            {
                ModelState.AddModelError("Confirm", "Passwords do not match.");
                return View();
            }
            
            var salt = generateSalt(32);
            Console.WriteLine(salt);
            user.Password = hashPassword(user.Password, salt);
            
            //save user to db
            var newUser = new User
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                PasswordHash = user.Password,
                Salt = salt,
                AccountType = "user",
                Tokens = 0
            };

            _context.Users.Add(newUser);
            _context.SaveChanges();

            ViewData["success"] = true;
            
            return View();
        }

        private string hashPassword(string password, string salt)
        {
            var bytes = Encoding.UTF8.GetBytes(password + salt);
            SHA256Managed sha256 = new SHA256Managed();
            var hash = sha256.ComputeHash(bytes);
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }

        private string generateSalt(int maximumSaltLength)
        {
            var salt = new byte[maximumSaltLength];
            using (var random = new RNGCryptoServiceProvider())
            {
                random.GetNonZeroBytes(salt);
            }

            return BitConverter.ToString(salt).Replace("-", "").ToLower();
        }

        private bool emailExists(string email)
        {
            var user = _context.Users.Where(s => s.Email == email).FirstOrDefault();
            return user != null;
        }

        private User getUser(string email)
        {
            return _context.Users.Where(s => s.Email == email).FirstOrDefault();
        } 
    }
}