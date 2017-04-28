using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartGallery.Data.Repositories
{
    public interface IRepositoryAsync<TEntity, TKey> where TEntity : class
    {
        Task<List<TEntity>> GetAllAsync();
        Task<TEntity> FindAsync(TKey id);
        Task AddAsync(TEntity entity);
        Task DeleteAsync(TEntity entity);
        Task SaveAsync();
    }
}
