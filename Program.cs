using Npgsql;
using Dapper;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var DbUser = builder.Configuration["DbUser"];
var DbPassword = builder.Configuration["DbPassword"];

var connString = $"Host=localhost;Database=tix;Username={DbUser};Password={DbPassword};";
await using var conn = new NpgsqlConnection(connString);
await conn.OpenAsync();

int TICKET_RESERVATION_ALLOCATION = 5;

// Stubbed external payment processing function 
Boolean requestTicketPayment(Guid ticketId) {
    return true;
}

// General event information retrieval endpoint
app.MapGet("/events", () => {
    var retrievedEvents = conn.Query<Event>("SELECT * FROM events;");
    return Results.Ok(retrievedEvents);
});

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
// Returns 200 OK if updating of
// event information was successful,
// 404 Not Found if event doesn't exist
app.MapPut("/events/{id}", (int id, Event eventInfo) => {
    // NOTE: I would have liked for this to 
    // be a PATCH request, but for ease of 
    // implementation and in order to assure
    // that the request is atomic, I've gone 
    // with a PUT request instead. 

    // Confirm event exists
    var retrievedEvent = conn.Query<Event>($"SELECT * FROM events WHERE id = {id};").AsList();
    if(retrievedEvent.Count == 0) {
        return Results.NotFound();
    }

    var dbUpdate = conn.Execute($"""
        UPDATE events SET 
            name = @name, 
            start = @start::timestamp, 
            venue = @venue, 
            description = @description, 
            capacity = @capacity, 
            sold = @sold
        WHERE id = {id};
        """,
        eventInfo
    );
    return Results.Ok();
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

    // Chosen solution of "just write
    // the issue in a string we return"
    // does fix it, but. It feels ew.
    // In future, find a better way.

    // Confirm event has space for ticket reservation 
    var retrievedEvent = eventQuery[0];
    if(retrievedEvent.Sold == retrievedEvent.Capacity) {
        // Non-atomic solution; is this an issue?
        var possiblyExpiredTickets = conn.Query<Ticket>($"SELECT * FROM tickets WHERE event = {id} AND reserved = TRUE;").AsList();
        // If there are no possible
        // tickets we can remove, then
        // we must be at capacity
        if(possiblyExpiredTickets.Count == 0) {
            return Results.Conflict("event at capacity");
        }

        // Otherwise, we must be able 
        // to remove at least one ticket,
        // which would allow the sale
        // of a new one to go through
        foreach(Ticket ticket in possiblyExpiredTickets) {
            if(ticket.Expiry < DateTime.Now) {
                conn.Execute($"DELETE FROM tickets WHERE id = '{ticket.Id}';");
                retrievedEvent.Sold -= 1;
            }
        }
        // Be sure to update the event's sold
        // tickets number afterwards to ensure
        // new ticket(s) can be reserved
        conn.Execute($"UPDATE events SET sold = {retrievedEvent.Sold} WHERE id = {id};");
    }

    // Confirm event is in future
    DateTime parsedTime;
    DateTime.TryParse(retrievedEvent.Start, out parsedTime);
    if(parsedTime < DateTime.Now) {
        return Results.Conflict("past event");
    }

    // Confirm seat has not been reserved or sold already
    // TODO: sanitize?
    var retrievedTicket = conn.Query<Ticket>($"SELECT * FROM tickets WHERE event = {id} AND seating = '{ticketInfo.Seating}';").AsList();
    if(retrievedTicket.Count != 0) {
        return Results.Conflict("seat already sold");
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
    // TODO: this could be accomplished with
    // a VIEW of the events db linking the 
    // sold column with the tickets table
    // (which would also ensure referentiability)
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

    // Call out to external payment solution
    if(!requestTicketPayment(id)) {
        return Results.StatusCode(402);
    }

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
        conn.Execute($"DELETE FROM tickets WHERE id = '{retrievedTicket.Id}';");
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
