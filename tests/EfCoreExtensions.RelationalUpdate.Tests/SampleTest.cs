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
    public class SampleTest
    {
        [Fact]
        public async Task Sample()
        {
            // Create Database
            var options = new DbContextOptionsBuilder<TestDbContext>();
            options.UseInMemoryDatabase("test");
            await using var context = new TestDbContext(options.Options);
            // Seed Data
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
            first.ChildEntities.FirstOrDefault().Name = "Updated Child Entity 2";
            first.ChildEntities.Add(new ChildEntity { Name = "Child Entity 3" });

            // Removed Child Entity 1 
            // Updated Child Entity 2
            // Added Child Entity 3

            await context.RelationalUpdateAsync(first);

            var newValue= await context.Entities.Include(p => p.ChildEntities).FirstOrDefaultAsync();


        }

    }
}
