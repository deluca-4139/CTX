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

psql -d tix -c "CREATE TABLE events ( \
                    id          INT PRIMARY KEY GENERATED ALWAYS AS IDENTITY, \
                    name        TEXT, \
                    start       TIMESTAMP, \
                    venue       TEXT, \
                    description TEXT, \
                    capacity    INT, \
                    sold        INT
                );"
psql -d tix -c "INSERT INTO events (name, start, venue, description, capacity, sold) VALUES ( \
                    'Wicked: The Musical', \
                    '2025-10-31 19:30:00', \
                    '5th Ave', \
                    'It''s Wicked: The Musical!', \
                    1000,
                    600
                );" 

# (probably will need command line arguments)
