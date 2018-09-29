using System.ComponentModel.DataAnnotations;
using System;
using System.Web;

namespace bidster.ViewModels
{
    public class NewAuction
    {
        [Required(ErrorMessage = "Name is required")]
        [MinLength(4)]
        public string Name { get; set; }

        [Required(ErrorMessage = "Price is required")]
        public float Price { get; set; }

        [Required(ErrorMessage = "Duration is required")]
        public long Duration { get; set; }

        [Required(ErrorMessage = "Image File is required")]
        [Display(Name = "Image")]
        public HttpPostedFileBase ImageFile { get; set; }
    }
}