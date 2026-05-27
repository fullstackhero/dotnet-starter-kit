#!/bin/sh
set -e

# Fail fast on missing required values rather than serve a broken bundle.
: "${FSH_API_URL:?FSH_API_URL is required (e.g. https://api.example.com)}"
: "${FSH_DASHBOARD_URL:?FSH_DASHBOARD_URL is required (e.g. https://app.example.com)}"

# Defaults for non-required values.
: "${FSH_DEFAULT_TENANT:=root}"

export FSH_API_URL FSH_DASHBOARD_URL FSH_DEFAULT_TENANT

# Render the runtime config from the template, writing into nginx's web root.
envsubst < /usr/share/nginx/html/config.json.template > /usr/share/nginx/html/config.json

# Drop the template so it isn't served accidentally.
rm /usr/share/nginx/html/config.json.template

exec nginx -g 'daemon off;'
