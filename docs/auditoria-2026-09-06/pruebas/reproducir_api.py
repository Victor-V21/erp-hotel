"""Reproducciones sobre datos ficticios. Requiere iniciar_api.py y BD vacía de auditoría.
No aceptar URLs externas: el destino y el usuario SQL están fijados al laboratorio.
"""
from pathlib import Path
import urllib.request, urllib.error, json, time, subprocess, concurrent.futures, uuid

BASE='http://127.0.0.1:5089/api'
OUT=Path(__file__).resolve().parent.parent/'evidencia'
results=[]
token=None
def sql(query):
    return subprocess.check_output(['psql','-h','127.0.0.1','-p','55439','-U','audit_user','-d','hotel_audit','-At','-c',query],text=True).strip()
def call(method,path,data=None,auth=None):
    headers={'Content-Type':'application/json'}
    t=token if auth is None else auth
    if t: headers['Authorization']='Bearer '+t
    req=urllib.request.Request(BASE+path,data=json.dumps(data).encode() if data is not None else None,headers=headers,method=method)
    start=time.perf_counter()
    try:
        with urllib.request.urlopen(req,timeout=45) as response: status,raw=response.status,response.read()
    except urllib.error.HTTPError as e: status,raw=e.code,e.read()
    try: body=json.loads(raw)
    except Exception: body=raw.decode(errors='replace')[:1200]
    return {'status':status,'body':body,'ms':round((time.perf_counter()-start)*1000,2)}
def save(name,observed):
    results.append({'prueba':name,'observado':observed})
    (OUT/'reproducciones-api.json').write_text(json.dumps(results,ensure_ascii=False,indent=2))
    print(name,json.dumps(observed,ensure_ascii=False)[:650],flush=True)
def new(path,data):
    r=call('POST',path,data)
    if r['status'] not in (200,201): raise RuntimeError((path,r))
    return r['body']
def item(price=100,rate=.15,**kw):
    return dict(description='Servicio ficticio auditoría',quantity=1,unitPrice=price,isvRate=rate,isExempt=False,isTouristTaxable=False,discountPercentage=0,**kw)

for n in range(60):
    try:
        r=call('POST','/auth/login',{'username':'admin','password':'Admin123!'},auth='')
        if r['status']==200: token=r['body']['accessToken']; break
    except OSError: pass
    time.sleep(.5)
else: raise RuntimeError('API no disponible; revisar arranque/migración')
save('T01 credenciales iniciales',{'status':r['status'],'rol':r['body']['user']['roles']})
save('T02 endpoints protegidos anónimos',call('GET','/invoices',auth=''))

guest=new('/guests',{'firstName':'Prueba','lastName':'Auditoria','documentType':'Otro','documentNumber':'AUDIT-0001'})
rt=new('/room-types',{'name':'AUDIT Sencilla','pricePerNight':1190,'capacity':2})
room=new('/rooms',{'roomNumber':'AUDIT-101','floor':1,'roomTypeId':rt['id']})
reservation=new('/reservations',{'guestId':guest['id'],'roomId':room['id'],'checkInDate':'2026-10-10','checkOutDate':'2026-10-11','adults':1,'children':0,'advancePayment':100})
r=call('POST','/reservations/checkin',{'reservationId':reservation['id'],'roomId':room['id']})
save('T03 checkin sin CAI deja estado parcial',{'respuesta':r,'reserva':call('GET','/reservations/'+reservation['id'])['body'],'folios':call('GET','/folios')['body']})
save('T04 disponibilidad futura no solapada',call('GET','/rooms/available?checkIn=2026-11-10&checkOut=2026-11-11'))
save('T05 cancelación de estadía en curso',call('POST','/reservations/'+reservation['id']+'/cancel'))
save('T06 confirmación de cancelada',call('POST','/reservations/'+reservation['id']+'/confirm'))
save('T07 actualizar solo salida anterior a entrada',call('PUT','/reservations/'+reservation['id'],{'checkOutDate':'2026-10-01'}))

cai=new('/cai',{'caiNumber':'AUDIT-CAI-00000001','issueDate':'2026-01-01','dueDate':'2027-12-31','initialRange':'001-001-01-00000001','finalRange':'001-001-01-00001000'})
inv=new('/invoices',{'caiId':cai['id'],'customerName':'Auditoria sintética','items':[item()]})
save('T08 primer correlativo omite inicio',{'inicial':cai['initialRange'],'emitido':inv['correlativeNumber']})
cn=new('/invoices/'+inv['id']+'/credit-note',{'caiId':cai['id'],'originalInvoiceId':inv['id'],'reason':'Devolución ficticia total','items':[item()]})
entries=call('GET','/accounts/journal-entries')['body']
save('T09 nota crédito suma ingresos y calcula ISV desde LineTotal cliente',{'nota':cn,'asientos':[e for e in entries if e['referenceId'] in (inv['id'],cn['id'])]})
save('T10 resumen fiscal suma original y nota',call('GET','/reports/tax-summary?from=2026-01-01&to=2027-12-31'))
before=sql('SELECT "FiscalHash" FROM "Invoices" WHERE "Id"=\''+cn['id']+'\'')
changed=call('PUT','/invoices/'+cn['id'],{'caiId':cai['id'],'customerName':'Nota modificada','items':[item(25)]})
save('T11 nota fiscal editable y hash/asiento desactualizados',{'respuesta':changed,'hash_igual':before==sql('SELECT "FiscalHash" FROM "Invoices" WHERE "Id"=\''+cn['id']+'\''),'asiento':next(e for e in call('GET','/accounts/journal-entries')['body'] if e['referenceId']==cn['id'])})
save('T12 línea negativa permitida',call('POST','/invoices',{'caiId':cai['id'],'customerName':'Negativo ficticio','items':[item(-100,0)]}))
save('T13 desglose tasa 18 equivocado',call('POST','/invoices',{'caiId':cai['id'],'customerName':'Tasa 18 ficticia','items':[item(100,.18)]}))
save('T14 base exenta mal clasificada',call('POST','/invoices',{'caiId':cai['id'],'customerName':'Exenta ficticia','items':[dict(item(),isExempt=True)]}))
before=sql('SELECT count(*) FROM "Invoices"')
r=call('POST','/invoices',{'caiId':cai['id'],'customerName':'Redondeo ficticio','items':[item(.03,.15),item(.03,.15)]})
save('T15 redondeo rompe asiento tras guardar factura',{'respuesta':r,'facturas_antes':before,'facturas_despues':sql('SELECT count(*) FROM "Invoices"'),'sin_asiento':sql('SELECT "CorrelativeNumber", "TotalAmount", "SubTotal"+"ISVAmount"+"TouristTaxAmount" FROM "Invoices" i WHERE NOT EXISTS (SELECT 1 FROM "AccountingEntries" e WHERE e."ReferenceId"=i."Id")')})
save('T16 nota directa sin original ni motivo',call('POST','/invoices',{'caiId':cai['id'],'customerName':'Nota sin origen','documentType':'NotaCredito','items':[item()]}))
save('T17 autorización DTO DateOnly DateTime',call('POST','/document-authorizations',{'documentType':'Factura','caiNumber':'AUDIT-AUTH-00000001','issueDate':'2026-01-01','dueDate':'2027-12-31','initialRange':'002-001-01-00000001','finalRange':'002-001-01-00001000'}))

supplier=new('/suppliers',{'name':'Proveedor ficticio','rtn':'00000000000001'})
purchase=new('/purchase-invoices',{'invoiceNumber':'AUDIT-COMPRA-1','supplierId':supplier['id'],'invoiceDate':'2026-09-06T12:00:00Z','items':[item()]})
save('T18 pago compra sin asiento de pago',{'respuesta':call('PUT','/purchase-invoices/'+purchase['id']+'/pay'),'asientos_referencia':sql('SELECT count(*) FROM "AccountingEntries" WHERE "ReferenceId"=\''+purchase['id']+'\'')})
duplicate=call('POST','/purchase-invoices',{'invoiceNumber':'AUDIT-COMPRA-1','supplierId':supplier['id'],'invoiceDate':'2026-09-06T12:00:00Z','items':[item()]})
save('T19 duplicidad compra mismo proveedor',duplicate)
save('T20 eliminar compra desincroniza saldos',{'respuesta':call('DELETE','/purchase-invoices/'+purchase['id']),'totales_catalogo':[a for a in call('GET','/accounts/flat')['body'] if a['accountNumber']=='2103'],'balance_comprobacion':[a for a in call('GET','/accounts/trial-balance')['body'] if a['accountNumber']=='2103']})
save('T21 borrar proveedor oculta compras',{'respuesta':call('DELETE','/suppliers/'+supplier['id']),'compras_visibles':call('GET','/purchase-invoices')['body']})

cash=new('/cash-registers',{'name':'Caja ficticia'})
call('POST','/cash-registers/'+cash['id']+'/open',{'cashRegisterId':cash['id'],'initialAmount':1000})
save('T22 cierre confía esperado del cliente',call('POST','/cash-registers/'+cash['id']+'/close',{'cashRegisterId':cash['id'],'expectedAmount':1,'countedAmount':1}))
save('T23 ventas no registradas en caja',{'movimientos':sql('SELECT "MovementType",count(*) FROM "CashMovements" GROUP BY 1')})
product=new('/inventory/products',{'name':'Producto ficticio','unitPrice':10,'currentStock':10,'minStockLevel':2})
save('T24 editar stock negativo sin kardex',{'respuesta':call('PUT','/inventory/products/'+product['id'],{'currentStock':-10}),'movimientos':call('GET','/inventory/movements?productId='+product['id'])['body']})

user=new('/users',{'username':'audit_recepcion','password':'AuditOnly123!','email':'audit@example.invalid','firstName':'Prueba','lastName':'Recepcion','roles':['Recepcion']})
login=call('POST','/auth/login',{'username':'audit_recepcion','password':'AuditOnly123!'},auth='')['body']
rtoken=login['accessToken']
save('T25 permisos recepción',{'roles':login['user']['roles'],'crear_cuenta':call('POST','/accounts',{'accountNumber':'9999','accountName':'Cambio no autorizado','accountType':'Activo'},rtoken),'usuarios':call('GET','/users',auth=rtoken)['status'],'exportar_huespedes':call('GET','/data/export/guests',auth=rtoken)['status'],'respaldo':call('GET','/backup/logs',auth=rtoken)['status']})
call('DELETE','/users/'+user['id'])
save('T26 token sigue válido tras desactivar usuario',call('GET','/accounts/flat',auth=rtoken)['status'])
save('T27 impresión Linux',call('GET','/print/printers'))

# Contención acotada: dos solicitudes leen el mismo CAI mientras otra sesión lo bloquea.
holder=subprocess.Popen(['psql','-h','127.0.0.1','-p','55439','-U','audit_user','-d','hotel_audit','-At','-c',"BEGIN; SELECT pg_advisory_xact_lock(hashtext('cai:"+cai['id']+"')); SELECT pg_sleep(2); COMMIT;"],stdout=subprocess.DEVNULL)
time.sleep(.25)
with concurrent.futures.ThreadPoolExecutor(max_workers=2) as pool:
    concurrent_results=list(pool.map(lambda _:call('POST','/invoices',{'caiId':cai['id'],'customerName':'Carrera ficticia','items':[item()]}),range(2)))
holder.wait()
save('T28 correlativo con contexto EF precargado',concurrent_results)
save('T29 consultas de estado enum',{'habitaciones':call('GET','/rooms/status/Libre'),'reservas':call('GET','/reservations?status=Confirmada')})
save('T30 historial de auditoría cubre pocas operaciones',sql('SELECT "Action",count(*) FROM "AuditLogs" GROUP BY 1 ORDER BY 1'))
print('FIN: '+str(len(results))+' reproducciones',flush=True)
