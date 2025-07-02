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
psql -d tix -c "CREATE TABLE events ( \
                    id          INT PRIMARY KEY GENERATED ALWAYS AS IDENTITY, \
                    name        TEXT, \
                    start       TIMESTAMP, \
                    venue       TEXT, \
                    description TEXT, \
                    capacity    INT, \
                    sold        INT
                );"

# Populate events table 
# with example values
psql -d tix -c "INSERT INTO events (name, start, venue, description, capacity, sold) VALUES ( \
                    'Wicked: The Musical', \
                    '2025-10-31 19:30:00', \
                    '5th Ave', \
                    'It''s Wicked: The Musical!', \
                    1000,
                    600
                );" 
psql -d tix -c "INSERT INTO events (name, start, venue, description, capacity, sold) VALUES ( \
                    'Bon Iver; SABLE fABLE Tour', \
                    '2026-01-15 13:00:00', \
                    'Climate Pledge Arena', \
                    'Bon Iver''s New Album Tour', \
                    2000,
                    2000
                );" 

# (probably will need command line arguments)
