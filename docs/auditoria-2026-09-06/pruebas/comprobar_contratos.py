"""Comprueba cuatro cuerpos/rutas usados por UI contra el laboratorio sintético."""
from pathlib import Path
source=Path(__file__).with_name('reproducir_api.py').read_text()
exec(source.split('for n in range(60):')[0])
token=call('POST','/auth/login',{'username':'admin','password':'Admin123!'},auth='')['body']['accessToken']
folios=call('GET','/folios')['body']
fid=next(f['id'] for f in folios if f['status']=='Abierto')
cases=[
 ('C01 cargo como frontend','/folios/'+fid+'/items',{'description':'Prueba contrato','quantity':1,'unitPrice':100,'isvRate':.15}),
 ('C02 ruta nota crédito frontend','/invoices/credit-note',{'reason':'Prueba contrato'}),
 ('C03 tipo Empresa frontend','/invoices',{'customerName':'Prueba contrato','taxpayerType':'Empresa','items':[{'description':'Prueba','quantity':1,'unitPrice':10}]}),
 ('C04 fecha ISO autorización frontend','/document-authorizations',{'documentType':'Factura','caiNumber':'CONTRATO-TEST','issueDate':'2026-01-01T06:00:00.000Z','dueDate':'2027-01-01T06:00:00.000Z','initialRange':'003-001-01-00000001','finalRange':'003-001-01-00001000'})]
out=[]
for name,path,data in cases:
    r=call('POST',path,data)
    out.append({'prueba':name,'status':r['status'],'respuesta':r['body']})
(OUT/'contratos-frontend.json').write_text(json.dumps(out,ensure_ascii=False,indent=2))
print([(r['prueba'],r['status']) for r in out])
