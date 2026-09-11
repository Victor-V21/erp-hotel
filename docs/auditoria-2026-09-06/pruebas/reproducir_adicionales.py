"""Segunda ronda acotada sobre el mismo laboratorio, posterior a reproducir_api.py."""
from pathlib import Path
source=Path(__file__).with_name('reproducir_api.py').read_text()
exec(source.split('for n in range(60):')[0])
results=json.loads((OUT/'reproducciones-api.json').read_text())
login=call('POST','/auth/login',{'username':'admin','password':'Admin123!'},auth='')['body']
token=login['accessToken']
cai=call('GET','/cai/active')['body']
save('T31 integridad bitácora propia',call('GET','/audit-logs/verify-integrity'))
save('T32 redondeo subtotal 0.03 con dos impuestos',call('POST','/invoices',{'caiId':cai['id'],'customerName':'Redondeo confirmado','items':[dict(item(.03,.15),isTouristTaxable=True)]}))
save('T33 factura guardada sin asiento',sql('SELECT "CustomerName", "SubTotal", "ISVAmount", "TouristTaxAmount", "TotalAmount" FROM "Invoices" i WHERE NOT EXISTS(SELECT 1 FROM "AccountingEntries" e WHERE e."ReferenceId"=i."Id")'))
save('T34 líneas DTO vacías frente a persistencia',{'items_api':call('GET','/invoices')['body'][0]['items'],'items_bd':sql('SELECT count(*) FROM "InvoiceItems"'),'folios_items_api':call('GET','/folios')['body'][0]['items'],'folios_items_bd':sql('SELECT count(*) FROM "FolioItems"')})
save('T35 autorización persistida aunque devuelve 500',{'cantidad_bd':sql('SELECT count(*) FROM "DocumentAuthorizations"'),'listar':call('GET','/document-authorizations')})
guest=call('GET','/guests')['body'][0]
rt=call('GET','/room-types')['body'][0]
room=new('/rooms',{'roomNumber':'AUDIT-102','floor':1,'roomTypeId':rt['id']})
reservation=new('/reservations',{'guestId':guest['id'],'roomId':room['id'],'checkInDate':'2026-11-10','checkOutDate':'2026-11-11','adults':10,'children':0,'advancePayment':200})
r=call('POST','/reservations/checkin',{'reservationId':reservation['id'],'roomId':room['id'],'cashReceived':1,'cashChange':500})
save('T36 checkin sobrecapacidad y efectivo insuficiente',r)
save('T37 snapshot ausente en factura checkin',sql('SELECT "CustomerName", "Status", "PaymentMethod", "CashReceived", "CashChange", "TotalAmount", "FiscalHash" IS NULL FROM "Invoices" WHERE "Id"=\''+r['body']['invoiceId']+'\''))
folio=call('GET','/folios/by-reservation/'+reservation['id'])['body']
call('POST','/folios/'+folio['id']+'/items',{'folioId':folio['id'],'description':'Consumo pendiente ficticio','quantity':1,'unitPrice':100,'isvRate':.15,'isTouristTaxable':False,'discountPercentage':0})
save('T38 checkout con consumo sin facturar',{'respuesta':call('POST','/reservations/checkout/'+reservation['id']),'folio_bd':sql('SELECT "TotalAmount", "Status" FROM "Folios" WHERE "Id"=\''+folio['id']+'\''),'n_facturas_huesped':sql('SELECT count(*) FROM "Invoices" WHERE "GuestId"=\''+guest['id']+'\'')})
save('T39 repetir checkin después de checkout',call('POST','/reservations/checkin',{'reservationId':reservation['id'],'roomId':room['id']}))

product=new('/inventory/products',{'name':'Concurrencia ficticia','unitPrice':10,'currentStock':10,'minStockLevel':2})
holder=subprocess.Popen(['psql','-h','127.0.0.1','-p','55439','-U','audit_user','-d','hotel_audit','-At','-c','BEGIN; SELECT "Id" FROM "Products" WHERE "Id"=\''+product['id']+'\' FOR UPDATE; SELECT pg_sleep(2); COMMIT;'],stdout=subprocess.DEVNULL)
time.sleep(.25)
with concurrent.futures.ThreadPoolExecutor(max_workers=2) as pool:
    rr=list(pool.map(lambda _:call('POST','/inventory/movements',{'productId':product['id'],'movementType':'Salida','quantity':7}),range(2)))
holder.wait()
save('T40 carrera inventario pierde actualización',{'respuestas':rr,'stock_bd':sql('SELECT "CurrentStock" FROM "Products" WHERE "Id"=\''+product['id']+'\''),'salidas_bd':sql('SELECT sum("Quantity") FROM "InventoryMovements" WHERE "ProductId"=\''+product['id']+'\'')})
user=next(u for u in call('GET','/users')['body'] if u['username']=='audit_recepcion')
call('PUT','/users/'+user['id'],{'isActive':True})
rlogin=call('POST','/auth/login',{'username':'audit_recepcion','password':'AuditOnly123!'},auth='')['body']
rtoken=rlogin['accessToken']
save('T41 recepción descarga respaldo completo',{'status':call('POST','/backup/manual',auth=rtoken)['status']})
roles=call('GET','/roles',auth=rtoken)['body']
admin=next(r for r in roles if r['name']=='Admin'); reception=next(r for r in roles if r['name']=='Recepcion')
rename_admin=call('PUT','/roles/'+admin['id'],{'name':'AdminAnterior'},rtoken)
rename_self=call('PUT','/roles/'+reception['id'],{'name':'Admin'},rtoken)
elevated=call('POST','/auth/refresh',{'refreshToken':rlogin['refreshToken']},auth='')
save('T42 escalación recepción a Admin cambiando nombres de roles',{'renombrar_admin':rename_admin,'renombrar_propio':rename_self,'roles_nuevos':elevated['body']['user']['roles'],'endpoint_admin':call('GET','/audit-logs',auth=elevated['body']['accessToken'])['status']})
# Restaurar nombres del laboratorio para posteriores lecturas; no toca sistemas reales.
call('PUT','/roles/'+reception['id'],{'name':'Recepcion'})
call('PUT','/roles/'+admin['id'],{'name':'Admin'})
save('T43 salud sin consulta DB',urllib.request.urlopen('http://127.0.0.1:5089/health').status)
print('FIN ADICIONALES',flush=True)
