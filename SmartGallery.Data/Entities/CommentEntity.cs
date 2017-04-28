using System;

namespace SmartGallery.Data.Entities
{
    public class CommentEntity
    {
        public string Id { get; set; }
        public string PictureId { get; set; }
        public DateTime? Created { get; set; }
        public string UserId { get; set; }
        public string ProfileUrl { get; set; }
        public string Message { get; set; }
    }
}