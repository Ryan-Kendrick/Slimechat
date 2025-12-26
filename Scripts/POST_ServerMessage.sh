#!/bin/bash

# POST /api/ServerMessage/

: "${apikey:?\$apikey variable must be provided}"

i=1
while [ $i -le 50 ]
do
        msg="Test message from server $i"
    curl -X POST "http://localhost:5000/api/ServerMessage/" \
  -H "Content-Type: application/json" \
  -H "key: $apikey" \
 -d @- <<EOF 
 { 
 "Message": "$msg" 
 }
EOF

    ((i++))
done