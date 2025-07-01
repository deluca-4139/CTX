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

# (probably will need command line arguments)
