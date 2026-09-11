# Sesión y transporte seguro para la LAN del hotel

Este documento fija la configuración operativa de autenticación para la instalación nativa en Windows 11. La contenerización continúa diferida. La aplicación web y la API deben publicarse desde el mismo proceso y origen HTTPS siempre que sea posible.

## Política implementada

- El token de acceso dura 15 minutos y solo vive en memoria del navegador. No se guarda usuario ni token en `localStorage` o `sessionStorage`; al cargar la nueva versión se eliminan credenciales persistentes dejadas por versiones anteriores.
- El token de renovación dura siete días, se rota en cada uso y se entrega en la cookie `hotel_erp_refresh` con `HttpOnly`, `SameSite=Strict`, ruta `/api/auth` y `Secure` fuera de desarrollo.
- La cookie legible `hotel_erp_csrf` contiene un valor aleatorio independiente. Renovar mediante cookie exige el mismo valor en `X-CSRF-Token`; la comparación del servidor es de tiempo constante.
- Login y renovación responden `Cache-Control: no-store` y nunca serializan el token de renovación en JSON. El alta administrativa de un usuario tampoco crea una sesión para esa cuenta.
- Cerrar sesión, cambiar la contraseña, desactivar el usuario o detectar reutilización de un token invalida la sesión en el servidor. El cierre y el cambio de contraseña eliminan ambas cookies.
- El navegador recupera la sesión mediante una sola renovación al arrancar. Las solicitudes que coinciden cuando vence el acceso comparten una renovación y luego se reintentan con el nuevo token. Las pestañas del mismo origen serializan la rotación con Web Locks cuando el navegador ofrece esa API, evitando consumir simultáneamente la misma cookie.
- CORS acepta credenciales únicamente desde la lista exacta `Cors:AllowedOrigins`. El arranque rechaza listas vacías, comodines, credenciales embebidas, rutas, consultas, fragmentos y valores que no sean orígenes HTTP/HTTPS.
- Production no incluye orígenes predeterminados. Los valores HTTP para Vite/localhost existen solo en `appsettings.Development.json`, por lo que no se mezclan con la lista HTTPS de la instalación.

El cuerpo de `POST /api/auth/refresh` conserva compatibilidad con un cliente no navegador que ya posea de forma segura un token de renovación. La interfaz hotelera usa exclusivamente las cookies; la API no entrega ese token en el cuerpo de login o renovación.

## Configuración recomendada en Windows 11

El certificado debe ser emitido por una CA confiable para los equipos de la red, contener en SAN el nombre usado por los operadores y estar instalado o disponible como PFX con permisos de lectura limitados a la cuenta del servicio. El ejemplo supone `hotel-erp.hotel.local` y el puerto 7443:

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Production'
$env:ASPNETCORE_URLS = 'https://0.0.0.0:7443'
$env:Kestrel__Certificates__Default__Path = 'C:\HotelERP\certs\hotel-erp.pfx'
$env:Kestrel__Certificates__Default__Password = '<CLAVE_PFX_DESDE_GESTOR_SEGURO>'
$env:AllowedHosts = 'hotel-erp.hotel.local'
$env:Cors__AllowedOrigins__0 = 'https://hotel-erp.hotel.local:7443'
```

Production no arranca hasta recibir al menos `Cors__AllowedOrigins__0`. Esto evita conservar por accidente los orígenes locales usados por Vite. Aunque frontend y API se sirvan desde el mismo origen, se declara la URL canónica para mantener una configuración explícita y permitir la comprobación de preflight.

Si el hotel decide permitir además acceso por una dirección IP fija, esa IP debe aparecer en el SAN del certificado, en `AllowedHosts` y como un origen CORS adicional con esquema y puerto exactos:

```powershell
$env:AllowedHosts = 'hotel-erp.hotel.local;192.168.50.10'
$env:Cors__AllowedOrigins__1 = 'https://192.168.50.10:7443'
```

Los secretos JWT, PostgreSQL, bootstrap y PFX se suministran por el mecanismo operativo elegido para el servicio y no se incorporan a scripts, `appsettings*.json`, accesos directos ni historial de PowerShell. La rotación operativa y el alta definitiva del servicio siguen siendo tareas de G1/G6.

`AuthenticationSession__SecureCookies=false` se admite únicamente con `ASPNETCORE_ENVIRONMENT=Development`. La aplicación aborta el arranque si se intenta desactivar en otro ambiente. Los nombres de cookies y cabecera forman parte del contrato compilado del frontend; no deben modificarse en producción sin generar y probar de nuevo el bundle.

## Límites de red

- Autorizar en Firewall de Windows el puerto HTTPS solo desde la VLAN o subred de personal del hotel.
- Bloquear el acceso desde Wi-Fi de huéspedes y redes públicas.
- Mantener PostgreSQL escuchando solo en el equipo local mientras API y base residan en la misma máquina.
- Evitar publicar un puerto HTTP operativo. La redirección a HTTPS es una ayuda para enlaces antiguos, no un sustituto de restringir el listener y el firewall.
- Distribuir y confiar la CA interna antes de entregar el sistema; no instruir a los operadores a ignorar advertencias del certificado.

## Comprobación antes de liberar

1. Abrir la URL HTTPS definitiva desde un equipo de recepción y comprobar que el navegador muestra una cadena de certificado válida.
2. Iniciar sesión, navegar al panel y recargar por completo. Debe continuar la sesión sin datos de autenticación en almacenamiento web.
3. Cerrar sesión, recargar y comprobar que vuelve a `/login`.
4. Repetir el cierre con dos pestañas: la segunda debe recibir `401`, fallar su renovación y volver a `/login` al intentar usar la API.
5. Ejecutar un preflight desde cada origen autorizado y desde un origen ajeno. Solo los autorizados deben recibir su origen exacto y `Access-Control-Allow-Credentials: true`.
6. Intentar renovar con la cookie y sin `X-CSRF-Token`, y luego con un valor diferente. Ambos intentos deben devolver `403` y no emitir acceso.
7. Ejecutar la [cadena local de QA](ejecucion-qa-local.md) con PostgreSQL de pruebas y conservar su resultado en el expediente de liberación.

La automatización cubre cookies, no exposición del refresh, CSRF, rotación/replay, logout, CORS permitido y rechazado, límite de intentos y errores seguros. La prueba del 10 de septiembre de 2026 añadió un recorrido de navegador con login, una segunda carga independiente recuperada por cookie, logout y una nueva carga rechazada. La prueba final sobre el certificado, firewall, DNS y dos equipos físicos corresponde a la instalación Windows del hotel.
