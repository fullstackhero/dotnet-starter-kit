#!/bin/sh
set -e

: "${FSH_API_URL:?FSH_API_URL is required (e.g. https://api.example.com)}"
: "${FSH_DEFAULT_TENANT:=root}"

export FSH_API_URL FSH_DEFAULT_TENANT

envsubst < /etc/fsh/config.json.template \
       > /usr/share/nginx/html/config.json

exec nginx -g 'daemon off;'
