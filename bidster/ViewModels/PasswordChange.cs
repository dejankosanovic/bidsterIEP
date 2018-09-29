using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace bidster.ViewModels
{
    public class PasswordChange
    {
        [Required(ErrorMessage = "Old Password is required")]
        [MinLength(8)]
        public string OldPassword { get; set; }

        [Required(ErrorMessage = "New Password is required")]
        [MinLength(8)]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "New Password confirmation is required")]
        [MinLength(8)]
        public string ConfirmPassword { get; set; }
    }
}