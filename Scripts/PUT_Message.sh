#!/bin/bash

# PUT /api/MessageHistory/{messageId}/

: "${apikey:?\$apikey variable must be provided}"
: "${1:?Message ID must be provided as first argument}"
: "${2:?New content must be provided as second argument}"

messageId="$1"
newContent="$2"

curl -X PUT "http://localhost:5000/api/MessageHistory/$messageId" \
    -H "Content-Type: application/json" \
    -H "key: $apikey" \
    -d @- <<EOF
{
  "NewContent": "$newContent"
}
EOF

