from pathlib import Path
import csv,json
root=Path(__file__).resolve().parents[1]
audit=root.parent/'auditoria-2026-09-06'
# ID: pasos; casos; puerta. Hallazgos originales conservados sin alterar su evidencia.
mapping={
'FIN-01':('S11 S12','NC01 NC02 NC03 CTA01','G2'),
'FIN-02':('S05 S10 S12','TX01 TX02 TX03 TX05','G2'),
'FIN-03':('S08 S11','MON10 NC01 NC04','G2'),
'FIN-04':('S01 S08 S21','MON02 MON03 REP03','G4'),
'FIN-05':('S08 S10 S12','MON08 CTA02 TX02','G2'),
'FIN-06':('S09 S11','FIS08 FIS09 FIS12 NC07','G2'),
'FIN-07':('S11 S20 S21','REP01 REP02 REP03','G4'),
'FIN-08':('S12 S16 S17','COM03 COM06 CTA04','G3'),
'FIN-09':('S13 S17','COM01 COM05 CTA04','G3'),
'FIN-10':('S16 S24','PAG02 PAG03 PAG05 PAG06 PAG08','G3'),
'FIN-11':('S13 S20','CTA07 CTA10 DAT06','G4'),
'FIN-12':('S01 S12 S20','CTA08 CTA09','G4'),
'FIN-13':('S08 S13','MON09 DAT07','G2'),
'FIN-14':('S01 S18 S21 S25','N03 RET04 REP06 REP07','G4'),
'HOT-01':('S13 S14','HOT01 HOT02 CON03','G3'),
'HOT-02':('S14 S15','HOT03 HOT05 TX01','G3'),
'HOT-03':('S14 S15 S16','HOT04 PAG01 PAG02 PAG04','G3'),
'HOT-04':('S09 S15 S16','HOT08 HOT09 HOT10 E2E03','G3'),
'HOT-05':('S01 S08 S15 S24','MON05 MON06 MON07 MON12 UI04','G3'),
'HOT-06':('S13 S14 S17 S20','DAT06 CTA07 COM05','G4'),
'INV-01':('S13 S19','CON05 INV01 INV04','G3'),
'INV-02':('S12 S17 S19','INV02 INV03 INV05 INV06','G3'),
'SEG-01':('S03 S04','SEC01 SEC03 SEC04','G1'),
'SEG-02':('S04 S05','Q06 SEC06 SEC10','G1'),
'SEG-03':('S03','SEC02 SEC03 SEC04','G1'),
'SEG-04':('S03 S28','SEC05','G1'),
'SEG-05':('S04','SEC06 SEC07 SEC08','G1'),
'SEG-06':('S06 S22','AUD01 AUD05','G4'),
'SEG-07':('S10 S22','AUD02 AUD03 AUD04 SEC11','G4'),
'SEG-08':('S03 S04 D01','SEC03 SEC11','G1 + GD'),
'SEG-09':('S04 S24','SEC08 SEC09 SEC10 UI07','G5'),
'SEG-10':('S02 S04 S05','Q04 Q06','G1'),
'OPS-01':('S02','Q01 Q02','G1'),
'OPS-02':('D01','', 'GD'),
'OPS-03':('D01','', 'GD'),
'OPS-04':('S23 D02','IMP03 IMP06 IMP07','G5 + GD'),
'OPS-05':('S07 S09 S23','FIS10 IMP01 IMP02 IMP05','G4'),
'OPS-06':('S28 S29 D01','OPS03 OPS07 M04','G5 + GD'),
'OPS-07':('S28','OPS04 OPS05 OPS06','G5'),
'OPS-08':('S27 D01','OPS01 OPS02','G5 + GD'),
'DB-01':('S07 S10 S13','CON01 CON02 TX02','G2'),
'DB-02':('S07','FIS01 FIS02 FIS03 FIS04 FIS05','G2'),
'DB-03':('S06 S13 S20 S21','DAT01 DAT02 DAT03 CTA09','G4'),
'DB-04':('S06 S24','API01 API02 API05','G1'),
'DB-05':('S26 S27','PERF01 PERF02 PERF03 UI15','G5'),
'DB-06':('S13 S14 S19 S27','DAT04 DAT05 DAT07 CON03 CON05','G5'),
'DB-07':('S00 S13 S29','M01 M02 M03 M05 M06','G6'),
'UX-01':('S25','UI10','G5'),
'UX-02':('S25','UI11','G5'),
'UX-03':('S25','UI08 UI09','G5'),
'UX-04':('S25','UI08','G5'),
'UX-05':('S06 S11 S24','API05 UI01 UI02 UI03','G5'),
'UX-06':('S06 S08 S15 S24','MON12 UI04 HOT10','G5'),
'UX-07':('S15 S24','HOT06 HOT07','G5'),
'UX-08':('S24 S25','UI07 UI13 UI16','G5'),
'UX-09':('S26','UI14 UI15 UI16 PERF01','G5'),
'UX-10':('S03 S25','SEC03 UI12','G5'),
'UX-11':('S04 S10 S24','UI05 TX03 TX05 SEC10','G5'),
'UX-12':('S18 S21 S22 S25','REP06 REP07 AUD05 UI12','G5'),
'UX-13':('S23','IMP04','G4'),
'UX-14':('S25','UI11 UI13','G5'),
'QA-01':('S02 S30','Q01 Q02 Q03 E2E06','G6'),
'QA-02':('S05 S08 S10 S12','Q05 MON10 TX02','G2'),
'QA-03':('S00 S02 S05 S29 S30 S31','Q01 Q06 M06 E2E06','G6')}
findings=json.loads((audit/'hallazgos.json').read_text())
assert set(mapping)=={x['id'] for x in findings}
testids={x['id'] for x in json.loads((root/'catalogo-pruebas.json').read_text())}
rows=[]
for h in findings:
 tasks,cases,gate=mapping[h['id']]
 assert set(cases.split())<=testids,(h['id'],cases)
 deferred=h['id'] in ('OPS-02','OPS-03')
 partial='D01' in tasks or 'D02' in tasks
 rows.append(dict(hallazgo=h['id'],prioridad=h['prioridad'],titulo=h['titulo'],pasos=tasks,pruebas=cases or 'GD: ensayo Docker posterior',puerta=gate,estado='Diferido por alcance Docker' if deferred else 'Pendiente; componente Docker diferido' if partial else 'Pendiente',archivos_originales='; '.join(dict.fromkeys(x['archivo'] for x in h['ubicaciones'])),cierre_requerido='GD; no se cierra en etapa local' if deferred else 'Evidencia de pruebas y firma de puerta; GD adicional si corresponde'))
with (root/'04-trazabilidad.csv').open('w',newline='') as f:
 w=csv.DictWriter(f,fieldnames=rows[0].keys());w.writeheader();w.writerows(rows)
(root/'04-trazabilidad.md').write_text('''# Trazabilidad de la auditoría al plan SAR

[Plan principal](../auditoria-2026-09-06/08-plan-de-correccion.md) · [CSV completo con archivos originales](04-trazabilidad.csv).

Los 64 hallazgos están incluidos. Todos están pendientes de implementación/verificación. OPS-02 y OPS-03 están totalmente diferidos por la exclusión de contenerización; los componentes de despliegue de SEG-08, OPS-04, OPS-06 y OPS-08 requieren GD además de su corrección funcional local. Las pruebas indicadas son referencias al catálogo nuevo, no resultados exitosos de la auditoría.

| Hallazgo | Prioridad | Pasos | Casos de aceptación | Puerta | Estado |
|---|---|---|---|---|---|
'''+''.join(f"| {r['hallazgo']} | {r['prioridad']} | {r['pasos']} | {r['pruebas']} | {r['puerta']} | {r['estado']} |\n" for r in rows))
print('Hallazgos mapeados:',len(rows))
