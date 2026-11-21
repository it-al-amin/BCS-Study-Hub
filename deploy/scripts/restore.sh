#!/usr/bin/env sh
# Restores a database dump and a library archive produced by backup.sh. THIS OVERWRITES CURRENT DATA.
# Usage:  sh deploy/scripts/restore.sh backups/db-<stamp>.sql.gz backups/library-<stamp>.tar.gz
set -eu
DB_DUMP="$1"
LIBRARY_ARCHIVE="$2"
set -a; . ./.env; set +a

printf 'This replaces database %s and all library files. Type RESTORE to continue: ' "$MYSQL_DATABASE"
read -r answer
[ "$answer" = "RESTORE" ] || { echo "Aborted."; exit 1; }

docker compose stop api
echo "Restoring database..."
gunzip -c "$DB_DUMP" | docker compose exec -T mysql mysql -u root -p"${MYSQL_ROOT_PASSWORD}" "${MYSQL_DATABASE}"

echo "Restoring library files..."
ARCHIVE_DIR="$(cd "$(dirname "$LIBRARY_ARCHIVE")" && pwd)"
ARCHIVE_NAME="$(basename "$LIBRARY_ARCHIVE")"
docker compose run --rm --no-deps -v "$ARCHIVE_DIR:/backup" --entrypoint sh api \
  -c "rm -rf /var/lib/bcs/library-files/* && tar -xzf /backup/$ARCHIVE_NAME -C /var/lib/bcs"

docker compose start api
echo "Restore complete. Check /health/ready and sign in to verify."
