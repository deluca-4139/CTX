using Npgsql;
using Dapper;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var DbUser = builder.Configuration["DbUser"];
var DbPassword = builder.Configuration["DbPassword"];

var connString = $"Host=localhost;Database=tix;Username={DbUser};Password={DbPassword};";
await using var conn = new NpgsqlConnection(connString);
await conn.OpenAsync();

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

app.MapPost("/events", (Event eventInfo) => {
    conn.Query<Event>($"INSERT INTO events (name, capacity) VALUES (@name, @capacity);", eventInfo);
});

app.Run();
