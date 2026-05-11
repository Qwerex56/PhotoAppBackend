#!/usr/bin/env bash

set -euo pipefail

cert_dir="docker/traefik/certs"

mkdir -p "$cert_dir"

mkcert -install
mkcert \
  -cert-file "$cert_dir/photoapp.localhost.pem" \
  -key-file "$cert_dir/photoapp.localhost-key.pem" \
  localhost \
  auth.localhost \
  users.localhost \
  media.localhost \
  traefik.localhost \
  127.0.0.1 \
  ::1