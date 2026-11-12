#!/usr/bin/env bash

DB_USERNAME="sa"
DB_PASSWORD="Password2026"
ROOT_DIR="$(cd "$(dirname "$0")/.." && pwd)"

# Copia un script de /database al contenedor y lo ejecuta con sqlcmd.
run_sql() {
  podman cp "${ROOT_DIR}/database/$1" "bonustest-sql:/tmp/$1"
  podman exec bonustest-sql /opt/mssql-tools18/bin/sqlcmd -C -S localhost \
    -U "${DB_USERNAME}" -P "${DB_PASSWORD}" -b -i "/tmp/$1"
}

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
    run_sql 01_schema_seed.sql
    ;;

  bonus)
    run_sql 02_bonus.sql
    ;;

  distribution)
    run_sql 03_bonus_project_distribution.sql
    ;;

  *)
    echo "Uso: $0 {db|start|seed|bonus|distribution}"
    ;;
esac
