using Npgsql;
using Xunit;
using Xunit.Abstractions;

namespace Proy_Back_QBD.Tests;

public class UnitTest1
{
    private readonly ITestOutputHelper _output;

    public UnitTest1(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task CheckSchema()
    {
        var connString = Environment.GetEnvironmentVariable("QBD_TEST_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connString))
        {
            _output.WriteLine("Database smoke test skipped: QBD_TEST_CONNECTION_STRING is not set.");
            return;
        }

        using var conn = new NpgsqlConnection(connString);
        await conn.OpenAsync();

        using var cmd = new NpgsqlCommand("SELECT 'DB Connection OK' as msg;", conn);
        var result = await cmd.ExecuteScalarAsync();
        _output.WriteLine(result?.ToString() ?? "");
    }
}



