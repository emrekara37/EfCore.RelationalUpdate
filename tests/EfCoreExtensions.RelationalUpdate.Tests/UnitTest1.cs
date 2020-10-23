using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EfCoreExtensions.RelationalUpdate.Tests
{
    public class Entity
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<ChildEntity> ChildEntities { get; set; }

    }

    public class ChildEntity
    {
        public int Id { get; set; }
        public int EntityId { get; set; }
        [ForeignKey(nameof(EntityId))]
        public Entity Entity { get; set; }

        public string Name { get; set; }
    }
    public class TestDbContext : DbContext
    {
        public DbSet<Entity> Entities { get; set; }
        public DbSet<ChildEntity> ChildEntities { get; set; }
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
        {

        }

    }
    public class UnitTest1
    {
        [Fact]
        public async Task Test1()
        {
            var options = new DbContextOptionsBuilder<TestDbContext>();
            options.UseInMemoryDatabase("test");
            await using var context = new TestDbContext(options.Options);
            await context.Entities.AddAsync(new Entity
            {
                Name = "Entity ",
                ChildEntities = new List<ChildEntity>
                {
                    new ChildEntity
                    {
                        Name = "Child Entity 1"
                    },
                    new ChildEntity
                    {
                        Name = "Child Entity 2"
                    }
                }
            });
            await context.SaveChangesAsync();


            // Let's get started
            var first = await context.Entities
                .Where(c => c.ChildEntities.Any(i => i.Id > 1))
                .Select(c => new Entity
                {
                    Id = c.Id,
                    Name = c.Name,
                    ChildEntities = c.ChildEntities.Where(x => x.Id > 1).ToList()
                })
                .FirstOrDefaultAsync();

            first.ChildEntities.Add(new ChildEntity { Name = "Child Entity 3" });

            try
            {
                var configuration = new RelationalUpdateConfiguration(true);
                configuration.AddType(typeof(ChildEntity), true);

                await context.RelationalUpdateAsync(first, configuration);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            
            var a = await context.Entities.Include(p => p.ChildEntities).FirstOrDefaultAsync();

        }

    }
}
