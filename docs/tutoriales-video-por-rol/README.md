# Tutoriales en video por rol

Capacitación breve para el uso diario de Hotel Maya Central. Cada video fue grabado sobre la interfaz real del ERP e incluye narración en español, cursor resaltado, subtítulos integrados y portada de marca.

| Rol | Contenido principal | Duración | Video |
|---|---|---:|---|
| Administrador | Panel, operación hotelera, facturación SAR, finanzas, usuarios y control | 1:27 | [Ver tutorial](administrador/hotel_erp_tutorial_administrador_es.mp4) |
| Recepción | Inicio de turno, reservaciones, check-in, estadía, facturación y caja | 1:17 | [Ver tutorial](recepcion/hotel_erp_tutorial_recepcion_es.mp4) |
| Caja | Inicio de turno, cobros, movimientos, arqueo y cierre | 0:50 | [Ver tutorial](caja/hotel_erp_tutorial_caja_es.mp4) |
| Contador | Reportes SAR, CAI, catálogo y balances, conciliación y revisión documental | 1:17 | [Ver tutorial](contador/hotel_erp_tutorial_contador_es.mp4) |

## Formato y control de calidad

- MP4 con video H.264 y audio AAC.
- Resolución 1920 × 1080 a 25 fotogramas por segundo.
- Narración sintética `es-MX-JorgeNeural`, a velocidad moderada.
- Subtítulos en español quemados en la imagen.
- Decodificación completa validada con FFmpeg.
- Duración del audio y subtítulos comprobada escena por escena.
- Revisión visual documentada en [control-calidad.json](control-calidad.json) y en las [hojas de contacto](control-calidad/).

Durante la grabación solo aparecieron respuestas esperadas del entorno: la comprobación de una sesión previa antes del acceso y la detección de impresoras, disponible únicamente en Windows. No afectaron los recorridos mostrados.

Las cuentas temporales usadas para grabar fueron desactivadas y sus contraseñas y sesiones se eliminaron al terminar.

## Archivos de producción

La carpeta [fuentes](fuentes/) contiene los guiones por rol, el grabador Playwright, el constructor del conjunto y el verificador técnico. Los archivos `escenas.json`, `manifest.json` y `registro-grabacion.json` de cada rol conservan el detalle de la producción sin incluir contraseñas.
