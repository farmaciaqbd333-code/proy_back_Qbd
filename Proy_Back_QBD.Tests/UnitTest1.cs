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
        var connString = "Host=dpg-d3vb7hur433s73coig6g-a.virginia-postgres.render.com;Port=5432;Database=qbdfarmacia_db;Username=franklinqbd;Password=E4ur6OIbe5R9djYDA1L1ORbWdm3aSn6C;SSL Mode=Require;Trust Server Certificate=true";
        using var conn = new NpgsqlConnection(connString);
        await conn.OpenAsync();

        using var cmd = new NpgsqlCommand("SELECT 'DB Connection OK' as msg;", conn);
        var result = await cmd.ExecuteScalarAsync();
        _output.WriteLine(result?.ToString() ?? "");
    }
}



