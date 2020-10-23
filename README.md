# EfCore.RelationalUpdate
This package provides relational update
# Usage
## No Configuration

```c#
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

```
## With Configuration
```c#
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

            // Not Removed Child Entity 1 
            // Updated Child Entity 2
            // Added Child Entity 3
            var configuration = new RelationalUpdateConfiguration();
            configuration.AddType(updatedType: typeof(ChildEntity), removeDataInDatabase: false);
            await context.RelationalUpdateAsync(first,configuration);

            var newValue = await context.Entities.Include(p => p.ChildEntities).FirstOrDefaultAsync();



```
