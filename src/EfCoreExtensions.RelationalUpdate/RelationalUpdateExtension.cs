using System;
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
                .Select(p => new RelationalUpdateConfigurationType(GetFirstGenericArgument(p.Metadata.ClrType), true))
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
#pragma warning disable EF1001 // Internal EF Core API usage.
            return (IQueryable)((IDbSetCache)context).GetOrAddSet(context.GetDependencies().SetSource, entityType);
#pragma warning restore EF1001 // Internal EF Core API usage.
        }

        public static string GetPrimaryKeyName(this EntityEntry entry)
        {
            return entry.Metadata.FindPrimaryKey().Properties.FirstOrDefault()?.Name;
        }

        public static Type GetFirstGenericArgument(this Type type)
        {
            return type.GetGenericArguments()[0];
        }

        public static async ValueTask<int> RelationalUpdateAsync<T>(this DbContext context, T entity, RelationalUpdateConfiguration configuration) where T : class
        {
            var entry = context.Entry(entity);
            var primaryKey = entry.GetPrimaryKeyValue();
            var navigation = entry.Metadata.GetNavigations().ToList();

            foreach (var collectionType in configuration.UpdatedTypes)
            {
                var propertyName = navigation.Where(p => p.ClrType.GetFirstGenericArgument() == collectionType.Type).Select(p => p.Name).FirstOrDefault();
                if (string.IsNullOrEmpty(propertyName)) continue;
                var collection = entry.Collection(propertyName);

                var foreignKey = collection.Metadata.ForeignKey;
                var primaryKeyName = collection.EntityEntry.GetPrimaryKeyName();

                var collectionValues = (IEnumerable<dynamic>)entity.GetType().GetProperty(propertyName)?.GetValue(entity, null);
                var dynamicList = (collectionValues ?? throw new InvalidOperationException()).ToDynamicList();
                var currentIds = dynamicList.Select(p => p.GetType().GetProperty(primaryKeyName)?.GetValue(p, null))
                    .ToList();
             
                var fkName = foreignKey.Properties.FirstOrDefault()?.Name;
                var databaseValues = await context.Query(collectionType.Type)
                     .AsQueryable()
                     .Where($"{fkName} == @0", primaryKey)
                     .ToDynamicListAsync();
                var databaseIds = databaseValues
                    .Select(p => p.GetType().GetProperty(primaryKeyName)?.GetValue(p, null))
                    .ToList();
                if (collectionType.RemoveDataInDatabase)
                {
                    var deletedItems = databaseValues.Where(p =>
                            currentIds.Contains(p.GetType().GetProperty(primaryKeyName)?.GetValue(p, null)) == false)
                        .ToList();
                    context.RemoveRange(deletedItems);
                }
                foreach (dynamic o in dynamicList)
                {
                    var id = o.GetType().GetProperty(primaryKeyName)?.GetValue(o, null);
                    if (databaseIds.Contains(id))
                    {
                        context.Entry(o).State = EntityState.Modified;
                    }
                    else
                    {
                        o.GetType().GetProperty(fkName)?.SetValue(o, primaryKey, null);
                        context.Entry(o).State = EntityState.Added;
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