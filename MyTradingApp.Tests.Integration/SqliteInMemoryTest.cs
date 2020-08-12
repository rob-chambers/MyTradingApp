using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using MyTradingApp.Core.Persistence;
using System;
using System.Data.Common;

namespace MyTradingApp.Tests.Integration
{
    public abstract class SqliteInMemoryTest : EfCoreTest, IDisposable
    {
        private readonly DbConnection _connection;

        public SqliteInMemoryTest()
            : base(GetOptions())
        {            
            _connection = RelationalOptionsExtension.Extract(ContextOptions).Connection;
        }

        private static DbContextOptions<ApplicationContext> GetOptions()
        {
            return new DbContextOptionsBuilder<ApplicationContext>()
                            .UseSqlite(CreateInMemoryDatabase())
                            .Options;
        }

        private static DbConnection CreateInMemoryDatabase()
        {
            var connection = new SqliteConnection("Filename=:memory:");
            connection.Open();
            return connection;
        }

        public void Dispose() => _connection.Dispose();
    }
}
