#!/usr/bin/env bash
set -euo pipefail

root_dir=$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)

echo "[QA] Restaurando backend"
dotnet restore "$root_dir/backend/hotel-erp.slnx" --nologo

echo "[QA] Compilando backend"
dotnet build "$root_dir/backend/hotel-erp.slnx" --no-restore --nologo

echo "[QA] Ejecutando pruebas unitarias"
dotnet test "$root_dir/backend/tests/hotel-erp.UnitTests/hotel-erp.UnitTests.csproj" --no-restore --nologo

if [[ -n "${HOTEL_ERP_TEST_ADMIN_CONNECTION:-}" ]]; then
  echo "[QA] Ejecutando integración PostgreSQL"
  dotnet test "$root_dir/backend/tests/hotel-erp.IntegrationTests/hotel-erp.IntegrationTests.csproj" --no-restore --nologo
else
  echo "[QA] Integración omitida: configure HOTEL_ERP_TEST_ADMIN_CONNECTION."
fi

echo "[QA] Instalando frontend desde package-lock.json"
npm --prefix "$root_dir/frontend" ci

echo "[QA] Auditando dependencias frontend"
npm --prefix "$root_dir/frontend" audit --audit-level=low

echo "[QA] Ejecutando lint frontend"
npm --prefix "$root_dir/frontend" run lint

echo "[QA] Generando frontend de producción"
npm --prefix "$root_dir/frontend" run build

echo "[QA] Validando espacios y marcadores de conflicto"
git -C "$root_dir" diff --check -- . \
  ':(exclude)backend/src/hotel-erp.Api/bin/**' \
  ':(exclude)backend/src/hotel-erp.Api/obj/**' \
  ':(exclude)backend/src/hotel-erp.Api/wwwroot/assets/**'

echo "[QA] Todo aprobado"
