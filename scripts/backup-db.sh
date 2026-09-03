#!/bin/bash
set -euo pipefail

CONTAINER_NAME=$(docker ps --filter "name=postgres-" --filter "status=running" --format "{{.Names}}" | head -1)
DB_NAME="TechSpherex-db"
DB_USER="postgres"
BACKUP_DIR="$(cd "$(dirname "$0")/.." && pwd)/backups"
PASSWORD="mkeNXENG*em*qFqjsUKpew"

if [ -z "$CONTAINER_NAME" ]; then
    echo "ERROR: No running PostgreSQL container found."
    exit 1
fi

mkdir -p "$BACKUP_DIR"
TIMESTAMP=$(date +%Y%m%d_%H%M%S)
BACKUP_FILE="$BACKUP_DIR/db_backup_${TIMESTAMP}.sql"

echo "Backing up database '$DB_NAME' from container '$CONTAINER_NAME'..."
docker exec -e PGPASSWORD="$PASSWORD" "$CONTAINER_NAME" \
    pg_dump -U "$DB_USER" -d "$DB_NAME" --no-owner > "$BACKUP_FILE"

echo "Backup completed: $BACKUP_FILE"
echo "Size: $(du -h "$BACKUP_FILE" | cut -f1)"

find "$BACKUP_DIR" -name "db_backup_*.sql" -mtime +7 -delete
echo "Cleaned up backups older than 7 days"
