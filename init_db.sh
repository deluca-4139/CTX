#!/bin/bash

# check whether postgres server is running

# check whether or not we've already created a database
# if not, create it 
# also need to have the capability of wiping/resetting
echo "Creating database..."

createdb tix
if [ $? -eq 1 ]; then
    echo "Database already exists. Resetting..."
    dropdb tix
    createdb tix
fi

# Create events table
psql -d tix -c "CREATE TABLE events ( 
                    id          INT PRIMARY KEY GENERATED ALWAYS AS IDENTITY, 
                    name        TEXT, 
                    start       TIMESTAMP, 
                    venue       TEXT, 
                    description TEXT, 
                    capacity    INT, 
                    sold        INT
                );"

# Populate events table 
# with example values
psql -d tix -c "INSERT INTO events (name, start, venue, description, capacity, sold) VALUES ( 
                    'Wicked: The Musical', 
                    '2025-10-31 19:30:00', 
                    '5th Ave', 
                    'It''s Wicked: The Musical!', 
                    1000, 
                    600 
                );" 
psql -d tix -c "INSERT INTO events (name, start, venue, description, capacity, sold) VALUES ( 
                    'Bon Iver; SABLE fABLE Tour', 
                    '2026-01-15 13:00:00', 
                    'Climate Pledge Arena', 
                    'Bon Iver''s New Album Tour', 
                    2000, 
                    2000 
                );" 
psql -d tix -c "INSERT INTO events (name, start, venue, description, capacity, sold) VALUES ( 
                    'The Already Happened Concert', 
                    '2010-04-15 16:00:00', 
                    'An Existing Venue', 
                    'This event occured in the past. Why are you here?', 
                    100, 
                    80 
                );" 

# Create tickets table
psql -d tix -c "CREATE TABLE tickets ( 
                    id              UUID PRIMARY KEY, 
                    event           INT REFERENCES events(id), 
                    ticketholder    TEXT, 
                    seating         TEXT, 
                    reserved        BOOLEAN, 
                    expiry          TIMESTAMP 
                );"

# Populate tickets table
# with example values
psql -d tix -c "INSERT INTO tickets (id, event, ticketholder, seating, reserved, expiry) VALUES ( 
                    '8ff7f91e-b44d-40ac-9efd-b92891fa4139', 
                    1, 
                    'Doe, John', 
                    'B12', 
                    FALSE, 
                    NULL 
                );"
psql -d tix -c "INSERT INTO tickets (id, event, ticketholder, seating, reserved, expiry) VALUES ( 
                    '31f847cd-8f6a-4a0c-b000-db939170c263', 
                    2, 
                    'Prentiss, Jane', 
                    'CC23', 
                    FALSE, 
                    NULL 
                );"
psql -d tix -c "INSERT INTO tickets (id, event, ticketholder, seating, reserved, expiry) VALUES ( 
                    '63aaeb93-82da-41c4-9b4e-3995e6d3b31f', 
                    2, 
                    'Prentiss, Jane', 
                    'CC24', 
                    TRUE, 
                    '2025-07-01 12:00:00' 
                );"

# (probably will need command line arguments)
