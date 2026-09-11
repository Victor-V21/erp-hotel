"""API auditada sin cambios, conectada exclusivamente al PostgreSQL de prueba."""
from pathlib import Path
import os, subprocess, json

base = Path(__file__).resolve().parent
runtime = base / 'runtime'
runtime.mkdir(exist_ok=True)
env = dict(os.environ)
env.update({
    'ASPNETCORE_ENVIRONMENT': 'Production',
    'ASPNETCORE_URLS': 'http://127.0.0.1:5089',
    'ConnectionStrings__DefaultConnection': 'Host=127.0.0.1;Port=55439;Database=hotel_audit;Username=audit_user;Include Error Detail=true',
    'Jwt__SecretKey': 'AUDIT_SYNTHETIC_KEY_ONLY_LOCAL_TEST_20260906_000000',
    'Jwt__Issuer': 'HotelERP', 'Jwt__Audience': 'HotelERP',
    'Backup__Enabled': 'false',
    'Backup__LocalPath': str(runtime / 'backups'),
    'Backup__GoogleDriveUploadCommand': '',
})
(runtime / 'appsettings.json').write_text(json.dumps({'Logging': {'LogLevel': {'Default':'Warning'}}}))
subprocess.run(['createdb', '-h', '127.0.0.1', '-p', '55439', '-U', 'audit_user', 'hotel_audit'], check=True)
with (base.parent / 'evidencia/api-runtime.log').open('w') as log:
    result = subprocess.run(['dotnet', str(base / 'backend-build/hotel-erp.Api.dll')], cwd=runtime, env=env, stdout=log, stderr=subprocess.STDOUT)
raise SystemExit(result.returncode)
