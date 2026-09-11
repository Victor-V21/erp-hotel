"""Pruebas finales acotadas: SOLO laboratorio local creado para la auditoría."""
from pathlib import Path
import json, hashlib, subprocess, urllib.request, time

OUT=Path(__file__).resolve().parent.parent/'evidencia'
ARGS=['psql','-h','127.0.0.1','-p','55439','-U','audit_user','-d','hotel_audit','-At','-v','ON_ERROR_STOP=1','-c']
def sql(s): return subprocess.check_output(ARGS+[s],text=True).strip()
assert sql('select current_database()')=='hotel_audit'
row=json.loads(sql('SELECT row_to_json(a) FROM "AuditLogs" a ORDER BY "Timestamp" LIMIT 1'))
keys=['PreviousHash','UserId','Action','EntityName','EntityId','CorrelativeNumber','PaymentMethod','Changes']
prefix='|'.join(str(row[k]) if row[k] is not None else '' for k in keys)+'|'
stamp=row['Timestamp']; base,_,frac=stamp.partition('.');frac=frac.ljust(6,'0')[:6]
candidates=[base+'.'+frac+str(tick)+suffix for tick in range(10) for suffix in ('','Z')]
matches=[s for s in candidates if hashlib.sha256((prefix+s).encode()).hexdigest().upper()==row['Hash']]
(OUT/'hash-timestamp.json').write_text(json.dumps({'timestamp_persistido':stamp,'timestamp_que_reproduce_hash':matches,'sin_datos_personales':True},indent=2))

req=urllib.request.Request('http://127.0.0.1:5089/api/auth/login',data=json.dumps({'username':'admin','password':'Admin123!'}).encode(),headers={'Content-Type':'application/json'})
token=json.load(urllib.request.urlopen(req))['accessToken']
def measure(path):
    observations=[]
    for _ in range(3):
        start=time.perf_counter()
        with urllib.request.urlopen(urllib.request.Request('http://127.0.0.1:5089/api'+path,headers={'Authorization':'Bearer '+token}),timeout=45) as r: data=r.read();status=r.status
        observations.append({'ms':round((time.perf_counter()-start)*1000,2),'bytes':len(data),'status':status})
    return observations
out={'base_facturas':int(sql('SELECT count(*) FROM "Invoices"')),'antes':measure('/invoices')}
# Réplicas ficticias sin líneas: prueba conservadora de volumen de cabeceras.
sql('''INSERT INTO "Invoices" SELECT (jsonb_populate_record(NULL::"Invoices",to_jsonb(i)||jsonb_build_object('Id',gen_random_uuid(),'CorrelativeNumber','PERF-AUDIT-'||n,'CustomerName','PERF AUDITORIA SINTETICA','InvoiceDate',timestamp '2020-01-01'+n*interval '1 hour'))).* FROM (SELECT * FROM "Invoices" LIMIT 1) i CROSS JOIN generate_series(1,5000) n''')
sql('ANALYZE "Invoices"')
out['despues_facturas']=int(sql('SELECT count(*) FROM "Invoices"'))
out['despues']=measure('/invoices')
out['advertencia']='Tres lecturas locales por escenario, caché caliente; no es benchmark del hardware del hotel. Réplicas sin líneas y sin asientos; no usar para validar contabilidad.'
(OUT/'rendimiento-local.json').write_text(json.dumps(out,indent=2))
plan=sql('''EXPLAIN (ANALYZE, BUFFERS) SELECT * FROM "Invoices" WHERE NOT "IsDeleted" AND "InvoiceDate">='2020-02-01' AND "InvoiceDate"<'2020-02-02' ''')
(OUT/'explain-facturas.txt').write_text(plan)
sql('''DELETE FROM "Invoices" WHERE "CorrelativeNumber" LIKE 'PERF-AUDIT-%' ''')
print(json.dumps(out,indent=2));print('Hash matching:',matches);print(plan)
