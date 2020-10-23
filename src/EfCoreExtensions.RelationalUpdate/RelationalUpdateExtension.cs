﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Internal;

namespace EfCoreExtensions.RelationalUpdate
{
    public static class RelationalUpdateExtension
    {
        public static Type GetFirstGenericArgumentsType(this Type type)
        {
            return type.GetGenericArguments()[0];
        }
        public static object GetDefaultValue(this Type t)
        {
            if (t.IsValueType && Nullable.GetUnderlyingType(t) == null)
                return Activator.CreateInstance(t);
            return null;
        }
        private static List<RelationalUpdateConfigurationType> GetCollectionTypes(this EntityEntry entry)
        {
            var collections = entry.Collections;

            return collections
                .Select(p => new RelationalUpdateConfigurationType(GetFirstGenericArgumentsType(p.Metadata.ClrType), true))
                .ToList();
        }

        public static ValueTask<int> RelationalUpdateAsync<T>(this DbContext context, T entity) where T : class
        {
            var configuration = new RelationalUpdateConfiguration
            {
                UpdatedTypes = context.Entry(entity).GetCollectionTypes()
            };
            return RelationalUpdateAsync(context, entity, configuration);
        }

        public static object GetPrimaryKeyValue(this EntityEntry entry)
        {
            return entry.Metadata.FindPrimaryKey().Properties.Select(p => entry.Property(p.Name).CurrentValue)
                .FirstOrDefault();
        }
        public static IQueryable Query(this DbContext context, string entityName) =>
            context.Query(context.Model.FindEntityType(entityName).ClrType);

        public static IQueryable Query(this DbContext context, Type entityType)
        {
            return (IQueryable)((IDbSetCache)context).GetOrAddSet(context.GetDependencies().SetSource, entityType);
        }

        public static string GetPrimaryKeyName(this EntityEntry entry)
        {
            return entry.Metadata.FindPrimaryKey().Properties.FirstOrDefault()?.Name;
        }

        public static async ValueTask<int> RelationalUpdateAsync<T>(this DbContext context, T entity, RelationalUpdateConfiguration configuration) where T : class
        {
            var entry = context.Entry(entity);
            var primaryKey = entry.GetPrimaryKeyValue();
            var navigation = entry.Metadata.GetNavigations().ToList();

            foreach (var collectionType in configuration.UpdatedTypes)
            {
                var propertyName = navigation.Where(p => p.ClrType.GetGenericArguments()[0] == collectionType.Type).Select(p => p.Name).FirstOrDefault();
                if (string.IsNullOrEmpty(propertyName)) continue;
                var collection = entry.Collection(propertyName);

                var foreignKey = collection.Metadata.ForeignKey;
                var primaryKeyName = collection.EntityEntry.GetPrimaryKeyName();

                var dynamicList = (IEnumerable<dynamic>)entity.GetType().GetProperty(propertyName)?.GetValue(entity, null);
                var primaryKeyList = (dynamicList ?? throw new NullReferenceException())
                    .Select(p => p.GetType().GetProperty(primaryKeyName)?.GetValue(p, null))
                    .Where(p => p != null && p != GetDefaultValue(p.GetType()))
                    .ToList();

                var fkName = foreignKey.Properties.FirstOrDefault()?.Name;
                if (!collectionType.RemoveDataInDatabase) continue;
                //await collection.Query()
                //     .Where($"{fkName} == @0", primaryKey)
                //     .Where($"p=> @0.Contains(p.{primaryKeyName}) == false", primaryKeyList)
                //     .ToDynamicListAsync();
                var databaseValues = await context.Query(collectionType.Type)
                     .AsQueryable()
                     .Where($"{fkName} == @0", primaryKey)
                     .ToDynamicListAsync();

                foreach (dynamic o in databaseValues)
                {
                    var id = o.GetType().GetProperty(primaryKeyName)?.GetValue(o, null);
                    if (primaryKeyList.Contains(id))
                    {
                        context.Entry(o).State = EntityState.Detached;
                    }
                    else
                    {
                        context.Remove(o);
                    }
                }

            }
            if (configuration.TriggerSaveChanges)
            {
                return await context.SaveChangesAsync();
            }

            return 0;
        }
    }
}