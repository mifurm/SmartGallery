using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SmartGallery.Web.ViewModels
{
    public class CommentVM
    {
        public DateTime? Created { get; set; }
        public string UserId { get; set; }
        public string ProfileUrl { get; set; }
        public string Message { get; set; }
    }
}