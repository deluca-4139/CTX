using Npgsql;

var connString = "Host=localhost;Database=tix;Username=rose;Password=password;";
await using var conn = new NpgsqlConnection(connString);
await conn.OpenAsync();

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.MapGet("/events", async () => {
    await using (var cmd = new NpgsqlCommand("SELECT * FROM events", conn))
    await using (var reader = await cmd.ExecuteReaderAsync())
    {
        string outputString = "";
        while(await reader.ReadAsync()) {
            outputString += reader.GetString(0);
        }
        return outputString;
    }
});

app.Run();
