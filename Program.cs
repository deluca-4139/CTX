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

int TICKET_RESERVATION_ALLOCATION = 5;

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

            // This is a bad idea when it comes to 
            // concurrency; refactor for atomicness
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
    // TODO: type confirmation and sanitization for data fields
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

// Ticket reservation endpoint
// Requires partial ticket info in POST body,
// namely ticketholder name and seating.
// Returns 201 Created if ticket was 
// successfully reserved, 404 Not Found
// if event does exist, or 409 Conflict
// if there was an issue with reservation.
app.MapPost("/events/{id}/reserve", (int id, Ticket ticketInfo) => {
    // TODO: do some transaction analysis 

    // Confirm event exists
    var eventQuery = conn.Query<Event>($"SELECT * FROM events WHERE id = {id};").AsList();
    if(eventQuery.Count == 0) {
        return Results.NotFound();
    }

    // TODO: all three of these checks
    // return the same HTTP response. 
    // Can we change their response to
    // better indicate to the client
    // what has gone wrong during
    // ticket reservation? 

    // Confirm event has space for ticket reservation 
    var retrievedEvent = eventQuery[0];
    if(retrievedEvent.Sold == retrievedEvent.Capacity) {
        // TODO: possibly check to see if any 
        // reservations have expired, and clear
        // them out to make room for this one?
        return Results.Conflict(retrievedEvent);
    }

    // Confirm event is in future
    DateTime parsedTime;
    DateTime.TryParse(retrievedEvent.Start, out parsedTime);
    if(parsedTime < DateTime.Now) {
        return Results.Conflict(retrievedEvent);
    }

    // Confirm seat has not been reserved or sold already
    // TODO: sanitize?
    var retrievedTicket = conn.Query<Ticket>($"SELECT * FROM tickets WHERE event = {id} AND seating = '{ticketInfo.Seating}';").AsList();
    if(retrievedTicket.Count != 0) {
        return Results.Conflict(retrievedEvent);
    } 

    // If we are here, we have confirmed
    // we can reserve a ticket, so:
    // update provided ticket info...
    ticketInfo.Event = id;
    ticketInfo.Id = Guid.NewGuid();
    ticketInfo.Reserved = true;
    ticketInfo.Expiry = DateTime.Now.AddMinutes(TICKET_RESERVATION_ALLOCATION);

    // create entry in tickets table...
    var createTicket = conn.QuerySingle<Ticket>($"""
        INSERT INTO tickets (id, event, ticketholder, seating, reserved, expiry) VALUES ( 
            @id, 
            @event, 
            @ticketholder, 
            @seating, 
            @reserved, 
            @expiry 
        ) RETURNING id;
        """,
        ticketInfo
    );

    // update events database to reflect 
    // newly created ticket...
    var dbUpdate = conn.Execute($"UPDATE events SET sold = {retrievedEvent.Sold + 1} WHERE id = {id};");

    // ...and clean up.
    return Results.Created($"/tickets/{ticketInfo.Id}", ticketInfo); // TODO: this endpoint doesn't exist; make GET?
});

// Ticket purchasing endpoint
// Confirms reservation of existing ticket.
// Returns 404 Not Found if reservation
// doesn't exist, 409 Conflict if 
// ticket is not reserved (i.e. already
// purchased), 410 Gone if reservation
// has expired, and 200 OK if confirmation 
// succeeds.  
app.MapPost("/tickets/{id}/purchase", (Guid id) => {
    // TODO: how do we confirm that
    // the person confirming the ticket
    // purchase has credentials to do so?

    // TODO: stub payment endpoint?

    // Confirm ticket reservation exists
    var ticketQuery = conn.Query<Ticket>($"SELECT * FROM tickets WHERE id = '{id}';").AsList();
    if(ticketQuery.Count == 0) {
        return Results.NotFound();
    }

    // Confirm ticket not already purchased
    var retrievedTicket = ticketQuery[0];
    if(!retrievedTicket.Reserved ?? false) {
        return Results.Conflict(retrievedTicket);
    }

    // Confirm reservation is not expired
    if(retrievedTicket.Expiry < DateTime.Now) {
        // TODO: remove reservation
        return Results.StatusCode(410);
    }

    // If we're here, we can confirm the purchase
    var dbUpdate = conn.Execute($"UPDATE tickets SET reserved = FALSE, expiry = NULL WHERE id = '{id}';");
    return Results.Ok();
});

// Event and ticket deletion endpoints
// These will delete the specified 
// resource. They return 404 Not Found
// if it does not exist, 200 OK otherwise.
app.MapDelete("/events/{id}", (int id) => {
    // TODO: headers for credential 
    // confirmation would be good

    // Confirm event exists
    var eventQuery = conn.Query<Event>($"SELECT * FROM events WHERE id = {id};").AsList();
    if(eventQuery.Count == 0) {
        return Results.NotFound();
    }

    var dbUpdate = conn.Execute($"DELETE FROM events WHERE id = {id};");
    return Results.Ok();
});
app.MapDelete("/tickets/{id}", (Guid id) => {
    // TODO: headers for credential 
    // confirmation would be good

    // Confirm ticket reservation exists
    var ticketQuery = conn.Query<Ticket>($"SELECT * FROM tickets WHERE id = '{id}';").AsList();
    if(ticketQuery.Count == 0) {
        return Results.NotFound();
    }

    var dbUpdate = conn.Execute($"DELETE FROM tickets WHERE id = '{id}';");
    return Results.Ok();
});

app.Run();
