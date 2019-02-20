﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using WeihanLi.Common.Models;
using WeihanLi.Extensions;

namespace WeihanLi.Common.Data
{
    public class Repository<TEntity> : IRepository<TEntity> where TEntity : new()
    {
        private static readonly Type EntityType = typeof(TEntity);

        private readonly string _tableName;
        protected readonly Lazy<DbConnection> _dbConnection;

        public Repository(Func<DbConnection> dbConnectionFunc)
        {
            _dbConnection = new Lazy<DbConnection>(dbConnectionFunc);

            _tableName = EntityType.IsDefined(typeof(TableAttribute))
                ? EntityType.GetCustomAttribute<TableAttribute>().Name
                : EntityType.Name;
        }

        public int Count(Expression<Func<TEntity, bool>> whereExpression)
        {
            var whereSql = SqlExpressionParser.ParseWhereExpression(whereExpression);

            var sql = $@"
SELECT COUNT(1) FROM {_tableName}
{whereSql.SqlText}
";
            return _dbConnection.Value.ExecuteScalarTo<int>(sql, whereSql.Parameters);
        }

        public Task<int> CountAsync(Expression<Func<TEntity, bool>> whereExpression)
        {
            var whereSql = SqlExpressionParser.ParseWhereExpression(whereExpression);

            var sql = $@"
SELECT COUNT(1) FROM {_tableName}
{whereSql.SqlText}
";
            return _dbConnection.Value.ExecuteScalarToAsync<int>(sql, whereSql.Parameters);
        }

        public virtual long LongCount(Expression<Func<TEntity, bool>> whereExpression)
        {
            var whereSql = SqlExpressionParser.ParseWhereExpression(whereExpression);

            var sql = $@"
SELECT COUNT(1) FROM {_tableName}
{whereSql.SqlText}
";
            return _dbConnection.Value.ExecuteScalarTo<long>(sql, whereSql.Parameters);
        }

        public virtual Task<long> LongCountAsync(Expression<Func<TEntity, bool>> whereExpression)
        {
            var whereSql = SqlExpressionParser.ParseWhereExpression(whereExpression);

            var sql = $@"
SELECT COUNT(1) FROM {_tableName}
{whereSql.SqlText}
";
            return _dbConnection.Value.ExecuteScalarToAsync<long>(sql, whereSql.Parameters);
        }

        public virtual bool Exist(Expression<Func<TEntity, bool>> whereExpression)
        {
            var whereSql = SqlExpressionParser.ParseWhereExpression(whereExpression);
            var sql = $@"SELECT IIF(EXISTS (SELECT TOP(1) 1 FROM {_tableName} {whereSql.SqlText}), 1, 0) AS BIT";
            return _dbConnection.Value.ExecuteScalarTo<bool>(sql, whereSql.Parameters);
        }

        public virtual Task<bool> ExistAsync(Expression<Func<TEntity, bool>> whereExpression)
        {
            var whereSql = SqlExpressionParser.ParseWhereExpression(whereExpression);
            var sql = $@"SELECT IIF(EXISTS (SELECT TOP(1) 1 FROM {_tableName} {whereSql.SqlText}), 1, 0) AS BIT";
            return _dbConnection.Value.ExecuteScalarToAsync<bool>(sql, whereSql.Parameters);
        }

        public virtual TEntity Fetch(Expression<Func<TEntity, bool>> whereExpression)
        {
            var whereSql = SqlExpressionParser.ParseWhereExpression(whereExpression);
            var sql = $@"
SELECT TOP 1 * FROM {_tableName}
{whereSql.SqlText}
";
            return _dbConnection.Value.Fetch<TEntity>(sql, whereSql.Parameters);
        }

        public virtual Task<TEntity> FetchAsync(Expression<Func<TEntity, bool>> whereExpression)
        {
            var whereSql = SqlExpressionParser.ParseWhereExpression(whereExpression);
            var sql = $@"
SELECT TOP 1 * FROM {_tableName}
{whereSql.SqlText}
";
            return _dbConnection.Value.FetchAsync<TEntity>(sql, whereSql.Parameters);
        }

        public virtual List<TEntity> Select(Expression<Func<TEntity, bool>> whereExpression)
        {
            var whereSql = SqlExpressionParser.ParseWhereExpression(whereExpression);
            var sql = $@"
SELECT * FROM {_tableName}
{whereSql.SqlText}
";
            return _dbConnection.Value.Select<TEntity>(sql, whereSql.Parameters).ToList();
        }

        public virtual Task<List<TEntity>> SelectAsync(Expression<Func<TEntity, bool>> whereExpression)
        {
            var whereSql = SqlExpressionParser.ParseWhereExpression(whereExpression);
            var sql = $@"
SELECT * FROM {_tableName}
{whereSql.SqlText}
";
            return _dbConnection.Value.SelectAsync<TEntity>(sql, whereSql.Parameters).ContinueWith(r => r.Result.ToList());
        }

        public List<TEntity> Select<TProperty>(int count, Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, TProperty>> orderByExpression, bool isAsc = false)
        {
            var whereSql = SqlExpressionParser.ParseWhereExpression(whereExpression);
            var sql = $@"
SELECT TOP 10 * FROM {_tableName}
{whereSql.SqlText}
ORDER BY {orderByExpression.GetMemberName()} {(isAsc ? "" : "DESC")}
";
            return _dbConnection.Value.Select<TEntity>(sql, whereSql.Parameters).ToList();
        }

        public Task<List<TEntity>> SelectAsync<TProperty>(int count, Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, TProperty>> orderByExpression, bool isAsc = false)
        {
            var whereSql = SqlExpressionParser.ParseWhereExpression(whereExpression);
            var sql = $@"
SELECT TOP 10 * FROM {_tableName}
{whereSql.SqlText}
ORDER BY {orderByExpression.GetMemberName()} {(isAsc ? "" : "DESC")}
";
            return _dbConnection.Value.SelectAsync<TEntity>(sql, whereSql.Parameters).ContinueWith(_ => _.Result.ToList());
        }

        public virtual PagedListModel<TEntity> Paged<TProperty>(int pageIndex, int pageSize, Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, TProperty>> orderByExpression, bool isAsc = false)
        {
            var whereSql = SqlExpressionParser.ParseWhereExpression(whereExpression);
            if (pageIndex <= 0)
            {
                pageIndex = 1;
            }
            if (pageSize <= 0)
            {
                pageSize = 10;
            }
            var sql = $@"
SELECT COUNT(1) FROM {_tableName}
{whereSql.SqlText}
";
            var total = _dbConnection.Value.ExecuteScalarTo<int>(sql, whereSql.Parameters);
            if (total == 0)
            {
                return new TEntity[0].ToPagedListModel(pageIndex, pageSize, 0);
            }

            var offset = (pageIndex - 1) * pageSize;

            sql = $@"
SELECT * FROM {_tableName}
{whereSql.SqlText}
ORDER BY {orderByExpression.GetMemberName()}{(isAsc ? "" : " DESC")}
OFFSET {offset} ROWS
FETCH NEXT {pageSize} ROWS ONLY
";

            return _dbConnection.Value.Select<TEntity>(sql, whereSql.Parameters).ToPagedListModel(pageIndex, pageSize, total);
        }

        public virtual async Task<PagedListModel<TEntity>> PagedAsync<TProperty>(int pageIndex, int pageSize, Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, TProperty>> orderByExpression, bool isAsc = false)
        {
            var whereSql = SqlExpressionParser.ParseWhereExpression(whereExpression);
            if (pageIndex <= 0)
            {
                pageIndex = 1;
            }
            if (pageSize <= 0)
            {
                pageSize = 10;
            }
            var sql = $@"
SELECT COUNT(1) FROM {_tableName}
{whereSql.SqlText}
";
            var total = await _dbConnection.Value.ExecuteScalarToAsync<int>(sql, whereSql.Parameters);
            if (total == 0)
            {
                return new TEntity[0].ToPagedListModel(pageIndex, pageSize, 0);
            }

            var offset = (pageIndex - 1) * pageSize;

            sql = $@"
SELECT * FROM {_tableName}
{whereSql.SqlText}
ORDER BY {orderByExpression.GetMemberName()}{(isAsc ? "" : " DESC")}
OFFSET {offset} ROWS
FETCH NEXT {pageSize} ROWS ONLY
";

            return (await _dbConnection.Value.SelectAsync<TEntity>(sql, whereSql.Parameters)).ToPagedListModel(pageIndex, pageSize, total);
        }

        public virtual int Insert(TEntity entity)
        {
            var fields = CacheUtil.TypePropertyCache.GetOrAdd(EntityType, t => t.GetProperties())
                .Where(p => !p.IsDefined(typeof(DatabaseGeneratedAttribute)) && !p.IsDefined(typeof(NotMappedAttribute)))
                .Select(_ => _.Name)
                .ToArray();

            var paramDictionary = new Dictionary<string, object>();
            var sqlBuilder = new StringBuilder($@"INSERT INTO {_tableName} ");
            sqlBuilder.AppendLine("(");
            sqlBuilder.AppendLine($"{fields.Select(_ => _).StringJoin($",{Environment.NewLine}")}");
            sqlBuilder.AppendLine(")");
            sqlBuilder.AppendLine("VALUES");
            sqlBuilder.AppendLine("(");
            sqlBuilder.AppendLine($"{fields.Select(_ => $"@{_}").StringJoin($",{Environment.NewLine}")}");
            sqlBuilder.AppendLine(")");
            foreach (var field in fields)
            {
                paramDictionary.Add($"{field}", entity.GetPropertyValue(field));
            }
            var sql = sqlBuilder.ToString();

            return _dbConnection.Value.Execute(sql, paramDictionary);
        }

        public virtual Task<int> InsertAsync(TEntity entity)
        {
            var fields = CacheUtil.TypePropertyCache.GetOrAdd(EntityType, t => t.GetProperties())
                .Where(p => !p.IsDefined(typeof(DatabaseGeneratedAttribute)) && !p.IsDefined(typeof(NotMappedAttribute)))
                .Select(_ => _.Name)
                .ToArray();
            var paramDictionary = new Dictionary<string, object>();

            var sqlBuilder = new StringBuilder($@"INSERT INTO {_tableName}");
            sqlBuilder.AppendLine("(");
            sqlBuilder.AppendLine($"{fields.Select(_ => _).StringJoin($",{Environment.NewLine}")}");
            sqlBuilder.AppendLine(")");
            sqlBuilder.AppendLine("VALUES (");
            sqlBuilder.AppendLine($"{fields.Select(_ => $"@{_}").StringJoin($",{Environment.NewLine}")}");
            foreach (var field in fields)
            {
                paramDictionary.Add($"{field}", entity.GetPropertyValue(field));
            }

            sqlBuilder.AppendLine(")");
            return _dbConnection.Value.ExecuteAsync(sqlBuilder.ToString(), paramDictionary);
        }

        public virtual int Insert(IEnumerable<TEntity> entities)
        {
            var count = entities?.Count() ?? 0;
            if (count == 0)
            {
                return 0;
            }
            if (count > 1000)
            {
                return -1; // too large, not supported
            }
            var fields = CacheUtil.TypePropertyCache.GetOrAdd(EntityType, t => t.GetProperties())
                .Where(p => !p.IsDefined(typeof(DatabaseGeneratedAttribute)) && !p.IsDefined(typeof(NotMappedAttribute)))
                .Select(_ => _.Name)
                .ToArray();
            var paramDictionary = new Dictionary<string, object>();
            var sqlBuilder = new StringBuilder($@"INSERT INTO {_tableName}");
            sqlBuilder.AppendLine("(");
            sqlBuilder.AppendLine($"{fields.Select(_ => _).StringJoin($",{Environment.NewLine}")}");
            sqlBuilder.AppendLine(")");
            sqlBuilder.AppendLine("VALUES");

            for (var i = 0; i < count; i++)
            {
                sqlBuilder.AppendLine();
                sqlBuilder.AppendLine("(");
                sqlBuilder.AppendLine($"{fields.Select(_ => $"@{_}_{i}").StringJoin($",{Environment.NewLine}")}");
                foreach (var field in fields)
                {
                    paramDictionary.Add($"{field}_{i}", EntityType.GetPropertyValue(field));
                }
                sqlBuilder.Append("),");
            }
            sqlBuilder.Remove(sqlBuilder.Length - 2, 1);

            return _dbConnection.Value.Execute(sqlBuilder.ToString(), paramDictionary);
        }

        public virtual Task<int> InsertAsync(IEnumerable<TEntity> entities)
        {
            var count = entities?.Count() ?? 0;
            if (count == 0)
            {
                return Task.FromResult(0);
            }
            if (count > 1000)
            {
                return Task.FromResult(-1); // too large, not supported
            }
            var fields = CacheUtil.TypePropertyCache.GetOrAdd(EntityType, t => t.GetProperties())
                .Where(p => !p.IsDefined(typeof(DatabaseGeneratedAttribute)) && !p.IsDefined(typeof(NotMappedAttribute)))
                .Select(_ => _.Name)
                .ToArray();
            var paramDictionary = new Dictionary<string, object>();
            var sqlBuilder = new StringBuilder($@"INSERT INTO {_tableName}");
            sqlBuilder.AppendLine("(");
            sqlBuilder.AppendLine($"{fields.Select(_ => _).StringJoin($",{Environment.NewLine}")}");
            sqlBuilder.AppendLine(")");
            sqlBuilder.AppendLine("VALUES");

            for (var i = 0; i < count; i++)
            {
                sqlBuilder.AppendLine();
                sqlBuilder.AppendLine("(");
                sqlBuilder.AppendLine($"{fields.Select(_ => $"@{_}_{i}").StringJoin($",{Environment.NewLine}")}");
                foreach (var field in fields)
                {
                    paramDictionary.Add($"{field}_{i}", EntityType.GetPropertyValue(field));
                }
                sqlBuilder.Append("),");
            }
            sqlBuilder.Remove(sqlBuilder.Length - 2, 1);

            return _dbConnection.Value.ExecuteAsync(sqlBuilder.ToString(), paramDictionary);
        }

        public virtual int Update<TProperty>(Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, TProperty>> propertyExpression, object value)
        {
            var whereSql = SqlExpressionParser.ParseWhereExpression(whereExpression);
            var propertyName = propertyExpression.GetMemberName();
            var sql = $@"
UPDATE {_tableName}
SET {propertyName} = @set_{propertyName}
{whereSql.SqlText}
";
            whereSql.Parameters.Add($"set_{propertyName}", value);
            return _dbConnection.Value.Execute(sql, whereSql.Parameters);
        }

        public virtual Task<int> UpdateAsync<TProperty>(Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, TProperty>> propertyExpression, object value)
        {
            var whereSql = SqlExpressionParser.ParseWhereExpression(whereExpression);
            var propertyName = propertyExpression.GetMemberName();
            var sql = $@"
UPDATE {_tableName}
SET {propertyName} = @set_{propertyName}
{whereSql.SqlText}
";
            whereSql.Parameters.Add($"set_{propertyName}", value);
            return _dbConnection.Value.ExecuteAsync(sql, whereSql.Parameters);
        }

        public virtual int Update(Expression<Func<TEntity, bool>> whereExpression, IDictionary<string, object> propertyValues)
        {
            if (propertyValues == null || propertyValues.Count == 0)
            {
                return 0;
            }
            var whereSql = SqlExpressionParser.ParseWhereExpression(whereExpression);
            var sql = $@"
UPDATE {_tableName}
SET {propertyValues.Keys.Select(p => $"{p}=@set_{p}").StringJoin($",{Environment.NewLine}")}
{whereSql.SqlText}
";
            foreach (var propertyValue in propertyValues)
            {
                whereSql.Parameters.Add($"set_{propertyValue.Key}", propertyValue.Value);
            }
            return _dbConnection.Value.Execute(sql, whereSql.Parameters);
        }

        public virtual Task<int> UpdateAsync(Expression<Func<TEntity, bool>> whereExpression, IDictionary<string, object> propertyValues)
        {
            if (propertyValues == null || propertyValues.Count == 0)
            {
                return Task.FromResult(0);
            }
            var whereSql = SqlExpressionParser.ParseWhereExpression(whereExpression);
            var sql = $@"
UPDATE {_tableName}
SET {propertyValues.Keys.Select(p => $"{p}=@set_{p}").StringJoin($",{Environment.NewLine}")}
{whereSql.SqlText}
";
            foreach (var propertyValue in propertyValues)
            {
                whereSql.Parameters.Add($"set_{propertyValue.Key}", propertyValue.Value);
            }
            return _dbConnection.Value.ExecuteAsync(sql, whereSql.Parameters);
        }

        public virtual int Delete(Expression<Func<TEntity, bool>> whereExpression)
        {
            var whereSql = SqlExpressionParser.ParseWhereExpression(whereExpression);
            var sql = $@"
DELETE FROM {_tableName}
{whereSql.SqlText}
";
            return _dbConnection.Value.Execute(sql, whereSql.Parameters);
        }

        public virtual Task<int> DeleteAsync(Expression<Func<TEntity, bool>> whereExpression)
        {
            var whereSql = SqlExpressionParser.ParseWhereExpression(whereExpression);
            var sql = $@"
DELETE FROM {_tableName}
{whereSql.SqlText}
";
            return _dbConnection.Value.ExecuteAsync(sql, whereSql.Parameters);
        }

        public virtual int Execute(string sqlStr, object param = null)
        => _dbConnection.Value.Execute(sqlStr, paramInfo: param);

        public virtual Task<int> ExecuteAsync(string sqlStr, object param = null)
        => _dbConnection.Value.ExecuteAsync(sqlStr, paramInfo: param);

        public virtual TResult ExecuteScalar<TResult>(string sqlStr, object param = null)

        => _dbConnection.Value.ExecuteScalarTo<TResult>(sqlStr, paramInfo: param);

        public virtual Task<TResult> ExecuteScalarAsync<TResult>(string sqlStr, object param = null)

        => _dbConnection.Value.ExecuteScalarToAsync<TResult>(sqlStr, paramInfo: param);
    }
}
