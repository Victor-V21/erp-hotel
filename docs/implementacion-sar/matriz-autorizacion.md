# Matriz de autorización técnica

Esta matriz documenta la configuración que el backend aplica y que las pruebas automatizadas verifican. Los nombres de rol son identificadores visibles; la autorización efectiva se decide por permisos incluidos en el token y vuelve a validarse contra la versión de seguridad del usuario.

## Perfiles del sistema

| Perfil | `SystemKey` | Permisos predeterminados | Alcance operativo |
|---|---|---|---|
| Admin | `administrator` | Todos los permisos del catálogo | Administración, operación, fiscal, contabilidad, caja, inventario y respaldos. |
| Recepcion | `reception` | `create_invoices`, `manage_reservations`, `manage_cash` | Reservas, huéspedes, habitaciones, folios, emisión, cobro y caja. |
| Caja | `cashier` | `manage_cash` | Consulta de folios/comprobantes, cobros, devoluciones y turnos de caja. |
| Contador | `accountant` | `manage_taxes`, `manage_accounting`, `view_reports`, `export_data`, `view_audit` | Configuración fiscal, contabilidad, compras, reportes, exportación y auditoría. |

Los cuatro perfiles se crean con una identidad estable. El sembrador puede añadir permisos predeterminados que falten, pero no elimina concesiones agregadas conscientemente a una instalación existente. Los cambios de roles o permisos incrementan la versión de seguridad del usuario y cortan sus sesiones vigentes.

## Catálogo de permisos

| Permiso | Admin | Recepcion | Caja | Contador |
|---|:---:|:---:|:---:|:---:|
| `manage_users` | Sí | No | No | No |
| `manage_roles` | Sí | No | No | No |
| `manage_settings` | Sí | No | No | No |
| `manage_taxes` | Sí | No | No | Sí |
| `manage_backups` | Sí | No | No | No |
| `view_audit` | Sí | No | No | Sí |
| `export_data` | Sí | No | No | Sí |
| `manage_accounting` | Sí | No | No | Sí |
| `view_reports` | Sí | No | No | Sí |
| `create_invoices` | Sí | Sí | No | No |
| `manage_reservations` | Sí | Sí | No | No |
| `manage_cash` | Sí | Sí | Sí | No |
| `manage_inventory` | Sí | No | No | No |

## Inventario de endpoints

`Autenticado` significa que cualquier sesión activa que ya completó el cambio obligatorio de contraseña puede leer ese recurso. Las escrituras quedan separadas por política incluso cuando el controlador ofrece lectura común.

| Familia de ruta | Lecturas | Mutaciones/acciones | Justificación |
|---|---|---|---|
| `/health` | Pública | — | Sonda local sin datos de negocio. |
| `/api/auth` | `login` y `refresh` públicas con límite; `me` autenticado | `register`: `manage_users`; `logout` y cambio de contraseña: dueño/usuario de la sesión | Inicio y mantenimiento de la propia sesión no requieren un privilegio administrativo. |
| `/api/users` | `manage_users` | `manage_users` | Datos de acceso y corte de sesiones. |
| `/api/roles` | `manage_roles` | `manage_roles` | Asignación de privilegios y protección de roles del sistema. |
| `/api/settings/business` | Autenticado | actualización: `manage_settings`; aprobación/retiro fiscal: `manage_taxes` | Datos del emisor se consultan para comprobantes; su modificación está segregada. |
| `/api/tax-configurations` | Autenticado | `manage_taxes` | Las tasas vigentes se consumen en operación; solo fiscal las modifica. |
| `/api/cai` | Autenticado | `manage_taxes` | Recepción necesita consultar rangos activos para emitir; altas/bajas son fiscales. |
| `/api/document-authorizations` | Autenticado | altas/bajas/adjunto/descarga: `manage_taxes` | La existencia y rango se usan al emitir; el expediente PDF permanece privado. |
| `/api/invoices` | Autenticado | crear, editar, anular y notas: `create_invoices` | Caja y contabilidad pueden consultar; la numeración fiscal queda en Recepción/Admin. |
| `/api/folios` | Autenticado | agregar consumo: `create_invoices` | Consulta necesaria para operación y cobro; los cargos alteran la futura facturación. |
| `/api/payments` | `manage_cash` | `manage_cash` | Registra dinero y aplicaciones a documentos. |
| `/api/refunds` y `/api/payments/{id}/refunds` | `manage_cash` | `manage_cash` | Devuelve dinero y cancela saldo a favor. |
| `/api/cash-registers` | `manage_cash` | `manage_cash` | Apertura, cierre, movimientos y arqueo. |
| `/api/card-settlements` | `manage_accounting` | `manage_accounting` | Conciliación del adquirente y asiento bancario. |
| `/api/accounts` | `manage_accounting` | `manage_accounting` | Catálogo, asientos, balance y auxiliares. |
| `/api/purchase-invoices` | `manage_accounting` | `manage_accounting` | Compras afectan impuestos, cuentas por pagar y diario. |
| `/api/suppliers` | `manage_accounting` | `manage_accounting` | Maestro fiscal de compras. |
| `/api/reports` | `view_reports` | — | Estados contables y libros SAR. |
| `/api/data/export` | `export_data` | — | Exportaciones con datos personales y fiscales. |
| `/api/audit-logs` | `view_audit` | verificación de integridad: `view_audit` | Bitácora y cadena de integridad. |
| `/api/backup` | `manage_backups` | crear respaldo: `manage_backups` | El respaldo contiene toda la base. |
| `/api/reservations` | Autenticado | `manage_reservations` | Lectura común para atención, contabilidad y cobro; cambios solo operativos. |
| `/api/rooms` y `/api/room-types` | Autenticado | `manage_reservations` | Disponibilidad y tarifas se consultan al facturar; el catálogo lo gestiona operación. |
| `/api/guests` y `/api/customers` | Autenticado | `manage_reservations` | Caja/contabilidad necesitan identificar al receptor; Recepción mantiene el maestro. |
| `/api/discounts` | Autenticado | `manage_settings` | Emisión consulta reglas activas; crear o cambiar una regla es configuración. |
| `/api/inventory` | `manage_inventory` | `manage_inventory` | Movimientos y ajustes de existencias quedan aislados en permiso propio. |
| `/api/dashboard/summary` | Autenticado | — | Resumen de operación para los cuatro perfiles internos. |
| `/api/print/printers` y pruebas | `manage_settings` | impresiones de prueba: `manage_settings` | Revelan/configuran recursos de Windows y generan papel de prueba. |
| `/api/print/invoice/{id}/preview` | Autenticado | reimpresión: autenticado | Produce una copia de un comprobante persistido, no cambia sus datos ni correlativo. |

## Controles automáticos

- La reflexión de controladores falla si aparece una acción sin `[Authorize]` o `[AllowAnonymous]`.
- La reflexión de métodos `POST`, `PUT`, `PATCH` y `DELETE` falla si no existe una política de permiso. La lista excepcional está limitada a login/refresh públicos, logout, cambio de la propia contraseña y reimpresión de un comprobante ya emitido.
- La integración crea una base PostgreSQL efímera y usuarios independientes de los cuatro perfiles. Verifica los permisos exactos y respuestas `401`, `403`, `404`, `200` o `400` representativas para caja, contabilidad, adjuntos, usuarios, respaldos, auditoría, inventario, proveedores, habitaciones y descuentos.
- El frontend usa los nombres estables `Admin`, `Recepcion`, `Caja` y `Contador`; las rutas sensibles se protegen con el mismo permiso del backend. En clientes y huéspedes, Contador conserva consulta de solo lectura y no recibe controles de edición.

Esta matriz constituye la aprobación técnica automatizada de SEC02 y SEC03. La revisión técnica independiente y la aprobación organizativa de responsabilidades siguen abiertas en G1.
