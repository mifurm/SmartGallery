using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmartGallery.Data.Entities;

namespace SmartGallery.Data.Repositories
{
    public interface ICommentRepository : IRepositoryAsync<CommentEntity, string>
    {
    }
}
