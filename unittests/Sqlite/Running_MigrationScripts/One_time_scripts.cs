﻿using Sqlite.TestInfrastructure;
using TestCommon.TestInfrastructure;

namespace Sqlite.Running_MigrationScripts;

[Collection(nameof(SqliteTestContainer))]
// ReSharper disable once InconsistentNaming
public class One_time_scripts : TestCommon.Generic.Running_MigrationScripts.One_time_scripts, IClassFixture<SimpleService>
{

    protected override IGrateTestContext Context { get; }

    protected override ITestOutputHelper TestOutput { get; }

    public One_time_scripts(SqliteTestContainer testContainer, SimpleService simpleService, ITestOutputHelper testOutput)
    {
        Context = new SqliteGrateTestContext(simpleService.ServiceProvider, testContainer);
        TestOutput = testOutput;
    }

}