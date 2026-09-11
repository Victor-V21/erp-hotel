# Seguridad, permisos y privacidad

### SEG-01 · P0 · Recepción puede convertirse en administrador modificando roles

**Ubicación:** [backend/src/hotel-erp.Api/Controllers/RolesController.cs:12](/home/vm/Projects/erp-hotel/backend/src/hotel-erp.Api/Controllers/RolesController.cs:12); [backend/src/hotel-erp.Api/Controllers/RolesController.cs:73](/home/vm/Projects/erp-hotel/backend/src/hotel-erp.Api/Controllers/RolesController.cs:73); [backend/src/hotel-erp.Api/Services/AuthService.cs:220](/home/vm/Projects/erp-hotel/backend/src/hotel-erp.Api/Services/AuthService.cs:220)

**Evidencia y alcance:** T42 reproducido: recepción renombra Admin a AdminAnterior, renombra Recepcion a Admin y refresca su sesión. Recibe rol Admin y GET /audit-logs devuelve 200. El controller de roles solo exige autenticación.

**Impacto:** Escalación completa: quien tenga una cuenta operativa puede administrar usuarios y acceder a información reservada. No requiere acceso al sistema operativo.

**Corrección propuesta:** Políticas de autorización de servidor para administración de roles; identificadores estables de roles privilegiados, nombres no usados como identidad mutable y protección de último administrador. Auditoría y revocación de sesiones al cambiar privilegios.

**Criterio de cierre:** Recepción y Contador reciben 403 en POST/PUT/DELETE roles; no pueden alterar permisos o nombres. Pruebas negativas por cada endpoint administrativo.

### SEG-02 · P0 · Credenciales iniciales y clave de firma están publicadas en el proyecto

**Ubicación:** [backend/src/hotel-erp.Api/appsettings.json:14](/home/vm/Projects/erp-hotel/backend/src/hotel-erp.Api/appsettings.json:14); [docker/docker-compose.yml:28](/home/vm/Projects/erp-hotel/docker/docker-compose.yml:28); [backend/src/hotel-erp.Api/Program.cs:311](/home/vm/Projects/erp-hotel/backend/src/hotel-erp.Api/Program.cs:311)

**Evidencia y alcance:** T01: el arranque nuevo permite el usuario/contraseña predecibles del seed. appsettings y Compose contienen claveJWT y contraseña de base literales. No se vuelven a copiar esos secretos en este informe.

**Impacto:** Si se despliega con esos valores, se puede entrar como admin y la clave de firma conocida permite fabricar tokens. La red local del hotel no constituye autorización.

**Corrección propuesta:** Rotar secretos, incluir historial del repositorio en la evaluación de exposición; configuración externa protegida, bootstrap de un solo uso y cambio obligatorio de contraseña. Fallar el arranque si sigue la clave de ejemplo.

**Criterio de cierre:** Instalación limpia no acepta contraseña compartida; tokens firmados con clave anterior se rechazan; escaneo de secretos no encuentra credenciales operativas versionadas.

### SEG-03 · P1 · Permisos declarados no se aplican a operaciones sensibles

**Ubicación:** [backend/src/hotel-erp.Api/Program.cs:113](/home/vm/Projects/erp-hotel/backend/src/hotel-erp.Api/Program.cs:113); [backend/src/hotel-erp.Api/Controllers/AccountingController.cs:12](/home/vm/Projects/erp-hotel/backend/src/hotel-erp.Api/Controllers/AccountingController.cs:12); [backend/src/hotel-erp.Api/Controllers/TaxConfigurationsController.cs:13](/home/vm/Projects/erp-hotel/backend/src/hotel-erp.Api/Controllers/TaxConfigurationsController.cs:13)

**Evidencia y alcance:** T25: recepción crea una cuenta contable y exporta huéspedes; otros endpoints fiscales, inventario, reportes y configuración solo requieren autenticación. Los claims de permisos no tienen políticas que los utilicen. Ocultar menús no protege una API.

**Impacto:** Cambios contables/fiscales y extracciones de datos fuera de las funciones del usuario; facilita fraude interno y errores accidentales.

**Corrección propuesta:** Matriz servidor recurso/acción/rol con denegación por defecto, separando consulta, emisión, ajustes, exportación, configuración y administración. Reutilizarla para navegación y backend.

**Criterio de cierre:** Tabla de pruebas para anónimo/recepción/contador/admin con 401/403/éxito esperado; ningún permiso se sustenta solo en la interfaz.

### SEG-04 · P1 · Recepción puede descargar un respaldo completo de PostgreSQL

**Ubicación:** [backend/src/hotel-erp.Api/Controllers/BackupController.cs:11](/home/vm/Projects/erp-hotel/backend/src/hotel-erp.Api/Controllers/BackupController.cs:11); [backend/src/hotel-erp.Api/Controllers/BackupController.cs:23](/home/vm/Projects/erp-hotel/backend/src/hotel-erp.Api/Controllers/BackupController.cs:23)

**Evidencia y alcance:** T41: cuentaRecepcion solicita respaldo manual y recibe 200 con el dump. Contiene todas las tablas, incluidas credenciales derivadas y sesiones. T25 también accede a logs de respaldo.

**Impacto:** Permite sacar el conjunto completo de datos personales y material de autenticación desde una cuenta operativa.

**Corrección propuesta:** Restringir generación y descarga a administración autorizada, registrar acceso, retención y almacenamiento cifrado. La exportación operativa debe limitarse al conjunto necesario.

**Criterio de cierre:** Recepción403 tanto al generar como descargar/listar; respaldo autorizado deja evento de auditoría y no expone rutas internas al resto.

### SEG-05 · P1 · Desactivar un usuario no invalida su token de acceso

**Ubicación:** [backend/src/hotel-erp.Api/Services/AuthService.cs:121](/home/vm/Projects/erp-hotel/backend/src/hotel-erp.Api/Services/AuthService.cs:121); [backend/src/hotel-erp.Api/Program.cs:86](/home/vm/Projects/erp-hotel/backend/src/hotel-erp.Api/Program.cs:86); [frontend/src/store/authStore.ts:43](/home/vm/Projects/erp-hotel/frontend/src/store/authStore.ts:43)

**Evidencia y alcance:** T26: token de usuario desactivado sigue obteniendo200. Revocación afecta refresh tokens; JwtBearer no comprueba estado/version del usuario. Logout del frontend limpia almacenamiento sin llamar a la revocación del backend; axios mantiene Authorization por defecto.

**Impacto:** Acceso continúa hasta expiración del token, con ventana aproximada configurada de 60 minutos más tolerancia. Roles/usuario en Zustand pueden quedar desactualizados tras refresh.

**Corrección propuesta:** Versión de sesión/seguridad validada por servidor en operaciones protegidas; invalidar al desactivar, cambiar credenciales o privilegios. Logout revoca sesión, limpia cabeceras y sincroniza pestañas.

**Criterio de cierre:** Token previo devuelve 401 inmediatamente tras desactivación/cambio de seguridad; refresh y pestañas abiertas se actualizan coherentemente.

### SEG-06 · P1 · Bitácora declara corrupta su propia cadena sin manipulación

**Ubicación:** [backend/src/hotel-erp.Api/Services/AuditService.cs:42](/home/vm/Projects/erp-hotel/backend/src/hotel-erp.Api/Services/AuditService.cs:42); [backend/src/hotel-erp.Api/Controllers/AuditLogsController.cs:146](/home/vm/Projects/erp-hotel/backend/src/hotel-erp.Api/Controllers/AuditLogsController.cs:146); [backend/src/hotel-erp.Api/Migrations/20260820161833_InitialPostgresCreate.cs:24](/home/vm/Projects/erp-hotel/backend/src/hotel-erp.Api/Migrations/20260820161833_InitialPostgresCreate.cs:24)

**Evidencia y alcance:** T31: verificación falla en primer registro recién creado. Comprobación final reconstruye el hash con 2026-09-07T05:35:04.9015318Z; PostgreSQL almacenó 2026-09-07T05:35:04.901531. Se pierde precisión de 100ns y representación de zona antes de verificar.

**Impacto:** Falsas alarmas de manipulación; la cadena no ofrece la garantía que anuncia la interfaz.

**Corrección propuesta:** Serialización canónica estable antes de persistir y firmar, con precisión PostgreSQL y zona explícita. Probar round-trip de datos antes de habilitar la verificación.

**Criterio de cierre:** Cadena nueva verifica íntegra después de reiniciar, restaurar y cambiar zona del host; una modificación real se detecta en el registro exacto.

### SEG-07 · P1 · La auditoría no es completa ni resistente a modificaciones privilegiadas

**Ubicación:** [backend/src/hotel-erp.Api/Services/AuditService.cs:25](/home/vm/Projects/erp-hotel/backend/src/hotel-erp.Api/Services/AuditService.cs:25); [backend/src/hotel-erp.Api/Controllers/AuditLogsController.cs:86](/home/vm/Projects/erp-hotel/backend/src/hotel-erp.Api/Controllers/AuditLogsController.cs:86); [backend/src/hotel-erp.Api/Controllers/RolesController.cs:68](/home/vm/Projects/erp-hotel/backend/src/hotel-erp.Api/Controllers/RolesController.cs:68)

**Evidencia y alcance:** T30 muestra cobertura principalmente de login/facturas mientras cambios de roles, caja, inventario y otros flujos no generan eventos equivalentes. Leer el último hash sin exclusión permite bifurcaciones concurrentes. Misma cuenta de BD tiene permisos para reescribir datos y cadena.

**Impacto:** No se puede reconstruir quién cambió datos críticos ni distinguir una reescritura completa. SHA256 por sí solo no es una firma digital ni inmutabilidad frente al administrador de la BD.

**Corrección propuesta:** Auditar todas las mutaciones críticas y fallos de autenticación; secuencia determinista serializada, almacenamiento append-only con privilegios separados y anclaje/copia externa. No registrar secretos en Changes.

**Criterio de cierre:** Operaciones simultáneas mantienen una sola cadena; toda acción privilegiada tiene actor, antes/después, hora y correlación. Intento de UPDATE con usuario API es denegado.

### SEG-08 · P1 · Puertos y privilegios exponen innecesariamente la base en la LAN

**Ubicación:** [docker/docker-compose.yml:12](/home/vm/Projects/erp-hotel/docker/docker-compose.yml:12); [docker/docker-compose.yml:7](/home/vm/Projects/erp-hotel/docker/docker-compose.yml:7); [docker/nginx/nginx.conf:13](/home/vm/Projects/erp-hotel/docker/nginx/nginx.conf:13)

**Evidencia y alcance:** Compose publica 5432, 8080, 3000 y 80 en todas las interfaces; API usa usuario postgres con contraseña literal. No se configura TLS en el proxy. Include Error Detail=true incrementa detalle de errores de BD.

**Impacto:** Equipos de la LAN pueden acceder directamente a servicios internos; HTTP deja expuestos credenciales/tokens al tráfico interceptado. Un fallo de API dispone de permisos de superusuario de BD.

**Corrección propuesta:** Publicar únicamente proxy en la interfaz administrativa/LAN necesaria, TLS con confianza local, firewall/VLAN separada de huéspedes. Usuario API de mínimo privilegio, migraciones con credencial separada; quitar detalle sensible en producción.

**Criterio de cierre:** Desde puesto del hotel solo se alcanza proxy;5432/8080 inaccesibles por red. Usuario API no crea roles ni cambia esquema y el login viaja cifrado.

### SEG-09 · P2 · Endurecimiento de sesión, archivos y errores incompleto

**Ubicación:** [frontend/src/lib/axios.ts:16](/home/vm/Projects/erp-hotel/frontend/src/lib/axios.ts:16); [backend/src/hotel-erp.Api/Controllers/DocumentAuthorizationsController.cs:122](/home/vm/Projects/erp-hotel/backend/src/hotel-erp.Api/Controllers/DocumentAuthorizationsController.cs:122); [backend/src/hotel-erp.Api/Program.cs:66](/home/vm/Projects/erp-hotel/backend/src/hotel-erp.Api/Program.cs:66)

**Evidencia y alcance:** Revisión estática: tokens accesibles a JavaScript en localStorage; faltan límites de frecuencia por origen aunque existe bloqueo por fallos de contraseña. Adjunto fiscal valida principalmente extensión, usa datos del CAI en nombre y no explicita tamaño/contenido seguro. No se confirmó escritura por traversal ni XSS explotable.

**Impacto:** Amplía impacto de un futuro XSS y facilita consumo de recursos/errores de archivo. Mensajes de validación y 500 son inconsistentes para el cliente.

**Corrección propuesta:** Evaluar sesión con cookie HttpOnly/SameSite y CSRF si se adopta; CSP y límites de login. Nombre aleatorio de adjunto, confinamiento de ruta, tamaño y firma PDF; ProblemDetails estable sin trazas sensibles.

**Criterio de cierre:** Archivo inválido/sobredimensionado es rechazado antes de guardar; no sale de Uploads;429 ante abuso y mensajes de error seguros y accionables.

### SEG-10 · P1 · Repositorios y lockfiles incluyen material sensible o vulnerable

**Ubicación:** [frontend/package-lock.json:1516](/home/vm/Projects/erp-hotel/frontend/package-lock.json:1516); [backend/src/hotel-erp.Api/hotel-erp.Api.csproj:15](/home/vm/Projects/erp-hotel/backend/src/hotel-erp.Api/hotel-erp.Api.csproj:15); [.gitignore:4](/home/vm/Projects/erp-hotel/.gitignore:4)

**Evidencia y alcance:** npm audit sobre package-lock:10 paquetes afectados (8 high, 1 moderate, 1 low); NuGet: AutoMapper 12.0.1 y Microsoft.OpenApi 2.0.0 con avisos high. node_modules real usa, por ejemplo, axios1.19.0, router-dom7.18.2 y Vite 8.2.1, distintos del lock npm. SQLite versionado contiene 1 usuario y 7 registros de refresh, sin huéspedes ni facturas; hay logs y bin/obj versionados.

**Impacto:** Reinstalar con npm puede introducir versiones distintas de las probadas. El historial conserva credenciales derivadas y artefactos sin procedencia clara. Aviso de dependencia no equivale a explotación demostrada: SSR/RSC/Node-adapter no están expuestos por esta SPA.

**Corrección propuesta:** Elegir un gestor y lockfile, instalación inmutable, actualizar versiones compatibles y repetir pruebas. Retirar de seguimiento datos/logs/binarios generados conservando respaldo privado; rotar material potencialmente expuesto. Clasificar alcanzabilidad de cada aviso.

**Criterio de cierre:** Build limpio usa versiones inventariadas, audit no contiene avisos sin evaluación, no se versionan BD/sesiones/logs; ejecutar regresión de mapeos y autenticación tras actualizar.


<!-- ANEXO VERIFICADO -->

## Revisión de prácticas maliciosas

Se revisaron puntos de ejecución de procesos, SQL manual, URLs, configuración, hooks, autenticación y exportaciones. Los procesos externos identificados corresponden a `pg_dump` y a subida de respaldos mediante configuración/rclone. La solicitud externa de la UI esGoogleFonts; las APIs de negocio usan servidor relativo. Los hooks de editores ejecutan scriptsImpeccable de rutas personalesWindows cuando existen: son deuda de portabilidad, no prueba de un programa malicioso.

No se encontró `dangerouslySetInnerHTML`/`eval` usado para ejecutar datos del huésped en las fuentes revisadas. SQL manual de los locks usa interpolación parametrizada de EF. No se confirmó inyección SQL, ejecución remota, exfiltración oculta o un traversal que escriba fuera de Uploads. Esas conclusiones son limitadas a las fuentes y patrones inspeccionados; no incluyen descompilar binarios ni revisar internamente todos los paquetes de terceros.

La escalaciónSEG-01 y descargaSEG-04 sí fueron ejecutadas con cuenta operativa en laboratorio. Son capacidad de abuso demostrada, sin inferir intención del autor. Una red LAN con varios usuarios necesita verificar permisos en cada solicitud; véase[OWASP Authorization](https://cheatsheetseries.owasp.org/cheatsheets/Authorization_Cheat_Sheet.html).

## Dependencias: alcance real del aviso

[Resumen de versiones](evidencia/dependencias-resumen.json), [npm audit](evidencia/npm-audit.json) y [NuGet](evidencia/nuget-vulnerabilidades.log) contienen resultados de consulta en esta sesión.

| Componente | Evaluación |
|---|---|
|AutoMapper 12.0.1|Aviso de recursión profunda/DoS; no se envió una carga para derribar el proceso. Revisar profundidad y rutas de objetos controlables. Actualizar una versión mayor requiere revisar compatibilidad y condiciones de uso. [Aviso del mantenedor](https://github.com/LuckyPennySoftware/AutoMapper/security/advisories/GHSA-rvv3-g6hj-g44x) |
|Microsoft.OpenApi 2.0.0|Aviso por leer documentosOpen API no confiables con referencias circulares; aquí se generaOpen API en Development y no se identificó importación pública de especificaciones. Paquete afectado, explotación HTTP no demostrada. [Aviso de Microsoft](https://github.com/microsoft/OpenAPI.NET/security/advisories/GHSA-v5pm-xwqc-g5wc) |
|Axios/router/Vite del locknpm|El scanner se refiere al locknpm; instalación pnpm usa versiones distintas. Avisos de SSR/RSC o adaptadorNode no se atribuyen automáticamente a esta SPA de navegador. Vite de desarrollo no debería publicarse como servidor productivo |
|Babel, brace-expansion, browserslist, postcss, nanoid, form-data|Revisar alcance build/Node y entradas no confiables; no sumar cada advisory como vulnerabilidad remota del hotel |

La SQLite versionada contiene 1 usuario, 7 registros RefreshTokens y 2AuditLogs; no contiene huéspedes ni facturas. Se conservaron solo conteos/esquema en el informe, no valores de contraseñas, sesiones o cambios del historial. Se debe evaluar y rotar material expuesto aun cuando la base sea antigua.
