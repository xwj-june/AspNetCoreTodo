using System;
using System.Linq;
using System.Threading.Tasks;
using AspNetCoreTodo.Data;
using AspNetCoreTodo.Models;
using AspNetCoreTodo.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AspNetCoreTodo.UnitTests
{
    public class TodoItemServiceShould
    {
        [Fact]
        public async Task AddNewItemAsIncompleteWithDueDate()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "Test_AddNewItem").Options;

            // Set up a context (connection to the "DB") for writing
            using (var context = new ApplicationDbContext(options))
            {
                var service = new TodoItemService(context);

                var fakeUser = new IdentityUser
                {
                    Id = "fake-000",
                    UserName = "fake@example.com"
                };

                await service.AddItemAsync(new TodoItem
                {
                    Title = "Testing?"
                }, fakeUser);
            }

            using (var context = new ApplicationDbContext(options))
            {
                var itemsInDatabase = await context
                    .Items.CountAsync();
                Assert.Equal(1, itemsInDatabase);

                var item = await context.Items.FirstAsync();
                Assert.Equal("Testing?", item.Title);
                Assert.Equal(false, item.IsDone);

                // Item should be due 3 days from now (give or take a second)
                var difference = DateTimeOffset.Now.AddDays(3) - item.DueAt;
                Assert.True(difference < TimeSpan.FromSeconds(1));
            }
        }

        [Fact]
        public async Task MakrDoneAsync()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "Test_AddNewItem").Options;

            // Set up a context (connection to the "DB") for writing
            //using (var context = new ApplicationDbContext(options))
            //{
            //    var service = new TodoItemService(context);
            //}

            using (var context = new ApplicationDbContext(options))
            {
                var service = new TodoItemService(context);
                var fakeUser = new IdentityUser
                {
                    Id = "fake-001",
                    UserName = "fake01@example.com"
                };
                context.Users.Add(fakeUser);

                var fakeItem = new TodoItem
                {
                    Id = Guid.NewGuid(),
                    Title = "fake item",
                    UserId = fakeUser.Id,
                    IsDone = false
                };
                context.Items.Add(fakeItem);
                await context.SaveChangesAsync();

                var currentItems = await context.Items.Where(i => i.UserId==fakeUser.Id).ToArrayAsync(); ;
                var expectedItem = await service.GetIncompleteItemsAsync(fakeUser);
                Assert.Equal(currentItems, expectedItem);


                var result = await service.MarkDoneAsync(Guid.NewGuid(), fakeUser);
                var result2 = await service.MarkDoneAsync(fakeItem.Id, fakeUser);

                Assert.Equal(false, result);
                Assert.Equal(true, result2);
            }
        }
    }
}