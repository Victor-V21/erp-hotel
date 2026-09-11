from pathlib import Path
import json,csv,re,hashlib
root=Path(__file__).resolve().parents[1]
repo=root.parents[1]
audit=root.parent/'auditoria-2026-09-06'
plan=(audit/'08-plan-de-correccion.md').read_text()
tests=json.loads((root/'catalogo-pruebas.json').read_text())
rows=list(csv.DictReader((root/'04-trazabilidad.csv').open()))
original=json.loads((audit/'hallazgos.json').read_text())
steps=set(re.findall(r'^### (S\d{2}|D\d{2})\.',plan,re.M))
expected={f'S{i:02d}' for i in range(32)}|{'D01','D02'}
errors=[]
if steps!=expected:errors.append({'pasos':sorted(steps^expected)})
testids={x['id'] for x in tests}
if len(testids)!=186 or len(tests)!=186:errors.append('cantidad/duplicados de pruebas')
if len(rows)!=64 or {x['hallazgo'] for x in rows}!={x['id'] for x in original}:errors.append('cobertura de hallazgos')
for row in rows:
 if not set(row['pasos'].split())<=steps:errors.append({'pasos_desconocidos':row['hallazgo']})
 if not row['pruebas'].startswith('GD:') and not set(row['pruebas'].split())<=testids:errors.append({'casos_desconocidos':row['hallazgo']})
 for p in row['archivos_originales'].split('; '):
  if not (repo/p).is_file(): errors.append({'ruta_original_inexistente':p})
mds=list(root.glob('*.md'))+[audit/'08-plan-de-correccion.md',audit/'README.md']
links=[]
for path in mds:
 text=path.read_text()
 for match in re.findall(r'\]\(([^)]+)\)',text):
  if match.startswith(('http:','https:','#')):continue
  target=match.split('#')[0]
  if not (path.parent/target).exists():errors.append({'enlace_roto':str(path),'destino':target})
  links.append(target)
 for case in re.findall(r'\b(?:Q|N|SEC|API|DAT|FIS|CON|MON|TX|NC|CTA|HOT|PAG|COM|RET|INV|REP|AUD|IMP|UI|PERF|OPS|M|E2E)\d{2}\b',text):
  if case not in testids:errors.append({'referencia_prueba':case,'archivo':str(path)})
initial=json.loads((root/'evidencia/huellas-inicio.json').read_text())
changed=[p for p,h in initial.items() if not (repo/p).is_file() or hashlib.sha256((repo/p).read_bytes()).hexdigest()!=h]
if changed:errors.append({'archivos_fuera_docs_cambiaron':changed})
result={'fecha':'2026-09-07','tipo':'validacion documental, no ejecucion de pruebas de la aplicacion','pasos_implementacion':32,'trabajos_docker_diferidos':2,'hallazgos_mapeados':len(rows),'casos_definidos':len(tests),'casos_ejecutados_en_esta_entrega':0,'casos_estado_pendiente':sum(x['estado']=='Pendiente' for x in tests),'enlaces_locales_comprobados':len(links),'archivos_versionados_fuera_docs_comparados':len(initial),'cambios_fuera_docs_durante_plan':changed,'errores':errors,'resultado':'OK' if not errors else 'REVISAR'}
(root/'evidencia/validacion-plan.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n')
print(json.dumps(result,ensure_ascii=False,indent=2))
raise SystemExit(bool(errors))
