using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.QueryParsers;
using NHibernate.AdoNet;
using NHibernate.Collection;
using NHibernate.Engine;
using NHibernate.Engine.Query.Sql;
using NHibernate.Event;
using NHibernate.Hql;
using NHibernate.Impl;
using NHibernate.Loader.Custom;
using NHibernate.Persister.Entity;
using NHibernate.Search.Backend;
using NHibernate.Search.Engine;
using NHibernate.Search.Query;
using NHibernate.Search.Util;
using NHibernate.Stat;
using NHibernate.Transaction;
using NHibernate.Type;

namespace NHibernate.Search.Impl
{
    public class FullTextSessionImpl : IFullTextSession, ISessionImplementor
    {
        private readonly ISession session;
        private readonly IEventSource eventSource;
        private readonly ISessionImplementor sessionImplementor;
        private ISearchFactoryImplementor searchFactory;

        public FullTextSessionImpl(ISession session)
        {
            this.session = session;
            this.eventSource = (IEventSource) session;
            this.sessionImplementor = (ISessionImplementor) session;
        }

        public ISearchFactory SearchFactory
        {
            get
            {
                if (searchFactory == null)
                {
                    searchFactory = ContextHelper.GetSearchFactory(session);
                }

                return searchFactory;
            }
        }

        private ISearchFactoryImplementor SearchFactoryImplementor
        {
            get
            {
                if (searchFactory == null)
                {
                    searchFactory = ContextHelper.GetSearchFactory(session);
                }

                return searchFactory;
            }
        }

        #region Delegating to Inner Session

        public void Flush()
        {
            session.Flush();
        }

        public IDbConnection Disconnect()
        {
            return session.Disconnect();
        }

        public void Reconnect()
        {
            session.Reconnect();
        }

        public void Reconnect(IDbConnection connection)
        {
            session.Reconnect(connection);
        }

        public IDbConnection Close()
        {
            return session.Close();
        }

        public void CancelQuery()
        {
            session.CancelQuery();
        }

        public bool IsDirty()
        {
            return session.IsDirty();
        }

    	public bool IsReadOnly(object entityOrProxy)
    	{
				return session.IsReadOnly(entityOrProxy);
    	}

    	public void SetReadOnly(object entityOrProxy, bool readOnly)
    	{
				session.SetReadOnly(entityOrProxy, readOnly);
			}

    	public object GetIdentifier(object obj)
        {
            return session.GetIdentifier(obj);
        }

        public bool Contains(object obj)
        {
            return session.Contains(obj);
        }

        public void Evict(object obj)
        {
            session.Evict(obj);
        }

        public object Load(System.Type theType, object id, LockMode lockMode)
        {
            return session.Load(theType, id, lockMode);
        }

		public object Load(string entityName, object id, LockMode lockMode)
		{
			return session.Load(entityName, id, lockMode);
		}

        public object Load(System.Type theType, object id)
        {
            return session.Load(theType, id);
        }

        public T Load<T>(object id, LockMode lockMode)
        {
            return session.Load<T>(id, lockMode);
        }

        public T Load<T>(object id)
        {
            return session.Load<T>(id);
        }

		public object Load(string entityName, object id)
		{
			return session.Load(entityName, id);
		}

        public void Load(object obj, object id)
        {
            session.Load(obj, id);
        }

        public void Replicate(object obj, ReplicationMode replicationMode)
        {
            session.Replicate(obj, replicationMode);
        }

        public ISessionStatistics Statistics
        {
            get { return session.Statistics; }
        }

		public EntityMode ActiveEntityMode
		{
			get { return session.ActiveEntityMode; }
		}

        public FlushMode FlushMode
        {
            get { return session.FlushMode; }
            set { session.FlushMode = value; }
        }

        public CacheMode CacheMode
        {
            get { return session.CacheMode; }
            set { session.CacheMode = value; }
        }

        public ISessionFactory SessionFactory
        {
            get { return session.SessionFactory; }
        }

        public IDbConnection Connection
        {
            get { return session.Connection; }
        }

        public bool IsOpen
        {
            get { return session.IsOpen; }
        }

        public bool IsConnected
        {
            get { return session.IsConnected; }
        }

    	public bool DefaultReadOnly
    	{
    		get { return session.DefaultReadOnly; }
				set { session.DefaultReadOnly = value; }
    	}

    	public ITransaction Transaction
        {
            get { return session.Transaction; }
        }

        public ISession SetBatchSize(int batchSize)
        {
            return session.SetBatchSize(batchSize);
        }

        public ISessionImplementor GetSessionImplementation()
        {
            return session.GetSessionImplementation();
        }

        public void Replicate(string entityName, object obj, ReplicationMode replicationMode)
        {
            session.Replicate(entityName, obj, replicationMode);
        }

        public object Save(object obj)
        {
            return session.Save(obj);
        }

        public void Save(object obj, object id)
        {
            session.Save(obj, id);
        }

        public object Save(string entityName, object obj)
        {
            return session.Save(entityName, obj);
        }

        public void SaveOrUpdate(object obj)
        {
            session.SaveOrUpdate(obj);
        }

        public void SaveOrUpdate(string entityName, object obj)
        {
            session.SaveOrUpdate(entityName, obj);
        }

        public void Update(object obj)
        {
            session.Update(obj);
        }

        public void Update(object obj, object id)
        {
            session.Update(obj, id);
        }

        public void Update(string entityName, object obj)
        {
            session.Update(entityName, obj);
        }

        public object Merge(object obj)
        {
            return session.Merge(obj);
        }

        public object Merge(string entityName, object obj)
        {
            return session.Merge(entityName, obj);
        }

        public T Merge<T>(T entity) where T : class
        {
            return session.Merge(entity);
        }

        public T Merge<T>(string entityName, T entity) where T : class
        {
            return session.Merge(entityName, entity);
        }

        public void Persist(object obj)
        {
            session.Persist(obj);
        }

        public void Persist(string entityName, object obj)
        {
            session.Persist(entityName, obj);
        }

        public object SaveOrUpdateCopy(object obj)
        {
            return session.SaveOrUpdateCopy(obj);
        }

        public object SaveOrUpdateCopy(object obj, object id)
        {
            return session.SaveOrUpdateCopy(obj, id);
        }

        public void Delete(object obj)
        {
            session.Delete(obj);
        }

		public void Delete(string entityName, object obj)
		{
			session.Delete(entityName, obj);
		}

        public int Delete(string query)
        {
            return session.Delete(query);
        }

        public int Delete(string query, object value, IType type)
        {
            return session.Delete(query, value, type);
        }

        public int Delete(string query, object[] values, IType[] types)
        {
            return session.Delete(query, values, types);
        }

        public void Lock(object obj, LockMode lockMode)
        {
            session.Lock(obj, lockMode);
        }

        public void Lock(string entityName, object obj, LockMode lockMode)
        {
            session.Lock(entityName, obj, lockMode);
        }

        public void Refresh(object obj)
        {
            session.Refresh(obj);
        }

        public void Refresh(object obj, LockMode lockMode)
        {
            session.Refresh(obj, lockMode);
        }

        public LockMode GetCurrentLockMode(object obj)
        {
            return session.GetCurrentLockMode(obj);
        }

        public ITransaction BeginTransaction()
        {
            return session.BeginTransaction();
        }

        public ITransaction BeginTransaction(IsolationLevel isolationLevel)
        {
            return session.BeginTransaction(isolationLevel);
        }

		public ICriteria CreateCriteria<T>() where T : class
        {
            return session.CreateCriteria<T>();
        }

        public ICriteria CreateCriteria<T>(string alias) where T : class
        {
            return session.CreateCriteria<T>(alias);
        }

		public ICriteria CreateCriteria(System.Type persistentClass)
        {
            return session.CreateCriteria(persistentClass);
        }

        public ICriteria CreateCriteria(System.Type persistentClass, string alias)
        {
            return session.CreateCriteria(persistentClass, alias);
        }

        public ICriteria CreateCriteria(string entityName)
        {
            return session.CreateCriteria(entityName);
        }

        public ICriteria CreateCriteria(string entityName, string alias)
        {
            return session.CreateCriteria(entityName, alias);
        }

    	public IQueryOver<T, T> QueryOver<T>() where T : class
    	{
    		return session.QueryOver<T>();
    	}

    	public IQueryOver<T, T> QueryOver<T>(Expression<Func<T>> alias) where T : class
    	{
    		return session.QueryOver(alias);
    	}

        public IQueryOver<T, T> QueryOver<T>(string entityName) where T : class
        {
            return session.QueryOver<T>(entityName);
        }

        public IQueryOver<T, T> QueryOver<T>(string entityName, Expression<Func<T>> alias) where T : class
        {
            return session.QueryOver<T>(entityName, alias);
        }

        public IQuery CreateQuery(string queryString)
        {
            return session.CreateQuery(queryString);
        }

    	public IQuery CreateFilter(object collection, string queryString)
        {
            return session.CreateFilter(collection, queryString);
        }

        public IQuery GetNamedQuery(string queryName)
        {
            return session.GetNamedQuery(queryName);
        }

        public ISQLQuery CreateSQLQuery(string queryString)
        {
            return session.CreateSQLQuery(queryString);
        }

        public void Clear()
        {
            session.Clear();
        }

        public object Get(System.Type clazz, object id)
        {
            return session.Get(clazz, id);
        }

        public object Get(System.Type clazz, object id, LockMode lockMode)
        {
            return session.Get(clazz, id, lockMode);
        }

        public object Get(string entityName, object id)
        {
            return session.Get(entityName, id);
        }

        public T Get<T>(object id)
        {
            return session.Get<T>(id);
        }

        public T Get<T>(object id, LockMode lockMode)
        {
            return session.Get<T>(id, lockMode);
        }

        public String GetEntityName(object obj)
        {
            return session.GetEntityName(obj);
        }

        public IFilter EnableFilter(string filterName)
        {
            return session.EnableFilter(filterName);
        }

        public IFilter GetEnabledFilter(string filterName)
        {
            return session.GetEnabledFilter(filterName);
        }

        public void DisableFilter(string filterName)
        {
            session.DisableFilter(filterName);
        }

        public IMultiQuery CreateMultiQuery()
        {
            return session.CreateMultiQuery();
        }

        public IMultiCriteria CreateMultiCriteria()
        {
            return session.CreateMultiCriteria();
        }

        public ISession GetSession(EntityMode entityMode)
        {
            return session.GetSession(entityMode);
        }

        #endregion

        #region ISessionImplementor

        public void Initialize()
        {
            sessionImplementor.Initialize();
        }

        public void InitializeCollection(IPersistentCollection collection, bool writing)
        {
            sessionImplementor.InitializeCollection(collection, writing);
        }

        public object InternalLoad(string entityName, object id, bool eager, bool isNullable)
        {
            return sessionImplementor.InternalLoad(entityName, id, eager, isNullable);
        }

        public object ImmediateLoad(string entityName, object id)
        {
            return sessionImplementor.ImmediateLoad(entityName, id);
        }

        public IList List(string query, QueryParameters parameters)
        {
            return sessionImplementor.List(query, parameters);
        }

        public IList List(IQueryExpression queryExpression, QueryParameters parameters)
        {
            return sessionImplementor.List(queryExpression, parameters);
        }

        public IQuery CreateQuery(IQueryExpression queryExpression)
        {
            return sessionImplementor.CreateQuery(queryExpression);
        }

        public void List(string query, QueryParameters parameters, IList results)
        {
            sessionImplementor.List(query, parameters, results);
        }

        public IList<T> List<T>(string query, QueryParameters queryParameters)
        {
            return sessionImplementor.List<T>(query, queryParameters);
        }

        public IList<T> List<T>(CriteriaImpl criteria)
        {
            return sessionImplementor.List<T>(criteria);
        }

        public void List(CriteriaImpl criteria, IList results)
        {
            sessionImplementor.List(criteria, results);
        }

        public IList List(CriteriaImpl criteria)
        {
            return sessionImplementor.List(criteria);
        }

        public IEnumerable Enumerable(string query, QueryParameters parameters)
        {
            return sessionImplementor.Enumerable(query, parameters);
        }

        public IEnumerable<T> Enumerable<T>(string query, QueryParameters queryParameters)
        {
            return sessionImplementor.Enumerable<T>(query, queryParameters);
        }

        public IList ListFilter(object collection, string filter, QueryParameters parameters)
        {
            return sessionImplementor.ListFilter(collection, filter, parameters);
        }

        public IList<T> ListFilter<T>(object collection, string filter, QueryParameters parameters)
        {
            return sessionImplementor.ListFilter<T>(collection, filter, parameters);
        }

        public IEnumerable EnumerableFilter(object collection, string filter, QueryParameters parameters)
        {
            return sessionImplementor.EnumerableFilter(collection, filter, parameters);
        }

        public IEnumerable<T> EnumerableFilter<T>(object collection, string filter, QueryParameters parameters)
        {
            return sessionImplementor.EnumerableFilter<T>(collection, filter, parameters);
        }

        public IEntityPersister GetEntityPersister(string entityName, object obj)
        {
            return sessionImplementor.GetEntityPersister(entityName, obj);
        }

        public void AfterTransactionBegin(ITransaction tx)
        {
            sessionImplementor.AfterTransactionBegin(tx);
        }

        public void BeforeTransactionCompletion(ITransaction tx)
        {
            sessionImplementor.BeforeTransactionCompletion(tx);
        }

        public void AfterTransactionCompletion(bool successful, ITransaction tx)
        {
            sessionImplementor.AfterTransactionCompletion(successful, tx);
        }

        public object GetContextEntityIdentifier(object obj)
        {
            return sessionImplementor.GetContextEntityIdentifier(obj);
        }

        public object Instantiate(string entityName, object id)
        {
            return sessionImplementor.Instantiate(entityName, id);
        }

        public IList List(NativeSQLQuerySpecification spec, QueryParameters queryParameters)
        {
            return sessionImplementor.List(spec, queryParameters);
        }

        public void List(NativeSQLQuerySpecification spec, QueryParameters queryParameters, IList results)
        {
            sessionImplementor.List(spec, queryParameters, results);
        }

        public IList<T> List<T>(NativeSQLQuerySpecification spec, QueryParameters queryParameters)
        {
            return sessionImplementor.List<T>(spec, queryParameters);
        }

        public void ListCustomQuery(ICustomQuery customQuery, QueryParameters queryParameters, IList results)
        {
            sessionImplementor.ListCustomQuery(customQuery, queryParameters, results);
        }

        public IList<T> ListCustomQuery<T>(ICustomQuery customQuery, QueryParameters queryParameters)
        {
            return sessionImplementor.ListCustomQuery<T>(customQuery, queryParameters);
        }

        public object GetFilterParameterValue(string filterParameterName)
        {
            return sessionImplementor.GetFilterParameterValue(filterParameterName);
        }

        public IType GetFilterParameterType(string filterParameterName)
        {
            return sessionImplementor.GetFilterParameterType(filterParameterName);
        }

        public IQuery GetNamedSQLQuery(string name)
        {
            return sessionImplementor.GetNamedSQLQuery(name);
        }

        public IQueryTranslator[] GetQueries(string query, bool scalar)
        {
            return sessionImplementor.GetQueries(query, scalar);
        }

        public object GetEntityUsingInterceptor(EntityKey key)
        {
            return sessionImplementor.GetEntityUsingInterceptor(key);
        }

        public string BestGuessEntityName(object entity)
        {
            return sessionImplementor.BestGuessEntityName(entity);
        }

        public string GuessEntityName(object entity)
        {
            return sessionImplementor.GuessEntityName(entity);
        }

        public int ExecuteNativeUpdate(NativeSQLQuerySpecification specification, QueryParameters queryParameters)
        {
            return sessionImplementor.ExecuteNativeUpdate(specification, queryParameters);
        }

        public int ExecuteUpdate(string query, QueryParameters queryParameters)
        {
            return sessionImplementor.ExecuteUpdate(query, queryParameters);
        }

        public void CloseSessionFromDistributedTransaction()
        {
            sessionImplementor.CloseSessionFromDistributedTransaction();
        }

        public long Timestamp
        {
            get { return sessionImplementor.Timestamp; }
        }

        public ISessionFactoryImplementor Factory
        {
            get { return sessionImplementor.Factory; }
        }

        public IBatcher Batcher
        {
            get { return sessionImplementor.Batcher; }
        }

        public IDictionary<string, IFilter> EnabledFilters
        {
            get { return sessionImplementor.EnabledFilters; }
        }

        public IInterceptor Interceptor
        {
            get { return sessionImplementor.Interceptor; }
        }

        public EventListeners Listeners
        {
            get { return sessionImplementor.Listeners; }
        }

        public int DontFlushFromFind
        {
            get { return sessionImplementor.DontFlushFromFind; }
        }

        public ConnectionManager ConnectionManager
        {
            get { return sessionImplementor.ConnectionManager; }
        }

        public bool IsEventSource
        {
            get { return sessionImplementor.IsEventSource; }
        }

        public IPersistenceContext PersistenceContext
        {
            get { return sessionImplementor.PersistenceContext; }
        }

        public string FetchProfile
        {
            get { return sessionImplementor.FetchProfile; }
            set { sessionImplementor.FetchProfile = value; }
        }

        public bool IsClosed
        {
            get { return sessionImplementor.IsClosed; }
        }

        public bool TransactionInProgress
        {
            get { return sessionImplementor.TransactionInProgress; }
        }

        public EntityMode EntityMode
        {
            get { return sessionImplementor.EntityMode; }
        }

        public FutureCriteriaBatch FutureCriteriaBatch
        {
            get { return sessionImplementor.FutureCriteriaBatch; }
        }

        public FutureQueryBatch FutureQueryBatch
        {
            get { return sessionImplementor.FutureQueryBatch; }
        }

        public Guid SessionId
        {
            get { return sessionImplementor.SessionId; }
        }

        public ITransactionContext TransactionContext
        {
            get { return sessionImplementor.TransactionContext; }
            set { sessionImplementor.TransactionContext = value; }
        }

        #endregion

        #region IFullTextSession Members

        public IFullTextQuery CreateFullTextQuery<TEntity>(string defaultField, string queryString)
        {
            using (new SessionIdLoggingContext(sessionImplementor.SessionId))
            {
                QueryParser queryParser = new QueryParser(defaultField, new StandardAnalyzer());
                Lucene.Net.Search.Query query = queryParser.Parse(queryString);
                return CreateFullTextQuery(query, typeof (TEntity));
            }
        }

        public IFullTextQuery CreateFullTextQuery<TEntity>(string queryString)
        {
            using (new SessionIdLoggingContext(sessionImplementor.SessionId))
            {
                QueryParser queryParser = new QueryParser(string.Empty, new StandardAnalyzer());
                Lucene.Net.Search.Query query = queryParser.Parse(queryString);
                return CreateFullTextQuery(query, typeof (TEntity));
            }
        }

        /// <summary>
        /// Execute a Lucene query and retrieve managed objects of type entities (or their indexed subclasses
        /// If entities is empty, include all indexed entities
        /// </summary>
        /// <param name="luceneQuery"></param>
        /// <param name="entities">entities must be immutable for the lifetime of the query object</param>
        /// <returns></returns>
        public IFullTextQuery CreateFullTextQuery(Lucene.Net.Search.Query luceneQuery, params System.Type[] entities)
        {
            using (new SessionIdLoggingContext(sessionImplementor.SessionId))
            {
                return new FullTextQueryImpl(luceneQuery, entities, session, null);
            }
        }

        /// <summary>
        /// (re)index an entity.
        /// Non indexable entities are ignored
        /// The entity must be associated with the session
        /// </summary>
        /// <param name="entity">The entity to index - must not be null</param>
        /// <returns></returns>
        public IFullTextSession Index(object entity)
        {
            using (new SessionIdLoggingContext(sessionImplementor.SessionId))
            {
                if (entity == null)
                {
                    return this;
                }

                System.Type clazz = NHibernateUtil.GetClass(entity);
                ISearchFactoryImplementor searchFactoryImplementor = SearchFactoryImplementor;

                // TODO: Cache that at the FTSession level
                // not strictly necesary but a small optmization
                DocumentBuilder builder = searchFactoryImplementor.DocumentBuilders[clazz];
                if (builder != null)
                {
                    object id = session.GetIdentifier(entity);
                    Work work = new Work(entity, id, WorkType.Index);
                    searchFactoryImplementor.Worker.PerformWork(work, eventSource);
                }

                return this;
            }
        }

        public void PurgeAll(System.Type clazz)
        {
            using (new SessionIdLoggingContext(sessionImplementor.SessionId))
            {
                Purge(clazz, null);
            }
        }

        public void Purge(System.Type clazz, object id)
        {
            using (new SessionIdLoggingContext(sessionImplementor.SessionId))
            {
                if (clazz == null)
                {
                    return;
                }

                ISearchFactoryImplementor searchFactoryImplementor = SearchFactoryImplementor;

                // TODO: Cache that at the FTSession level
                // not strictly necesary but a small optmization
                DocumentBuilder builder = searchFactoryImplementor.DocumentBuilders[clazz];
                if (builder != null)
                {
                    // TODO: Check to see this entity type is indexed
                    WorkType workType = id == null ? WorkType.PurgeAll : WorkType.Purge;
                    Work work = new Work(clazz, id, workType);
                    searchFactoryImplementor.Worker.PerformWork(work, eventSource);
                }
            }
        }

        public void Dispose()
        {
            session.Dispose();
        }

        #endregion
    }
}