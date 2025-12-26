#!/bin/bash

# GET /api/MessageHistory/{userId}/

: "${1:?User ID must be provided as first argument}"

userId="$1"
count="${2:-""}"

if [ -n "$count" ]; then
    curl -X GET "http://localhost:5000/api/MessageHistory/$userId" \
        -H "Content-Type: application/json" \
        -d @- <<EOF
{
  "Count": $count
}
EOF
else
    curl -X GET "http://localhost:5000/api/MessageHistory/$userId" \
        -H "Content-Type: application/json"
fi

