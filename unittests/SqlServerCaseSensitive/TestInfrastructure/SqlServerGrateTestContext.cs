﻿using System.Data.Common;
using System.Runtime.InteropServices;
using grate.Configuration;
using grate.Infrastructure;
using grate.Migration;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TestCommon.TestInfrastructure;
using static System.Runtime.InteropServices.Architecture;

namespace SqlServerCaseSensitive.TestInfrastructure;

class SqlServerGrateTestContext : IGrateTestContext
{
    public IServiceProvider ServiceProvider { get; private set; }
    private readonly SqlServerTestContainer _testContainer;
    public SqlServerGrateTestContext(string serverCollation, IServiceProvider serviceProvider, SqlServerTestContainer container)
    {
        ServiceProvider = serviceProvider;
        _testContainer = container;
        ServerCollation = serverCollation;
    }
    public SqlServerGrateTestContext(IServiceProvider serviceProvider, SqlServerTestContainer container) : this("Danish_Norwegian_CI_AS", serviceProvider, container)
    {
    }
    public string AdminPassword => _testContainer.AdminPassword;
    public int? Port => _testContainer.TestContainer!.GetMappedPublicPort(_testContainer.Port);

    public string AdminConnectionString => $"Data Source=localhost,{Port};Initial Catalog=master;User Id=sa;Password={AdminPassword};Encrypt=false;Pooling=false";
    public string ConnectionString(string database) => $"Data Source=localhost,{Port};Initial Catalog={database};User Id=sa;Password={AdminPassword};Encrypt=false;Pooling=false";
    public string UserConnectionString(string database) => $"Data Source=localhost,{Port};Initial Catalog={database};User Id=sa;Password={AdminPassword};Encrypt=false;Pooling=false";

    public DbConnection GetDbConnection(string connectionString) => new SqlConnection(connectionString);

    public ISyntax Syntax => new SqlServerSyntax();
    public Type DbExceptionType => typeof(SqlException);

    public DatabaseType DatabaseType => DatabaseType.sqlserver;
    public bool SupportsTransaction => true;
    public string DatabaseTypeName => "SQL server";
    public string MasterDatabase => "master";

    public IDatabase DatabaseMigrator => new SqlServerDatabase(ServiceProvider.GetRequiredService<ILogger<SqlServerDatabase>>());

    public SqlStatements Sql => new()
    {
        SelectVersion = "SELECT @@VERSION",
        SleepTwoSeconds = "WAITFOR DELAY '00:00:02'"
    };

    public string ExpectedVersionPrefix => RuntimeInformation.ProcessArchitecture switch
    {
        Arm64 => "Microsoft Azure SQL Edge Developer",
        X64 => "Microsoft SQL Server 2019",
        var other => throw new PlatformNotSupportedException("Unsupported platform for running tests: " + other)
    };

    public bool SupportsCreateDatabase => true;

    public string ServerCollation { get; }
}