#!/bin/bash

# DELETE /api/MessageHistory/{messageId}/

: "${apikey:?\$apikey variable must be provided}"
: "${1:?Message ID must be provided as first argument}"

messageId="$1"

curl -X DELETE "http://localhost:5000/api/MessageHistory/$messageId" \
    -H "Content-Type: application/json" \
    -H "key: $apikey"

