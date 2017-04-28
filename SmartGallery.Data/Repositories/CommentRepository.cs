using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using SmartGallery.Data.Entities;

namespace SmartGallery.Data.Repositories
{
    public class CommentRepository : ICommentRepository
    {
        public CommentRepository(string dbHost, string dbKey, string dbName, string commentCollection)
        {
            _client = new DocumentClient(new Uri(dbHost),
                dbKey);
            _dbName = dbName;
            _commentCollection = commentCollection;
        }

        private readonly DocumentClient _client;
        private readonly string _dbName;
        private readonly string _commentCollection;

        public async Task<List<CommentEntity>> GetAllAsync()
        {
            return await GetAllAsync(x => true);
        }

        public async Task<List<CommentEntity>> GetAllAsync(Expression<Func<CommentEntity, bool>> predicate)
        {
            var result = await GetItemsAsync<CommentEntity>(_commentCollection, predicate);
            return result.ToList();
        }

        public async Task<CommentEntity> FindAsync(string id)
        {
            var result = await GetAllAsync(x => x.Id == id);
            return result.FirstOrDefault();
        }

        public async Task AddAsync(CommentEntity entity)
        {
            var commentsUri = UriFactory.CreateDocumentCollectionUri(_dbName, _commentCollection);
            await _client.CreateDocumentAsync(commentsUri, entity);
        }

        public async Task DeleteAsync(CommentEntity entity)
        {
            var commentUri = UriFactory.CreateDocumentUri(_dbName, _commentCollection, entity.Id);
            await _client.DeleteDocumentAsync(commentUri);
        }

        public Task SaveAsync()
        {
            throw new NotImplementedException();
        }

        public async Task InitializeAsync()
        {
            await _client.CreateDatabaseIfNotExistsAsync(new Database()
            {
                Id = _dbName
            });
            await _client.CreateDocumentCollectionIfNotExistsAsync(
                UriFactory.CreateDatabaseUri(_dbName),
                new DocumentCollection()
                {
                    Id = _commentCollection
                },
                new RequestOptions()
                {
                    OfferThroughput = 400
                }
                );
        }
        private async Task<IEnumerable<T>> GetItemsAsync<T>(string collectionId, Expression<Func<T, bool>> predicate)
        {
            var collectionUri = UriFactory.CreateDocumentCollectionUri(_dbName, collectionId);
            var query = _client.CreateDocumentQuery<T>(collectionUri)
                .Where(predicate)
                .AsDocumentQuery();

            List<T> list = new List<T>();
            while (query.HasMoreResults)
            {
                var queryResult = await query.ExecuteNextAsync<T>();
                list.AddRange(queryResult);
            }
            return list;
        }
    }
}
