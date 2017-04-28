using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace SmartGallery.Web.Models
{
    public class Comment
    {
        [Required]
        public string Message { get; set; }
    }
}