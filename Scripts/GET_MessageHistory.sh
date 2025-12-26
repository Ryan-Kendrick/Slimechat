#!/bin/bash

# GET /api/MessageHistory/

count="${1:-""}"

if [ -n "$count" ]; then
    curl -X GET "http://localhost:5000/api/MessageHistory" \
        -H "Content-Type: application/json" \
        -d @- <<EOF
{
  "Count": $count
}
EOF
else
    curl -X GET "http://localhost:5000/api/MessageHistory" \
        -H "Content-Type: application/json"
fi

