using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SmartGallery.Web.ViewModels
{
    public class PhotoVM
    {
        public string PictureUrl { get; set; }
        public string PictureName { get; set; }
        public string Caption { get; set; }
        public List<string> Tags { get; set; }
        public List<CommentVM> Comments { get; set; }
    }
}
