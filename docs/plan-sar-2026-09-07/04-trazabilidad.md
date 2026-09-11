# Trazabilidad de la auditoría al plan SAR

[Plan principal](../auditoria-2026-09-06/08-plan-de-correccion.md) · [CSV completo con archivos originales](04-trazabilidad.csv).

Los 64 hallazgos están incluidos. Todos están pendientes de implementación/verificación. OPS-02 y OPS-03 están totalmente diferidos por la exclusión de contenerización; los componentes de despliegue de SEG-08, OPS-04, OPS-06 y OPS-08 requieren GD además de su corrección funcional local. Las pruebas indicadas son referencias al catálogo nuevo, no resultados exitosos de la auditoría.

| Hallazgo | Prioridad | Pasos | Casos de aceptación | Puerta | Estado |
|---|---|---|---|---|---|
| FIN-01 | P0 | S11 S12 | NC01 NC02 NC03 CTA01 | G2 | Pendiente |
| FIN-02 | P0 | S05 S10 S12 | TX01 TX02 TX03 TX05 | G2 | Pendiente |
| FIN-03 | P1 | S08 S11 | MON10 NC01 NC04 | G2 | Pendiente |
| FIN-04 | P1 | S01 S08 S21 | MON02 MON03 REP03 | G4 | Pendiente |
| FIN-05 | P1 | S08 S10 S12 | MON08 CTA02 TX02 | G2 | Pendiente |
| FIN-06 | P1 | S09 S11 | FIS08 FIS09 FIS12 NC07 | G2 | Pendiente |
| FIN-07 | P1 | S11 S20 S21 | REP01 REP02 REP03 | G4 | Pendiente |
| FIN-08 | P1 | S12 S16 S17 | COM03 COM06 CTA04 | G3 | Pendiente |
| FIN-09 | P1 | S13 S17 | COM01 COM05 CTA04 | G3 | Pendiente |
| FIN-10 | P1 | S16 S24 | PAG02 PAG03 PAG05 PAG06 PAG08 | G3 | Pendiente |
| FIN-11 | P1 | S13 S20 | CTA07 CTA10 DAT06 | G4 | Pendiente |
| FIN-12 | P2 | S01 S12 S20 | CTA08 CTA09 | G4 | Pendiente |
| HOT-01 | P1 | S13 S14 | HOT01 HOT02 CON03 | G3 | Pendiente |
| HOT-02 | P1 | S14 S15 | HOT03 HOT05 TX01 | G3 | Pendiente |
| HOT-03 | P1 | S14 S15 S16 | HOT04 PAG01 PAG02 PAG04 | G3 | Pendiente |
| HOT-04 | P1 | S09 S15 S16 | HOT08 HOT09 HOT10 E2E03 | G3 | Pendiente |
| HOT-05 | P1 | S01 S08 S15 S24 | MON05 MON06 MON07 MON12 UI04 | G3 | Pendiente |
| HOT-06 | P1 | S13 S14 S17 S20 | DAT06 CTA07 COM05 | G4 | Pendiente |
| INV-01 | P1 | S13 S19 | CON05 INV01 INV04 | G3 | Pendiente |
| INV-02 | P2 | S12 S17 S19 | INV02 INV03 INV05 INV06 | G3 | Pendiente |
| SEG-01 | P0 | S03 S04 | SEC01 SEC03 SEC04 | G1 | Pendiente |
| SEG-02 | P0 | S04 S05 | Q06 SEC06 SEC10 | G1 | Pendiente |
| SEG-03 | P1 | S03 | SEC02 SEC03 SEC04 | G1 | Pendiente |
| SEG-04 | P1 | S03 S28 | SEC05 | G1 | Pendiente |
| SEG-05 | P1 | S04 | SEC06 SEC07 SEC08 | G1 | Pendiente |
| SEG-06 | P1 | S06 S22 | AUD01 AUD05 | G4 | Pendiente |
| SEG-07 | P1 | S10 S22 | AUD02 AUD03 AUD04 SEC11 | G4 | Pendiente |
| SEG-08 | P1 | S03 S04 D01 | SEC03 SEC11 | G1 + GD | Pendiente; componente Docker diferido |
| SEG-09 | P2 | S04 S24 | SEC08 SEC09 SEC10 UI07 | G5 | Pendiente |
| SEG-10 | P1 | S02 S04 S05 | Q04 Q06 | G1 | Pendiente |
| OPS-01 | P0 | S02 | Q01 Q02 | G1 | Pendiente |
| OPS-02 | P0 | D01 | GD: ensayo Docker posterior | GD | Diferido por alcance Docker |
| OPS-03 | P1 | D01 | GD: ensayo Docker posterior | GD | Diferido por alcance Docker |
| OPS-04 | P1 | S23 D02 | IMP03 IMP06 IMP07 | G5 + GD | Pendiente; componente Docker diferido |
| OPS-05 | P1 | S07 S09 S23 | FIS10 IMP01 IMP02 IMP05 | G4 | Pendiente |
| OPS-06 | P1 | S28 S29 D01 | OPS03 OPS07 M04 | G5 + GD | Pendiente; componente Docker diferido |
| OPS-07 | P2 | S28 | OPS04 OPS05 OPS06 | G5 | Pendiente |
| OPS-08 | P2 | S27 D01 | OPS01 OPS02 | G5 + GD | Pendiente; componente Docker diferido |
| DB-01 | P1 | S07 S10 S13 | CON01 CON02 TX02 | G2 | Pendiente |
| DB-02 | P1 | S07 | FIS01 FIS02 FIS03 FIS04 FIS05 | G2 | Pendiente |
| DB-03 | P1 | S06 S13 S20 S21 | DAT01 DAT02 DAT03 CTA09 | G4 | Pendiente |
| DB-04 | P1 | S06 S24 | API01 API02 API05 | G1 | Pendiente |
| DB-05 | P2 | S26 S27 | PERF01 PERF02 PERF03 UI15 | G5 | Pendiente |
| DB-06 | P2 | S13 S14 S19 S27 | DAT04 DAT05 DAT07 CON03 CON05 | G5 | Pendiente |
| DB-07 | P2 | S00 S13 S29 | M01 M02 M03 M05 M06 | G6 | Pendiente |
| UX-01 | P1 | S25 | UI10 | G5 | Pendiente |
| UX-02 | P1 | S25 | UI11 | G5 | Pendiente |
| UX-03 | P1 | S25 | UI08 UI09 | G5 | Pendiente |
| UX-04 | P1 | S25 | UI08 | G5 | Pendiente |
| UX-05 | P1 | S06 S11 S24 | API05 UI01 UI02 UI03 | G5 | Pendiente |
| UX-06 | P1 | S06 S08 S15 S24 | MON12 UI04 HOT10 | G5 | Pendiente |
| UX-07 | P1 | S15 S24 | HOT06 HOT07 | G5 | Pendiente |
| UX-08 | P1 | S24 S25 | UI07 UI13 UI16 | G5 | Pendiente |
| UX-09 | P2 | S26 | UI14 UI15 UI16 PERF01 | G5 | Pendiente |
| UX-10 | P2 | S03 S25 | SEC03 UI12 | G5 | Pendiente |
| UX-11 | P2 | S04 S10 S24 | UI05 TX03 TX05 SEC10 | G5 | Pendiente |
| UX-12 | P2 | S18 S21 S22 S25 | REP06 REP07 AUD05 UI12 | G5 | Pendiente |
| UX-13 | P2 | S23 | IMP04 | G4 | Pendiente |
| UX-14 | P2 | S25 | UI11 UI13 | G5 | Pendiente |
| QA-01 | P1 | S02 S30 | Q01 Q02 Q03 E2E06 | G6 | Pendiente |
| QA-02 | P2 | S05 S08 S10 S12 | Q05 MON10 TX02 | G2 | Pendiente |
| QA-03 | P2 | S00 S02 S05 S29 S30 S31 | Q01 Q06 M06 E2E06 | G6 | Pendiente |
| FIN-13 | P1 | S08 S13 | MON09 DAT07 | G2 | Pendiente |
| FIN-14 | P1 | S01 S18 S21 S25 | N03 RET04 REP06 REP07 | G4 | Pendiente |
