#!/usr/bin/env bash

DB_USERNAME="sa"
DB_PASSWORD="Password2026"
ROOT_DIR="$(cd "$(dirname "$0")/.." && pwd)"

case "$1" in
  db)
    podman run -d \
      --name bonustest-sql \
      -e "ACCEPT_EULA=Y" \
      -e "MSSQL_SA_PASSWORD=${DB_PASSWORD}" \
      -p 1433:1433 \
      mcr.microsoft.com/mssql/server:2022-latest
    ;;

  start)
    podman start bonustest-sql
    ;;

  seed)
    podman cp "${ROOT_DIR}/database/01_schema_seed.sql" bonustest-sql:/tmp/01_schema_seed.sql
    podman exec bonustest-sql /opt/mssql-tools18/bin/sqlcmd -C -S localhost \
      -U "${DB_USERNAME}" -P "${DB_PASSWORD}" -b -i /tmp/01_schema_seed.sql
    ;;

  bonus)
    podman cp "${ROOT_DIR}/database/02_bonus.sql" bonustest-sql:/tmp/02_bonus.sql
    podman exec bonustest-sql /opt/mssql-tools18/bin/sqlcmd -C -S localhost \
      -U "${DB_USERNAME}" -P "${DB_PASSWORD}" -b -i /tmp/02_bonus.sql
    ;;

  *)
    echo "Uso: $0 {db|start|seed|bonus}"
    ;;
esac
