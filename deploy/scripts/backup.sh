#!/usr/bin/env sh
# Backs up the MySQL database AND the library file volume. Both are needed: MySQL holds only file metadata.
# Usage (from the repository root, with the compose stack running):  sh deploy/scripts/backup.sh [backup-dir]
set -eu
BACKUP_DIR="${1:-./backups}"
STAMP="$(date -u +%Y%m%dT%H%M%SZ)"
mkdir -p "$BACKUP_DIR"
set -a; . ./.env; set +a

echo "Dumping database ${MYSQL_DATABASE}..."
# --single-transaction gives a consistent InnoDB snapshot without locking writes.
docker compose exec -T mysql mysqldump --single-transaction --routines --triggers --set-gtid-purged=OFF \
  -u root -p"${MYSQL_ROOT_PASSWORD}" "${MYSQL_DATABASE}" | gzip > "$BACKUP_DIR/db-$STAMP.sql.gz"

echo "Archiving library files..."
docker compose run --rm --no-deps -v "$(pwd)/$BACKUP_DIR:/backup" --entrypoint sh api \
  -c "tar -czf /backup/library-$STAMP.tar.gz -C /var/lib/bcs library-files"

echo "Backup written: $BACKUP_DIR/db-$STAMP.sql.gz and $BACKUP_DIR/library-$STAMP.tar.gz"
echo "Copy both files off this host and test a restore regularly."
