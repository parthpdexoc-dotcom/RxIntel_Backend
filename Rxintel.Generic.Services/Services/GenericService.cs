using AutoMapper;
using Rxintel.Generic.Common.Implementations.Paginations;
using Rxintel.Generic.Common.Implementations.Responses;
using Rxintel.Generic.Enums;
using Rxintel.Generic.Repositories.Interfaces;
using Rxintel.Generic.Services.Interfaces;
using Rxintel.Logger.Logger;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;

namespace Rxintel.Generic.Services
{
    /// <summary>
    /// Generic service implementation providing common business logic operations for DTOs and entities.
    /// </summary>
    /// <typeparam name="TDto">The DTO type.</typeparam>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    public abstract class GenericService<TDto, TEntity> : IGenericService<TDto, TEntity> where TEntity : class where TDto : class
    {
        private readonly IGenericRepository<TEntity> _repository;
        private readonly IMapper _mapper;
        private readonly string _primaryKeyName;
        private readonly IRxintelLogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="GenericService{TDto, TEntity}"/> class.
        /// </summary>
        /// <param name="repository">The generic repository for data access.</param>
        /// <param name="mapper">The AutoMapper instance for object mapping.</param>
        /// <param name="primaryKeyName">The name of the primary key property.</param>
        /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
        public GenericService(IGenericRepository<TEntity> repository, IMapper mapper, string primaryKeyName)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _primaryKeyName = primaryKeyName ?? throw new ArgumentNullException(nameof(primaryKeyName));
            _logger = RxintelLogManager.GetLogger(typeof(GenericService<TDto, TEntity>));
        }

        /// <summary>
        /// Logs an error with context information.
        /// </summary>
        /// <param name="ex">The exception to log.</param>
        /// <param name="methodName">The name of the method where the error occurred.</param>
        /// <param name="additionalInfo">Additional context information.</param>
        private void LogError(Exception ex, string methodName, string additionalInfo = null)
        {
            var message = $"Error occurred in {methodName} for service {typeof(TEntity).Name}";
            if (!string.IsNullOrWhiteSpace(additionalInfo))
                message += $": {additionalInfo}";

            _logger.Error(message, ex);
        }

        /// <summary>
        /// Logs an informational message with formatted parameters.
        /// </summary>
        /// <param name="message">The message format string.</param>
        /// <param name="args">The arguments to format into the message.</param>
        private void LogInfo(string message, params object[] args)
        {
            if (args?.Length > 0)
                _logger.InfoFormat(message, memberName: "", lineNumber: 0, args);
            else
                _logger.Info(message);
        }

        /// <summary>
        /// Logs a debug message with formatted parameters.
        /// </summary>
        /// <param name="message">The message format string.</param>
        /// <param name="args">The arguments to format into the message.</param>
        private void LogDebug(string message, params object[] args)
        {
            if (args?.Length > 0)
                _logger.InfoFormat($"[DEBUG] {message}", memberName: "", lineNumber: 0, args);
            else
                _logger.Info($"[DEBUG] {message}");
        }

        /// <summary>
        /// Retrieves all DTOs matching the specified filter with optional pagination and includes.
        /// </summary>
        /// <param name="filter">Optional filter expression to apply.</param>
        /// <param name="includes">Optional navigation properties to include in the query.</param>
        /// <param name="pageParam">Optional pagination parameters.</param>
        /// <returns>A collection of DTOs matching the criteria.</returns>
        /// <exception cref="ArgumentException">Thrown when includes array is empty.</exception>
        public async virtual Task<IEnumerable<TDto>> GetAllAsync(Expression<Func<TEntity, bool>>? filter = null, Expression<Func<TEntity, object>>[] includes = null, PageParam pageParam = null)
        {
            if (includes != null && includes.Length == 0)
                throw new ArgumentException("Includes array cannot be empty", nameof(includes));

            LogDebug("Retrieving all DTOs of type {0} with filter: {1}, includes: {2}, pagination: {3}",
                typeof(TDto).Name, filter != null, includes?.Length ?? 0, pageParam != null);

            try
            {
                var entities = await _repository.GetAllAsync(filter, includes, pageParam).ConfigureAwait(false);
                var result = _mapper.Map<IEnumerable<TDto>>(entities);

                LogDebug("Successfully retrieved {0} DTOs of type {1}",
                    result.Count(), typeof(TDto).Name);
                return result;
            }
            catch (Exception ex)
            {
                LogError(ex, nameof(GetAllAsync), $"Filter: {filter != null}, Includes: {includes?.Length ?? 0}, Pagination: {pageParam != null}");
                throw;
            }
        }

        /// <summary>
        /// Retrieves a DTO by its primary key (Guid).
        /// </summary>
        /// <param name="id">The primary key value.</param>
        /// <returns>The DTO if found; otherwise, null.</returns>
        /// <exception cref="ArgumentException">Thrown when id is empty.</exception>
        public async virtual Task<TDto> GetByIdAsync(Guid id)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("Id cannot be empty", nameof(id));

            LogDebug("Retrieving DTO of type {0} by Guid key {1}", typeof(TDto).Name, id);

            try
            {
                var entity = await _repository.GetByIdAsync(id, _primaryKeyName).ConfigureAwait(false);
                var result = _mapper.Map<TDto>(entity);

                LogDebug("DTO retrieval {0} for type {1} with key {2}",
                    result != null ? "successful" : "unsuccessful", typeof(TDto).Name, id);
                return result;
            }
            catch (Exception ex)
            {
                LogError(ex, nameof(GetByIdAsync), $"Guid: {id}");
                throw;
            }
        }

        /// <summary>
        /// Retrieves a DTO by its primary key (string).
        /// </summary>
        /// <param name="id">The primary key value.</param>
        /// <returns>The DTO if found; otherwise, null.</returns>
        /// <exception cref="ArgumentException">Thrown when id is null or empty.</exception>
        public async virtual Task<TDto> GetByIdAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Id cannot be null or empty", nameof(id));

            LogDebug("Retrieving DTO of type {0} by string key {1}", typeof(TDto).Name, id);

            try
            {
                var entity = await _repository.GetByIdAsync(id, _primaryKeyName).ConfigureAwait(false);
                var result = _mapper.Map<TDto>(entity);

                LogDebug("DTO retrieval {0} for type {1} with key {2}",
                    result != null ? "successful" : "unsuccessful", typeof(TDto).Name, id);
                return result;
            }
            catch (Exception ex)
            {
                LogError(ex, nameof(GetByIdAsync), $"String: {id}");
                throw;
            }
        }
        /// <summary>
        /// Retrieves a DTO by composite keys.
        /// </summary>
        /// <param name="key1">The first key value.</param>
        /// <param name="key1PropertyName">The name of the first key property.</param>
        /// <param name="key2">The second key value.</param>
        /// <param name="key2PropertyName">The name of the second key property.</param>
        /// <returns>The DTO if found; otherwise, null.</returns>
        /// <exception cref="ArgumentException">Thrown when any parameter is null or empty.</exception>
        public virtual async Task<TDto> GetByCompositeKeysAsync(string key1, string key1PropertyName, string key2, string key2PropertyName)
        {
            if (string.IsNullOrWhiteSpace(key1))
                throw new ArgumentException("Key1 cannot be null or empty", nameof(key1));
            if (string.IsNullOrWhiteSpace(key1PropertyName))
                throw new ArgumentException("Key1PropertyName cannot be null or empty", nameof(key1PropertyName));
            if (string.IsNullOrWhiteSpace(key2))
                throw new ArgumentException("Key2 cannot be null or empty", nameof(key2));
            if (string.IsNullOrWhiteSpace(key2PropertyName))
                throw new ArgumentException("Key2PropertyName cannot be null or empty", nameof(key2PropertyName));

            LogDebug("Retrieving DTO of type {0} by composite keys {1}={2}, {3}={4}",
                typeof(TDto).Name, key1PropertyName, key1, key2PropertyName, key2);

            try
            {
                var entity = await _repository.GetByCompositeKeysAsync<string, string>(key1, key1PropertyName, key2, key2PropertyName).ConfigureAwait(false);
                var result = _mapper.Map<TDto>(entity);

                LogDebug("DTO retrieval {0} for type {1} with composite keys",
                    result != null ? "successful" : "unsuccessful", typeof(TDto).Name);
                return result;
            }
            catch (Exception ex)
            {
                LogError(ex, nameof(GetByCompositeKeysAsync), $"Keys: {key1PropertyName}={key1}, {key2PropertyName}={key2}");
                throw;
            }
        }

        /// <summary>
        /// Retrieves a collection of DTOs by composite keys.
        /// </summary>
        /// <param name="key1">The first key value.</param>
        /// <param name="key1PropertyName">The name of the first key property.</param>
        /// <param name="key2">The second key value.</param>
        /// <param name="key2PropertyName">The name of the second key property.</param>
        /// <returns>A collection of DTOs matching the composite keys.</returns>
        /// <exception cref="ArgumentException">Thrown when any parameter is null or empty.</exception>
        public virtual async Task<IEnumerable<TDto>> GetListByCompositeKeysAsync(string key1, string key1PropertyName, string key2, string key2PropertyName)
        {
            if (string.IsNullOrWhiteSpace(key1))
                throw new ArgumentException("Key1 cannot be null or empty", nameof(key1));
            if (string.IsNullOrWhiteSpace(key1PropertyName))
                throw new ArgumentException("Key1PropertyName cannot be null or empty", nameof(key1PropertyName));
            if (string.IsNullOrWhiteSpace(key2))
                throw new ArgumentException("Key2 cannot be null or empty", nameof(key2));
            if (string.IsNullOrWhiteSpace(key2PropertyName))
                throw new ArgumentException("Key2PropertyName cannot be null or empty", nameof(key2PropertyName));

            LogDebug("Retrieving DTOs of type {0} by composite keys {1}={2}, {3}={4}",
                typeof(TDto).Name, key1PropertyName, key1, key2PropertyName, key2);

            try
            {
                var entities = await _repository.GetListByCompositeKeysAsync<string, string>(key1, key1PropertyName, key2, key2PropertyName).ConfigureAwait(false);
                var result = _mapper.Map<IEnumerable<TDto>>(entities);

                LogDebug("Successfully retrieved {0} DTOs of type {1} with composite keys",
                    result.Count(), typeof(TDto).Name);
                return result;
            }
            catch (Exception ex)
            {
                LogError(ex, nameof(GetListByCompositeKeysAsync), $"Keys: {key1PropertyName}={key1}, {key2PropertyName}={key2}");
                throw;
            }
        }
        /// <summary>
        /// Inserts a new DTO or updates an existing one and returns a <see cref="SuccessResult{T}"/> with save/update message.
        /// </summary>
        /// <param name="dto">The DTO to insert or update.</param>
        /// <returns>Success result with upserted data, or invalid message when dto is null.</returns>
        public virtual async Task<SuccessResult<TDto>> UpsertAsync(TDto dto)
        {
            var entityName = typeof(TEntity).Name;

            if (dto == null)
            {
                return new SuccessResult<TDto>
                {
                    Data = null,
                    Status = (int)HttpStatusCode.OK,
                    Message = CustomMessage.GetInvalidMessage(entityName)
                };
            }

            var isNew = IsNewRecord(dto);
            LogDebug("Upserting DTO of type {0}, IsNew: {1}", typeof(TDto).Name, isNew);

            try
            {
                var entity = _mapper.Map<TEntity>(dto);
                await _repository.UpsertAsync(entity, _primaryKeyName).ConfigureAwait(false);
                var result = _mapper.Map<TDto>(entity);

                LogDebug("Successfully upserted DTO of type {0}", typeof(TDto).Name);

                return new SuccessResult<TDto>
                {
                    Data = result,
                    Status = (int)HttpStatusCode.OK,
                    Message = isNew
                        ? CustomMessage.GetSaveMessage(entityName)
                        : CustomMessage.GetUpdateMessage(entityName)
                };
            }
            catch (Exception ex)
            {
                LogError(ex, nameof(UpsertAsync), $"DTO: {typeof(TDto).Name}");
                throw;
            }
        }

        private bool IsNewRecord(TDto dto)
        {
            var prop = dto.GetType().GetProperty(_primaryKeyName)
                ?? dto.GetType().GetProperty("Id");
            if (prop == null)
                return false;

            var value = prop.GetValue(dto);
            return value is Guid guid && guid == Guid.Empty;
        }

        /// <summary>
        /// Inserts multiple new DTOs or updates existing ones.
        /// </summary>
        /// <param name="dtos">The DTOs to insert or update.</param>
        /// <returns>A collection of upserted DTOs.</returns>
        /// <exception cref="ArgumentNullException">Thrown when dtos is null.</exception>
        /// <exception cref="ArgumentException">Thrown when dtos is empty.</exception>
        public async virtual Task<IEnumerable<TDto>> BulkUpsertAsync(IEnumerable<TDto> dtos)
        {
            if (dtos == null)
                throw new ArgumentNullException(nameof(dtos));

            var dtosList = dtos.ToList();
            if (!dtosList.Any())
                throw new ArgumentException("DTOs collection cannot be empty", nameof(dtos));

            LogDebug("Bulk upserting {0} DTOs of type {1}", dtosList.Count, typeof(TDto).Name);

            try
            {
                var entities = _mapper.Map<IEnumerable<TEntity>>(dtosList);
                await _repository.BulkUpsertAsync(entities, _primaryKeyName).ConfigureAwait(false);
                var result = _mapper.Map<IEnumerable<TDto>>(entities);

                LogDebug("Successfully bulk upserted {0} DTOs of type {1}",
                    result.Count(), typeof(TDto).Name);
                return result;
            }
            catch (Exception ex)
            {
                LogError(ex, nameof(BulkUpsertAsync), $"DTO: {typeof(TDto).Name}, Count: {dtosList.Count}");
                throw;
            }
        }

        public async Task<SuccessResult<IEnumerable<TDto>>> BulkUpsert(IEnumerable<TDto> dtos)
        {
            var entityName = typeof(TEntity).Name;

            if (dtos == null)
            {
                return new SuccessResult<IEnumerable<TDto>>
                {
                    Data = null,
                    Status = (int)HttpStatusCode.OK,
                    Message = CustomMessage.GetInvalidMessage(entityName)
                };
            }

            var saved = await BulkUpsertAsync(dtos).ConfigureAwait(false);

            return new SuccessResult<IEnumerable<TDto>>
            {
                Data = saved?.ToList() ?? new List<TDto>(),
                Status = (int)HttpStatusCode.OK,
                Message = CustomMessage.GetSaveMessage(entityName)
            };
        }

        /// <summary>
        /// Updates the primary key value of an entity.
        /// </summary>
        /// <param name="oldKey">The current primary key value.</param>
        /// <param name="newKey">The new primary key value.</param>
        /// <param name="updateProperty">The property to update (optional).</param>
        /// <returns>True if the primary key was updated; otherwise, false.</returns>
        /// <exception cref="ArgumentException">Thrown when oldKey or newKey is null or empty.</exception>
        public async virtual Task<bool> UpdatePrimaryKeyAsync(string oldKey, string newKey, string updateProperty = null)
        {
            if (string.IsNullOrWhiteSpace(oldKey))
                throw new ArgumentException("OldKey cannot be null or empty", nameof(oldKey));
            if (string.IsNullOrWhiteSpace(newKey))
                throw new ArgumentException("NewKey cannot be null or empty", nameof(newKey));

            LogDebug("Updating primary key for entity type {0} from {1} to {2}",
                typeof(TEntity).Name, oldKey, newKey);

            try
            {
                var result = await _repository.UpdatePrimaryKeyAsync(oldKey, newKey, updateProperty).ConfigureAwait(false);
                LogDebug("Primary key update {0} for entity type {1}",
                    result ? "successful" : "unsuccessful", typeof(TEntity).Name);
                return result;
            }
            catch (Exception ex)
            {
                LogError(ex, nameof(UpdatePrimaryKeyAsync), $"OldKey: {oldKey}, NewKey: {newKey}");
                throw;
            }
        }

        /// <summary>
        /// Updates entities matching the specified filter using the provided update action.
        /// </summary>
        /// <param name="filter">The filter expression to identify entities to update.</param>
        /// <param name="updateAction">The action to apply to each matching entity.</param>
        /// <returns>The number of entities updated.</returns>
        /// <exception cref="ArgumentNullException">Thrown when filter or updateAction is null.</exception>
        public virtual async Task<int> UpdateAsync(Expression<Func<TEntity, bool>> filter, Action<TEntity> updateAction)
        {
            if (filter == null)
                throw new ArgumentNullException(nameof(filter));
            if (updateAction == null)
                throw new ArgumentNullException(nameof(updateAction));

            LogDebug("Updating entities of type {0} with custom filter", typeof(TEntity).Name);

            try
            {
                var result = await _repository.UpdateAsync(filter, updateAction).ConfigureAwait(false);
                LogDebug("Successfully updated {0} entities of type {1}", result, typeof(TEntity).Name);
                return result;
            }
            catch (Exception ex)
            {
                LogError(ex, nameof(UpdateAsync), $"Entity: {typeof(TEntity).Name}");
                throw;
            }
        }

        /// <summary>
        /// Deletes an entity by its primary key and returns a <see cref="SuccessResult{T}"/> with delete message.
        /// </summary>
        /// <param name="id">The primary key value.</param>
        /// <returns>Success result with delete success/failed message.</returns>
        public virtual async Task<SuccessResult<TDto>> DeleteAsync(Guid id)
        {
            var entityName = typeof(TEntity).Name;

            if (id == Guid.Empty)
            {
                return new SuccessResult<TDto>
                {
                    Data = null,
                    Status = (int)HttpStatusCode.OK,
                    Message = CustomMessage.GetInvalidMessage(entityName)
                };
            }

            LogDebug("Deleting entity of type {0} with key {1}", typeof(TEntity).Name, id);

            try
            {
                var deleted = await _repository.DeleteAsync(id).ConfigureAwait(false);
                LogDebug("Entity deletion {0} for type {1} with key {2}",
                    deleted ? "successful" : "unsuccessful", typeof(TEntity).Name, id);

                return new SuccessResult<TDto>
                {
                    Data = null,
                    Status = (int)HttpStatusCode.OK,
                    Message = deleted
                        ? CustomMessage.GetDeleteSuccessMessage(entityName)
                        : CustomMessage.GetDeleteFailedMessage(entityName)
                };
            }
            catch (Exception ex)
            {
                LogError(ex, nameof(DeleteAsync), $"Entity: {typeof(TEntity).Name}, Key: {id}");
                throw;
            }
        }

        /// <summary>
        /// Deletes multiple entities by their key values.
        /// </summary>
        /// <param name="ids">The key values of entities to delete.</param>
        /// <returns>True if any entities were deleted; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException">Thrown when ids is null.</exception>
        /// <exception cref="ArgumentException">Thrown when ids is empty.</exception>
        public async Task<bool> BulkDeleteAsync(List<string> ids)
        {
            if (ids == null)
                throw new ArgumentNullException(nameof(ids));
            if (!ids.Any())
                throw new ArgumentException("Ids collection cannot be empty", nameof(ids));

            LogDebug("Bulk deleting entities of type {0} with {1} keys", typeof(TEntity).Name, ids.Count);

            try
            {
                var result = await _repository.BulkDeleteAsync(ids, _primaryKeyName).ConfigureAwait(false);
                LogDebug("Bulk deletion {0} for {1} entities of type {2}",
                    result ? "successful" : "unsuccessful", ids.Count, typeof(TEntity).Name);
                return result;
            }
            catch (Exception ex)
            {
                LogError(ex, nameof(BulkDeleteAsync), $"Entity: {typeof(TEntity).Name}, Count: {ids.Count}");
                throw;
            }
        }

        /// <summary>
        /// Deletes entities by a specific property value.
        /// </summary>
        /// <typeparam name="TValue">The type of the property value.</typeparam>
        /// <param name="propertyName">The name of the property to filter by.</param>
        /// <param name="value">The value to match.</param>
        /// <returns>True if any entities were deleted; otherwise, false.</returns>
        /// <exception cref="ArgumentException">Thrown when propertyName is null or empty.</exception>
        public async Task<bool> DeleteByPropertyAsync<TValue>(string propertyName, TValue value)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
                throw new ArgumentException("PropertyName cannot be null or empty", nameof(propertyName));

            LogDebug("Deleting entities of type {0} by property {1}={2}",
                typeof(TEntity).Name, propertyName, value);

            try
            {
                var result = await _repository.DeleteByPropertyAsync(propertyName, value).ConfigureAwait(false);
                LogDebug("Property-based deletion {0} for entities of type {1}",
                    result ? "successful" : "unsuccessful", typeof(TEntity).Name);
                return result;
            }
            catch (Exception ex)
            {
                LogError(ex, nameof(DeleteByPropertyAsync), $"Entity: {typeof(TEntity).Name}, Property: {propertyName}");
                throw;
            }
        }
        /// <summary>
        /// Adds multiple DTOs to the repository.
        /// </summary>
        /// <param name="clientDtos">The DTOs to add.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when clientDtos is null.</exception>
        /// <exception cref="ArgumentException">Thrown when clientDtos is empty.</exception>
        public async Task AddRangeAsync(IEnumerable<TDto> clientDtos)
        {
            if (clientDtos == null)
                throw new ArgumentNullException(nameof(clientDtos));

            var dtosList = clientDtos.ToList();
            if (!dtosList.Any())
                throw new ArgumentException("ClientDtos collection cannot be empty", nameof(clientDtos));

            LogDebug("Adding {0} DTOs of type {1}", dtosList.Count, typeof(TDto).Name);

            try
            {
                var entities = _mapper.Map<IEnumerable<TEntity>>(dtosList);
                await _repository.AddRangeAsync(entities).ConfigureAwait(false);
                LogDebug("Successfully added {0} DTOs of type {1}", dtosList.Count, typeof(TDto).Name);
            }
            catch (Exception ex)
            {
                LogError(ex, nameof(AddRangeAsync), $"DTO: {typeof(TDto).Name}, Count: {dtosList.Count}");
                throw;
            }
        }

        /// <summary>
        /// Retrieves DTOs by multiple composite key combinations.
        /// </summary>
        /// <param name="compositeKeys">A collection of dictionaries containing key-value pairs for composite keys.</param>
        /// <returns>A collection of DTOs matching any of the composite key combinations.</returns>
        /// <exception cref="ArgumentNullException">Thrown when compositeKeys is null.</exception>
        /// <exception cref="ArgumentException">Thrown when compositeKeys is empty.</exception>
        public async Task<IEnumerable<TDto>> GetListByCompositeKeyArrayAsync(IEnumerable<Dictionary<string, string>> compositeKeys)
        {
            if (compositeKeys == null)
                throw new ArgumentNullException(nameof(compositeKeys));

            var keysList = compositeKeys.ToList();
            if (!keysList.Any())
                throw new ArgumentException("CompositeKeys collection cannot be empty", nameof(compositeKeys));

            LogDebug("Retrieving DTOs of type {0} by {1} composite key combinations",
                typeof(TDto).Name, keysList.Count);

            try
            {
                var entities = await _repository.GetListByCompositeKeyArrayAsync(compositeKeys).ConfigureAwait(false);
                var result = _mapper.Map<IEnumerable<TDto>>(entities);

                LogDebug("Successfully retrieved {0} DTOs of type {1} by composite keys",
                    result.Count(), typeof(TDto).Name);
                return result;
            }
            catch (Exception ex)
            {
                LogError(ex, nameof(GetListByCompositeKeyArrayAsync), $"DTO: {typeof(TDto).Name}, Count: {keysList.Count}");
                throw;
            }
        }
        protected PagedList<TItem> ToPagedList<TItem>(List<TItem> list, PageParam? pageParam) where TItem : class
        {
            if (pageParam != null && pageParam.IsPaginationRequested)
                return ApplyPagination(list.AsQueryable(), pageParam);

            return new PagedList<TItem> { Data = list, TotalRecords = list.Count };
        }
        protected PagedList<T> ApplyPagination<T>(IQueryable<T> query, PageParam pageParam) where T : class
        {
            // ?? Null safety + default values
            pageParam ??= new PageParam();

            var offset = pageParam.Offset ?? 0;
            var limit = pageParam.Limit ?? 10;

            // ?? Sorting (safe dynamic)
            if (!string.IsNullOrWhiteSpace(pageParam.SortBy))
            {
                try
                {
                    var propInfo = typeof(T).GetProperty(pageParam.SortBy,
                        System.Reflection.BindingFlags.IgnoreCase |
                        System.Reflection.BindingFlags.Public |
                        System.Reflection.BindingFlags.Instance);

                    if (propInfo != null)
                    {
                        var parameter = Expression.Parameter(typeof(T), "x");
                        var property = Expression.Property(parameter, propInfo);
                        var lambda = Expression.Lambda(property, parameter);

                        string methodName = pageParam.SortDirection?.ToLower() == "desc"
                            ? "OrderByDescending"
                            : "OrderBy";

                        var resultExpression = Expression.Call(
                            typeof(Queryable),
                            methodName,
                            new Type[] { typeof(T), propInfo.PropertyType },
                            query.Expression,
                            Expression.Quote(lambda)
                        );

                        query = query.Provider.CreateQuery<T>(resultExpression);
                    }
                }
                catch
                {
                    // sorting fail ho to ignore karo (safe fallback)
                }
            }

            // ?? Total records
            var totalRecords = query.Count();

            // ?? Pagination
            var data = query
                .Skip(offset)
                .Take(limit)
                .ToList();

            return new PagedList<T>(data, pageParam, totalRecords);
        }

        /// <summary>
        /// Wraps bulk-saved DTOs in <see cref="SuccessResult{T}"/> using the same
        /// <see cref="CustomMessage.GetSaveMessage"/> template as <see cref="UpsertAsync"/>.
        /// </summary>
        protected SuccessResult<IEnumerable<TDto>> BuildBulkUpsertSuccessResult(IEnumerable<TDto>? data)
        {
            var entityName = typeof(TEntity).Name;
            var list = data != null ? data.ToList() : new List<TDto>();
            return new SuccessResult<IEnumerable<TDto>>
            {
                Data = list,
                Status = (int)HttpStatusCode.OK,
                Message = CustomMessage.GetSaveMessage(entityName)
            };
        }

    
        

    }
}
