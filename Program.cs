using Npgsql;
using Dapper;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var DbUser = builder.Configuration["DbUser"];
var DbPassword = builder.Configuration["DbPassword"];

var connString = $"Host=localhost;Database=tix;Username={DbUser};Password={DbPassword};";
await using var conn = new NpgsqlConnection(connString);
await conn.OpenAsync();

app.MapGet("/", () => "Hello World!");

// Event info retrieval endpoint
// Returns 200 OK and the data for the event
// if it exists, 404 Not Found otherwise
app.MapGet("/events/{id}", (int id) => {
    var retrievedEvent = conn.Query<Event>($"SELECT * FROM events WHERE id = {id};").AsList();
    if(retrievedEvent.Count != 0) {
        return Results.Ok(retrievedEvent[0]);
    } else {
        return Results.NotFound();
    }
});

// Event updating endpoint
// Returns 204 No Content if updating of
// event information was successful,
// 404 Not Found if event doesn't exist
app.MapPatch("/events/{id}", (int id, JsonDocument passedJson) => {
    var retrievedEvent = conn.Query<Event>($"SELECT * FROM events WHERE id = {id};").AsList();
    if(retrievedEvent.Count != 0) {
        var jsonEnumerator = passedJson.RootElement.EnumerateObject();
        while(jsonEnumerator.MoveNext()) {
            // TODO: this does not perform any 
            // type checks on passed json; assumes
            // that all properties passed exist
            // on event type. Add sanitization?
            var dbUpdate = conn.Execute($"UPDATE events SET {jsonEnumerator.Current.Name} = '{jsonEnumerator.Current.Value}' WHERE id = {id};");
        }
        return Results.NoContent();
    } else {
        return Results.NotFound();
    }
});

// Event creation endpoint
// Returns 201 Created after creation,
// along with the ID of the created event
app.MapPost("/events", (Event eventInfo) => {
    // TODO: type confirmation and sanitization for data fields?
    var addEventToDb = conn.QuerySingle<Event>($"""
        INSERT INTO events (name, start, venue, description, capacity, sold) VALUES (
            @name,
            @start::timestamp,
            @venue,
            @description,
            @capacity,
            @sold
        ) RETURNING id;
        """,
        eventInfo
    );
    
    eventInfo.Id = addEventToDb.Id;
    return Results.Created($"/events/{eventInfo.Id}", eventInfo);
});

app.Run();
