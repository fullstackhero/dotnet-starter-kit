#!/bin/sh
set -e

: "${FSH_API_URL:?FSH_API_URL is required (e.g. https://api.example.com)}"
: "${FSH_DEFAULT_TENANT:=root}"

# The inactivity timings are bare JSON numbers in the template, so each needs a value and it
# must be a positive integer. The defaults mirror DEFAULT_INACTIVITY_* in src/env.ts.
: "${FSH_INACTIVITY_IDLE_MS:=1200000}"
: "${FSH_INACTIVITY_WARNING_MS:=60000}"
require_positive_ms() {
  case "$2" in
    ""|*[!0-9]*|0*) echo "$1 must be a positive integer in milliseconds, got '$2'" >&2; exit 1 ;;
  esac
}
require_positive_ms FSH_INACTIVITY_IDLE_MS "$FSH_INACTIVITY_IDLE_MS"
require_positive_ms FSH_INACTIVITY_WARNING_MS "$FSH_INACTIVITY_WARNING_MS"

export FSH_API_URL FSH_DEFAULT_TENANT FSH_INACTIVITY_IDLE_MS FSH_INACTIVITY_WARNING_MS

envsubst < /etc/fsh/config.json.template \
       > /usr/share/nginx/html/config.json

exec nginx -g 'daemon off;'
