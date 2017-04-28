using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmartGallery.Data.Entities;

namespace SmartGallery.Data.Repositories
{
    public interface IRepository<TEntity, TKey> where TEntity : class
    {
        List<TEntity> GetAll();
        TEntity Find(TKey id);
        void Add(TEntity entity);
        void Delete(TEntity entity);
        void Save();
    }
}
