using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using FluentAssertions;
using grate.Configuration;
using grate.Infrastructure;
using grate.Migration;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using NSubstitute;
using NUnit.Framework;

namespace grate.unittests.PostgreSQL
{
    [TestFixture]
    public class MigrationTable
    {
        private GrateConfiguration? _config;
        private static string? AdminConnectionString() => $"Host=localhost;Port={GrateTestContext.PostgreSql.Port};Database=postgres;Username=postgres;Password={GrateTestContext.PostgreSql.AdminPassword}";
        private static string? ConnectionString(string database) => $"Host=localhost;Port={GrateTestContext.PostgreSql.Port};Database={database};Username=postgres;Password={GrateTestContext.PostgreSql.AdminPassword}";

        [TestCase("ScriptsRun")]
        [TestCase("ScriptsRunErrors")]
        [TestCase("Version")]
        public async Task Is_created_if_it_does_not_exist(string tableName)
        {
            var db = "MonoBonoJono";
            var fullTableName = "grate.\"" + tableName + "\"";
            
            var knownFolders = KnownFolders.In(CreateRandomTempDirectory());

            await using (var migrator = GetMigrator(db, true, knownFolders))
            {
                await migrator.Migrate();
            }

            IEnumerable<string> scripts;
            string sql = $"SELECT modified_date FROM {fullTableName}";
            
            await using (var conn = new NpgsqlConnection(ConnectionString(db)))
            {
                scripts = await conn.QueryAsync<string>(sql);
            }
            scripts.Should().NotBeNull();
        }
        
        
        [TestCase("ScriptsRun")]
        [TestCase("ScriptsRunErrors")]
        [TestCase("Version")]
        public async Task Is_created_even_if_scripts_fail(string tableName)
        {
            var db = "DatabaseWithFailingScripts";
            var fullTableName = "grate.\"" + tableName + "\"";
            
            var knownFolders = KnownFolders.In(CreateRandomTempDirectory());
            CreateInvalidSql(knownFolders.Up);

            await using (var migrator = GetMigrator(db, true, knownFolders))
            {
                try
                {
                    await migrator.Migrate();
                }
                catch (NpgsqlException)
                {
                }
            }

            IEnumerable<string> scripts;
            string sql = $"SELECT modified_date FROM {fullTableName}";
            
            await using (var conn = new NpgsqlConnection(ConnectionString(db)))
            {
                scripts = await conn.QueryAsync<string>(sql);
            }
            scripts.Should().NotBeNull();
        }
        
        [TestCase("ScriptsRun")]
        [TestCase("ScriptsRunErrors")]
        [TestCase("Version")]
        public async Task Migration_does_not_fail_if_table_already_exists(string tableName)
        {
            var db = "MonoBonoJono";

            var knownFolders = KnownFolders.In(CreateRandomTempDirectory());
            
            await using (var migrator = GetMigrator(db, true, knownFolders))
            {
                await migrator.Migrate();
            }
            
            // Run migration again - make sure it does not throw an exception
            await using (var migrator = GetMigrator(db, true, knownFolders))
            {
                Assert.DoesNotThrowAsync(() => migrator.Migrate());
            }
        }
        
        [Test()]
        public async Task Inserts_version_in_version_table()
        {
            var db = "BooYaTribe";

            var knownFolders = KnownFolders.In(CreateRandomTempDirectory());
            
            await using (var migrator = GetMigrator(db, true, knownFolders))
            {
                await migrator.Migrate();
            }

            IEnumerable<string> entries;
            string sql = $"SELECT version FROM grate.\"Version\"";
            
            await using (var conn = new NpgsqlConnection(ConnectionString(db)))
            {
                entries = await conn.QueryAsync<string>(sql);
            }

            var versions = entries.ToList();
            versions.Should().HaveCount(1);
            versions.FirstOrDefault().Should().Be("a.b.c.d");
        }

   private GrateMigrator GetMigrator(string databaseName, bool createDatabase, KnownFolders knownFolders)
        {
            var connectionString = ConnectionString(databaseName);

            var dbLogger = new NullLogger<PostgreSqlDatabase>();
            var factory = Substitute.For<IFactory>();
            factory.GetService<DatabaseType, IDatabase>(DatabaseType.postgresql)
                .Returns(new PostgreSqlDatabase(dbLogger));

            var dbMigrator = new DbMigrator(factory, new NullLogger<DbMigrator>(), new HashGenerator());
            var migrator = new GrateMigrator(new NullLogger<GrateMigrator>(), dbMigrator);

            _config = new GrateConfiguration()
            {
                CreateDatabase = createDatabase, 
                ConnectionString = connectionString,
                AdminConnectionString = AdminConnectionString(),
                Version = "a.b.c.d",
                KnownFolders = knownFolders,
                AlterDatabase = true,
                NonInteractive = true,
                DatabaseType = DatabaseType.postgresql
            };


            dbMigrator.ApplyConfig(_config);

            return migrator;
        }

        private static DirectoryInfo CreateRandomTempDirectory()
        {
            var dummyFile = Path.GetTempFileName();
            File.Delete(dummyFile);

            var scriptsDir = Directory.CreateDirectory(dummyFile);
            return scriptsDir;
        }

        private static void CreateDummySql(MigrationsFolder? folder)
        {
            var dummySql = "SELECT @@VERSION";
            var path = MakeSurePathExists(folder);
            WriteSql(path, "1_jalla.sql", dummySql);
        }
        
        private static void CreateInvalidSql(MigrationsFolder? folder)
        {
            var dummySql = "SELECT TOP";
            var path = MakeSurePathExists(folder);
            WriteSql(path, "2_failing.sql", dummySql);
        }

        private static void WriteSql(DirectoryInfo path, string filename, string? sql)
        {
            File.WriteAllText(Path.Combine(path.ToString(), filename), sql);
        }

        private static DirectoryInfo MakeSurePathExists(MigrationsFolder? folder)
        {
            var path = folder?.Path ?? throw new ArgumentException(nameof(folder.Path));

            if (!path.Exists)
            {
                path.Create();
            }

            return path;
        }
        
    }
}