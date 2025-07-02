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
* purchase tickets
    - POST /purchase/{id} ?? idk 
    - for "assuming there's a payment processing system I can leverage", is there a way I can create a "stubbed" function that schematically works but just always returns OK
* cancel reservations
    - what if the person has bought more than one ticket?
    - PUT or POST? probably POST?
    - what if the person hasn't bought *any* tickets? probably simple to just check within the database first and confirm that they have one before trying to cancel it 
* view ticket availability 
    - probably the same as something in the events section above
    - could also just be a GET /tickets/{id}
* what data does a ticket hold?
    - unique ID? 
    - ticketholder name
    - seating? 
    - event linked to? probably just event ID 

other thoughts:
* for "updating" or "managing" event information (such as editing event information or ticket types), it would make sense for there to be a second "entity" type; admin versus customer, e.g. but I don't think this is a reasonable feature to implement, as it's not listed in the requirements, so I think it's out of scope for this project 
    - ehhh could offer authentication via headers?
    - if so then the headers should specify auth info which should be included within event information to match admins with events
* specific reasons for using direct database queries vs. object mapper (micro ORM) vs. full ORM
* also reasons for using endpoint API vs controller API
