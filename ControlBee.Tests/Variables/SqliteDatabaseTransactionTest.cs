using System;
using System.IO;
using ControlBee.Models;
using ControlBee.Variables;
using JetBrains.Annotations;
using Xunit;
using Assert = Xunit.Assert;

namespace ControlBee.Tests.Variables;

[TestSubject(typeof(SqliteDatabase))]
public class SqliteDatabaseTransactionTest : IDisposable
{
    private readonly string _tempFolder;
    private readonly SystemConfigurations _config;
    private readonly SqliteDatabase _db;

    public SqliteDatabaseTransactionTest()
    {
        _tempFolder = Path.Combine(
            Path.GetTempPath(),
            "ControlBeeTest_" + Guid.NewGuid().ToString("N")
        );
        Directory.CreateDirectory(_tempFolder);
        _config = new SystemConfigurations { DataFolder = _tempFolder };
        _db = new SqliteDatabase(_config);
    }

    public void Dispose()
    {
        _db.Dispose();
        try
        {
            Directory.Delete(_tempFolder, recursive: true);
        }
        catch { }
    }

    [Fact]
    public void CommitPersistsWrites()
    {
        using (var tx = _db.BeginTransaction())
        {
            _db.WriteVariables(VariableScope.Local, "r1", "actor", "path", "\"v1\"");
            tx!.Commit();
        }

        var row = _db.Read("r1", "actor", "path");
        Assert.True(row.HasValue);
        Assert.Equal("\"v1\"", row!.Value.value);
    }

    [Fact]
    public void DisposeWithoutCommitRollsBackWrites()
    {
        using (var tx = _db.BeginTransaction())
        {
            _db.WriteVariables(VariableScope.Local, "r2", "actor", "path", "\"v2\"");
            // No commit — using-block dispose should roll back.
        }

        var row = _db.Read("r2", "actor", "path");
        Assert.False(row.HasValue);
    }

    [Fact]
    public void NestedBeginTransactionReturnsNoOpAndPreservesOuterTransaction()
    {
        using (var outer = _db.BeginTransaction())
        {
            _db.WriteVariables(VariableScope.Local, "r3", "actor", "path", "\"v3\"");

            using (var inner = _db.BeginTransaction())
            {
                _db.WriteVariables(VariableScope.Local, "r3", "actor", "path2", "\"v3b\"");
                inner!.Commit(); // No-op: outer still owns the real transaction.
            }

            // Outer not yet committed — neither row should be visible from a fresh read
            // on the *same* connection inside the transaction. (SQLite shows uncommitted
            // writes to its own connection, so we instead test final state after rollback.)
        }

        // Outer rolled back (we never called outer.Commit()) — both writes vanish.
        Assert.False(_db.Read("r3", "actor", "path").HasValue);
        Assert.False(_db.Read("r3", "actor", "path2").HasValue);
    }

    [Fact]
    public void WriteWithoutTransactionStillCommitsImmediately()
    {
        _db.WriteVariables(VariableScope.Local, "r4", "actor", "path", "\"v4\"");

        var row = _db.Read("r4", "actor", "path");
        Assert.True(row.HasValue);
        Assert.Equal("\"v4\"", row!.Value.value);
    }
}
