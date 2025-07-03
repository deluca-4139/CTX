core features are
* event management
* ticket reservations and sales
* venue capacity management

notes:
* remember to call out assumptions and document choices as you go
* CRUD api - create, retrieve, update, delete

for events:
* basic event details (date, venue, description)
    - should probably start here, just because GET requests are likely easier to mess around with at the beginning 
    - route could be GET /events/{id} perhaps?
    - what data does an event carry? 
        - date
        - venue
        - description 
        - capacity (contained within venue? probably not)
        - current ticket sales (to be able to compare with capacity)
        - current guestlist? (to confirm reservation cancel)
            - probably unnecessary; would just query the tickets table instead
* create/update concert events
    - POST /events/create (not idempotent)
        - PATCH is better for small updates; is also not idempotent, but requires extra work to confirm that data being passed in is valid in terms of shape
    - how do I pass parameters to a POST request again lol 
        - payload
* set ticket types and pricing 
    - PATCH /events/{id}/tickets (only issuing partial update)
    - ticket types probably means seating types (general admission, assigned seating, VIP, etc)
* manage available capacity 
    - PATCH /events/{id}/capacity (? idk what best practice is here)

for tickets:
* reserve tickets for a time window
    - what does this mean???
        - it means reserving tickets during checkout; "this ticket is held for you for 5 minutes while you enter your payment info"
* purchase tickets
    - POST /purchase/{id} ?? idk 
    - for "assuming there's a payment processing system I can leverage", is there a way I can create a "stubbed" function that schematically works but just always returns OK
    - do we want to check that an event is in the future before selling a ticket?
* cancel reservations
    - what if the person has bought more than one ticket?
        - I think the most reasonable/sanity-preserving method here is to assume that the frontend that is using our API endpoints is able to choose and provide a specific ticket to cancel the reservation of. perhaps for "extra credit" I could create a ticket retrieval GET endpoint to facilitate this? 
    - PUT or POST? probably POST?
    - what if the person hasn't bought *any* tickets? probably simple to just check within the database first and confirm that they have one before trying to cancel it 
    - do we want confirmation before deletion? can probably assume as above that the potential frontend consuming our API will have some sort of confirmation screen before submitting the POST request to cancel reservation
* view ticket availability 
    - probably the same as something in the events section above
    - could also just be a GET /tickets/{id}
* what data does a ticket hold?
    - unique ID? 
        - does this need to be anything other than just a sequential integer? probably for security reasons it would be best to generate a UUID
    - ticketholder name
    - seating? (should be unique within events, but that requires a more complex SQL setup)
    - event linked to? probably just event ID 

other thoughts:
* for "updating" or "managing" event information (such as editing event information or ticket types), it would make sense for there to be a second "entity" type; admin versus customer, e.g. but I don't think this is a reasonable feature to implement, as it's not listed in the requirements, so I think it's out of scope for this project 
    - ehhh could offer authentication via headers?
    - if so then the headers should specify auth info which should be included within event information to match admins with events
* specific reasons for using direct database queries vs. object mapper (micro ORM) vs. full ORM
* also reasons for using endpoint API vs controller API

next steps:
* refactor the sold/capacity logic to allow for a new table that tracks ticket types, prices, and sales
* unit testing!
* sanitization of inputs where applicable 
* headers on requests for credentials when performing privileged actions
* refactoring of endpoints to move database logic into classes
