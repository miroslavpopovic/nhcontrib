﻿using System;
using System.Collections;
using System.Collections.Generic;
using Antlr.Runtime;
using Antlr.Runtime.Tree;
using Iesi.Collections.Generic;
using log4net;
using NHibernate.Engine;
using NHibernate.Hql.Ast.ANTLR.Exec;
using NHibernate.Hql.Ast.ANTLR.Loader;
using NHibernate.Hql.Ast.ANTLR.Tree;
using NHibernate.Hql.Ast.ANTLR.Util;
using NHibernate.Param;
using NHibernate.Persister.Entity;
using NHibernate.SqlCommand;
using NHibernate.Type;
using NHibernate.Util;

namespace NHibernate.Hql.Ast.ANTLR
{
	[CLSCompliant(false)]
	public class QueryTranslatorImpl : IFilterTranslator
	{
		private static readonly ILog log = LogManager.GetLogger(typeof(QueryTranslatorImpl));

		private bool _shallowQuery;
		private bool _compiled;
		private readonly string _queryIdentifier;
		private readonly string _hql;
		private IDictionary<string, IFilter> _enabledFilters;
		private readonly ISessionFactoryImplementor _factory;
		private QueryLoader _queryLoader;
		private IStatementExecutor statementExecutor;
		private IStatement sqlAst;
		private ParameterTranslationsImpl _paramTranslations;
		private IDictionary<string, string> tokenReplacements;
		private HqlParseEngine _parser;
		private HqlSqlGenerator _generator;

		/// <summary>
		/// Creates a new AST-based query translator.
		/// </summary>
		/// <param name="queryIdentifier">The query-identifier (used in stats collection)</param>
		/// <param name="query">The hql query to translate</param>
		/// <param name="enabledFilters">Currently enabled filters</param>
		/// <param name="factory">The session factory constructing this translator instance.</param>
		public QueryTranslatorImpl(
				string queryIdentifier,
				string query,
				IDictionary<string, IFilter> enabledFilters,
				ISessionFactoryImplementor factory)
		{
			_queryIdentifier = queryIdentifier;
			_hql = query;
			_compiled = false;
			_shallowQuery = false;
			_enabledFilters = enabledFilters;
			_factory = factory;
		}

		/// <summary>
		/// Compile a "normal" query. This method may be called multiple
		/// times. Subsequent invocations are no-ops.
		/// </summary>
		/// <param name="replacements">Defined query substitutions.</param>
		/// <param name="shallow">Does this represent a shallow (scalar or entity-id) select?</param>
		public void Compile(IDictionary<string, string> replacements, bool shallow)
		{
			DoCompile( replacements, shallow, null );
		}

		public IList List(ISessionImplementor session, QueryParameters queryParameters)
		{
			// Delegate to the QueryLoader...
			ErrorIfDML();
			var query = ( QueryNode ) sqlAst;
			bool hasLimit = queryParameters.RowSelection != null && queryParameters.RowSelection.DefinesLimits;
			bool needsDistincting = ( query.GetSelectClause().IsDistinct || hasLimit ) && ContainsCollectionFetches;

			QueryParameters queryParametersToUse;

			if ( hasLimit && ContainsCollectionFetches ) 
			{
				log.Warn( "firstResult/maxResults specified with collection fetch; applying in memory!" );
				var selection = new RowSelection
				                         	{
				                         		FetchSize = queryParameters.RowSelection.FetchSize,
				                         		Timeout = queryParameters.RowSelection.Timeout
				                         	};
				queryParametersToUse = queryParameters.CreateCopyUsing( selection );
			}
			else 
			{
				queryParametersToUse = queryParameters;
			}

			IList results = _queryLoader.List(session, queryParametersToUse);

			if ( needsDistincting ) 
			{
				int includedCount = -1;
				// NOTE : firstRow is zero-based
				int first = !hasLimit || queryParameters.RowSelection.FirstRow == RowSelection.NoValue
							? 0
							: queryParameters.RowSelection.FirstRow;
				int max = !hasLimit || queryParameters.RowSelection.MaxRows == RowSelection.NoValue
							? -1
							: queryParameters.RowSelection.MaxRows;

				int size = results.Count;
				var tmp = new List<object>();
				var distinction = new IdentitySet();

				for ( int i = 0; i < size; i++ ) 
				{
					object result = results[i];
					if ( !distinction.Add(result ) ) 
					{
						continue;
					}
					includedCount++;
					if ( includedCount < first ) 
					{
						continue;
					}
					tmp.Add( result );
					// NOTE : ( max - 1 ) because first is zero-based while max is not...
					if ( max >= 0 && ( includedCount - first ) >= ( max - 1 ) ) 
					{
						break;
					}
				}

				results = tmp;
			}

			return results;
		}

		public IEnumerable GetEnumerable(QueryParameters queryParameters, ISessionImplementor session)
		{
			ErrorIfDML();
			return _queryLoader.GetEnumerable(queryParameters, session);
		}

		public int ExecuteUpdate(QueryParameters queryParameters, ISessionImplementor session)
		{
			ErrorIfSelect();
			return statementExecutor.Execute(queryParameters, session);
		}

		private void ErrorIfSelect()
		{
			if (!sqlAst.NeedsExecutor)
			{
				throw new QueryExecutionRequestException("Not supported for select queries:", _hql);
			}
		}

		public NHibernate.Loader.Loader Loader
		{
			get { return _queryLoader; }
		}

		public virtual IType[] ActualReturnTypes
		{
			get { return _queryLoader.ReturnTypes; }
		}

		public string[][] GetColumnNames()
		{
			ErrorIfDML();
			return sqlAst.Walker.SelectClause.ColumnNames;
		}

		public IParameterTranslations GetParameterTranslations()
		{
			if (_paramTranslations == null)
			{
				_paramTranslations = new ParameterTranslationsImpl(sqlAst.Walker.Parameters);
			}

			return _paramTranslations;
		}

		public ISet<string> QuerySpaces
		{
			get { return sqlAst.Walker.QuerySpaces; }
		}

		public string SQLString
		{
			get { return _generator.Sql.ToString(); }
		}

		public IStatement SqlAST
		{
			get { return sqlAst; }
		}

		public IList<IParameterSpecification> CollectedParameterSpecifications
		{
			get { return _generator.CollectionParameters; }
		}

		public SqlString SqlString
		{
			get { return _generator.Sql; }
		}

		public string QueryIdentifier
		{
			get { return _queryIdentifier; }
		}

		public IList<string> CollectSqlStrings
		{
			get
			{
				var list = new List<string>();
				if (IsManipulationStatement)
				{
					foreach (var sqlStatement in statementExecutor.SqlStatements)
					{
						if (sqlStatement != null)
						{
							list.Add(sqlStatement.ToString());
						}
					}
				}
				else
				{
					list.Add(_generator.Sql.ToString());
				}
				return list;
			}
		}

		public string QueryString
		{
			get { return _hql; }
		}

		public IDictionary<string, IFilter> EnabledFilters
		{
			get { return _enabledFilters; }
		}

		public IType[] ReturnTypes
		{
			get
			{
				ErrorIfDML();
				return sqlAst.Walker.ReturnTypes;
			}
		}

		public string[] ReturnAliases
		{
			get
			{
				ErrorIfDML();
				return sqlAst.Walker.ReturnAliases;
			}
		}

		public bool ContainsCollectionFetches
		{
			get
			{
				ErrorIfDML();
				IList<IASTNode> collectionFetches = ((QueryNode)sqlAst).FromClause.GetCollectionFetches();
				return collectionFetches != null && collectionFetches.Count > 0;
			}
		}

		public bool IsManipulationStatement
		{
			get { return sqlAst.NeedsExecutor; }
		}

		/// <summary>
		/// Compile a filter. This method may be called multiple
		/// times. Subsequent invocations are no-ops.
		/// </summary>
		/// <param name="collectionRole">the role name of the collection used as the basis for the filter.</param>
		/// <param name="replacements">Defined query substitutions.</param>
		/// <param name="shallow">Does this represent a shallow (scalar or entity-id) select?</param>
		public void Compile(string collectionRole, IDictionary<string, string> replacements, bool shallow)
		{
			DoCompile(replacements, shallow, collectionRole);
		}

		public bool IsShallowQuery
		{
			get { return _shallowQuery; }
		}

		/// <summary>
		/// Performs both filter and non-filter compiling.
		/// </summary>
		/// <param name="replacements">Defined query substitutions.</param>
		/// <param name="shallow">Does this represent a shallow (scalar or entity-id) select?</param>
		/// <param name="collectionRole">the role name of the collection used as the basis for the filter, NULL if this is not a filter.</param>
		private void DoCompile(IDictionary<string, string> replacements, bool shallow, String collectionRole) 
		{
			// If the query is already compiled, skip the compilation.
			if ( _compiled ) 
			{
				if ( log.IsDebugEnabled ) 
				{
					log.Debug( "compile() : The query is already compiled, skipping..." );
				}
				return;
			}

			// Remember the parameters for the compilation.
			tokenReplacements = replacements ?? new Dictionary<string, string>(1);

			_shallowQuery = shallow;

			try 
			{
				// PHASE 1 : Parse the HQL into an AST.
				HqlParseEngine parser = Parse(true);

			    // PHASE 2 : Analyze the HQL AST, and produce an SQL AST.
				var translator = Analyze(parser, collectionRole);

				sqlAst = translator.SqlStatement;

				// at some point the generate phase needs to be moved out of here,
				// because a single object-level DML might spawn multiple SQL DML
				// command executions.
				//
				// Possible to just move the sql generation for dml stuff, but for
				// consistency-sake probably best to just move responsiblity for
				// the generation phase completely into the delegates
				// (QueryLoader/StatementExecutor) themselves.  Also, not sure why
				// QueryLoader currently even has a dependency on this at all; does
				// it need it?  Ideally like to see the walker itself given to the delegates directly...

				if (sqlAst.NeedsExecutor) 
				{
					statementExecutor = BuildAppropriateStatementExecutor(sqlAst);
				}
				else 
				{
					// PHASE 3 : Generate the SQL.
					_generator = new HqlSqlGenerator(sqlAst, parser.Tokens, _factory);
					_generator.Generate();

					_queryLoader = new QueryLoader(this, _factory, sqlAst.Walker.SelectClause);
				}

				_compiled = true;
			}
			catch ( QueryException qe ) 
			{
				qe.QueryString = _hql;
				throw;
			}
			catch ( RecognitionException e ) 
			{
				// we do not actually propogate ANTLRExceptions as a cause, so
				// log it here for diagnostic purposes
				if ( log.IsInfoEnabled ) 
				{
					log.Info( "converted antlr.RecognitionException", e );
				}
				throw QuerySyntaxException.Convert( e, _hql );
			}

			_enabledFilters = null; //only needed during compilation phase...
		}

		private IStatementExecutor BuildAppropriateStatementExecutor(IStatement statement)
		{
			HqlSqlWalker walker = statement.Walker;
			if (walker.StatementType == HqlSqlWalker.DELETE)
			{
				FromElement fromElement = walker.GetFinalFromClause().GetFromElement();
				IQueryable persister = fromElement.Queryable;
				if (persister.IsMultiTable)
				{
					return new MultiTableDeleteExecutor(statement);
				}
				else
				{
					return new BasicExecutor(statement, persister);
				}
			}
			else if (walker.StatementType == HqlSqlWalker.UPDATE)
			{
				FromElement fromElement = walker.GetFinalFromClause().GetFromElement();
				IQueryable persister = fromElement.Queryable;
				if (persister.IsMultiTable)
				{
					// even here, if only properties mapped to the "base table" are referenced
					// in the set and where clauses, this could be handled by the BasicDelegate.
					// TODO : decide if it is better performance-wise to perform that check, or to simply use the MultiTableUpdateDelegate
					return new MultiTableUpdateExecutor(statement);
				}
				else
				{
					return new BasicExecutor(statement, persister);
				}
			}
			else if (walker.StatementType == HqlSqlWalker.INSERT)
			{
				return new BasicExecutor(statement, ((InsertStatement)statement).IntoClause.Queryable);
			}
			else
			{
				throw new QueryException("Unexpected statement type");
			}
		}

		private HqlSqlTranslator Analyze(HqlParseEngine parser, string collectionRole)
		{
			var translator = new HqlSqlTranslator(parser.Ast, parser.Tokens, this, _factory, tokenReplacements,
																								 collectionRole);
			translator.Translate();

			return translator;
		}

		private HqlParseEngine Parse(bool isFilter)
		{
			if (_parser == null)
			{
				_parser = new HqlParseEngine(_hql, isFilter, _factory);
				_parser.Parse();
			}
			return _parser;
		}

		private void ErrorIfDML()
		{
			if (sqlAst.NeedsExecutor)
			{
				throw new QueryExecutionRequestException("Not supported for DML operations", _hql);
			}
		}
	}

	internal class HqlParseEngine
	{
		private static readonly ILog log = LogManager.GetLogger(typeof(HqlParseEngine));

		private readonly string _hql;
		private CommonTokenStream _tokens;
		private readonly bool _filter;
		private IASTNode _ast;
		private readonly ISessionFactoryImplementor _sfi;

		public HqlParseEngine(string hql, bool filter, ISessionFactoryImplementor sfi)
		{
			_hql = hql;
			_filter = filter;
			_sfi = sfi;
		}

		public HqlParseEngine(IASTNode ast, ISessionFactoryImplementor sfi)
		{
			_sfi = sfi;
			_ast = ast;
		}

		public IASTNode Ast
		{
			get { return _ast; }
		}

		public CommonTokenStream Tokens
		{
			get { return _tokens; }
		}

		public void Parse()
		{
			if (_ast == null)
			{
				// Parse the query string into an HQL AST.
				var lex = new HqlLexer(new CaseInsensitiveStringStream(_hql));
				_tokens = new CommonTokenStream(lex);

				var parser = new HqlParser(_tokens);
				parser.TreeAdaptor = new ASTTreeAdaptor();

				parser.Filter = _filter;

				if (log.IsDebugEnabled)
				{
					log.Debug("parse() - HQL: " + _hql);
				}

            try
            {
               _ast = (IASTNode) parser.statement().Tree;

               var walker = new NodeTraverser(new ConstantConverter(_sfi));
               walker.TraverseDepthFirst(_ast);

               //showHqlAst( hqlAst );
            }
            finally
            {
               parser.ParseErrorHandler.ThrowQueryException();
            }
			}
		}

		class ConstantConverter : IVisitationStrategy
		{
			private IASTNode dotRoot;
			private ISessionFactoryImplementor _sfi;

			public ConstantConverter(ISessionFactoryImplementor sfi)
			{
				_sfi = sfi;
			}

			public void Visit(IASTNode node)
			{
				if (dotRoot != null)
				{
					// we are already processing a dot-structure
					if (ASTUtil.IsSubtreeChild(dotRoot, node))
					{
						// ignore it...
						return;
					}

					// we are now at a new tree level
					dotRoot = null;
				}

				if (dotRoot == null && node.Type == HqlSqlWalker.DOT)
				{
					dotRoot = node;
					HandleDotStructure(dotRoot);
				}
			}

			private void HandleDotStructure(IASTNode dotStructureRoot)
			{
				String expression = ASTUtil.GetPathText(dotStructureRoot);

				object constant = ReflectHelper.GetConstantValue(expression, _sfi);

				if (constant != null)
				{
					dotStructureRoot.ClearChildren();
					dotStructureRoot.Type = HqlSqlWalker.JAVA_CONSTANT;
					dotStructureRoot.Text = expression;
				}
			}
		}
	}

	internal class HqlSqlTranslator
	{
		private readonly IASTNode _inputAst;
		private readonly CommonTokenStream _tokens;
		private readonly QueryTranslatorImpl _qti;
		private readonly ISessionFactoryImplementor _sfi;
		private readonly IDictionary<string, string> _tokenReplacements;
		private readonly string _collectionRole;
		private IStatement _resultAst;

		public HqlSqlTranslator(IASTNode ast, CommonTokenStream tokens, QueryTranslatorImpl qti, ISessionFactoryImplementor sfi, IDictionary<string, string> tokenReplacements, string collectionRole)
		{
			_inputAst = ast;
			_tokens = tokens;
			_qti = qti;
			_sfi = sfi;
			_tokenReplacements = tokenReplacements;
			_collectionRole = collectionRole;
		}

		public IStatement SqlStatement
		{
			get { return _resultAst; }
		}

		public IStatement Translate()
		{
			if (_resultAst == null)
			{
				var nodes = new HqlSqlWalkerTreeNodeStream(_inputAst);
				nodes.TokenStream = _tokens;

				var hqlSqlWalker = new HqlSqlWalker(_qti, _sfi, nodes, _tokenReplacements, _collectionRole);
				hqlSqlWalker.TreeAdaptor = new HqlSqlWalkerTreeAdaptor(hqlSqlWalker);

            try
            {
               // Transform the tree.
               _resultAst = (IStatement) hqlSqlWalker.statement().Tree;

               /*
               if ( AST_LOG.isDebugEnabled() ) {
                  ASTPrinter printer = new ASTPrinter( SqlTokenTypes.class );
                  AST_LOG.debug( printer.showAsString( w.getAST(), "--- SQL AST ---" ) );
               }
               */
            }
            finally
            {
               hqlSqlWalker.ParseErrorHandler.ThrowQueryException();
            }
			}

			return _resultAst;
		}
	}

	internal class HqlSqlGenerator
	{
		private static readonly ILog log = LogManager.GetLogger(typeof(HqlSqlGenerator));

		private readonly IASTNode _ast;
		private readonly ITokenStream _tokens;
		private readonly ISessionFactoryImplementor _sfi;
		private SqlString _sql;
		private IList<IParameterSpecification> _parameters;

		public HqlSqlGenerator(IStatement ast, ITokenStream tokens, ISessionFactoryImplementor sfi)
		{
			_ast = (IASTNode)ast;
			_tokens = tokens;
			_sfi = sfi;
		}

		public SqlString Sql
		{
			get { return _sql; }
		}

		public IList<IParameterSpecification> CollectionParameters
		{
			get { return _parameters; }
		}

		public SqlString Generate()
		{
			if (_sql == null)
			{
				var nodes = new CommonTreeNodeStream(_ast);
				nodes.TokenStream = _tokens;

				var gen = new SqlGenerator(_sfi, nodes);
				//gen.TreeAdaptor = new ASTTreeAdaptor();

            try
            {
               gen.statement();

               _sql = gen.GetSQL();

               if (log.IsDebugEnabled)
               {
                  log.Debug("SQL: " + _sql);
               }
            }
            finally
            {
               gen.ParseErrorHandler.ThrowQueryException();
            }

				_parameters = gen.GetCollectedParameters();
			}

			return _sql;
		}
	}
}
