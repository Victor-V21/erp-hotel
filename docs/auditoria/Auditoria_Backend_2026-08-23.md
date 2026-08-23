# Auditoría de Backend - 23 de Agosto de 2026

## Resumen Ejecutivo
Se realizó una compilación completa y un análisis estático de código en el proyecto Backend (`hotel-erp.Api`). La compilación fue exitosa (0 errores), lo que significa que el servidor puede ejecutarse, pero se identificaron **11 advertencias (warnings)** críticas que podrían causar fallos en producción (especialmente si el servidor se ejecuta en un entorno Linux o Docker).

---

## 1. Advertencia de Plataforma (Impresión de Recibos)
**Ubicación:** `src/hotel-erp.Api/Services/EscPosService.cs` (Múltiples líneas: 298 a 324)
**Código del Error:** `CA1416`

### Descripción
El sistema utiliza la librería `System.Drawing.Common` (clases `Bitmap`, `Image.Width`, `Image.Height`, `Image.FromStream`, `GetPixel`) para procesar el logo en la impresión de tickets de caja (ESC/POS). Microsoft ha marcado estas APIs como compatibles **únicamente con Windows**. 

**Nota importante:** Dado que este ERP está diseñado **estrictamente y de forma exclusiva para Windows 11**, esta advertencia **no representa un problema** y no causará caídas. Es el comportamiento esperado.

### Solución Paso a Paso
1. **Silenciar la advertencia:** Como el sistema solo operará en Windows, puedes ignorar de forma segura esta advertencia.
2. Para evitar que el compilador siga mostrando el aviso, puedes añadir la siguiente propiedad en tu archivo `hotel-erp.Api.csproj` (dentro de `<PropertyGroup>`):
   ```xml
   <NoWarn>$(NoWarn);CA1416</NoWarn>
   ```
   Alternativamente, puedes marcar la clase o método `EscPosService` con el atributo `[SupportedOSPlatform("windows")]`.

---

## 2. Riesgo de Referencia Nula (Null Reference)
**Ubicación:** `src/hotel-erp.Api/Database/Repositories/InvoiceRepository.cs` (Línea 27)
**Código del Error:** `CS8602`

### Descripción
El compilador detectó un posible error donde el código intenta acceder a una propiedad de un objeto que podría ser `null` ("Desreferencia de una referencia posiblemente NULL").
En la línea:
```csharp
.Include(i => i.Guest).ThenInclude(g => g.Reservations)
```
Si una factura (`Invoice`) pertenece a un `Customer` en lugar de a un `Guest`, `i.Guest` será `null`. Llamar a `.ThenInclude(g => g.Reservations)` sobre un `Guest` nulo es una operación peligrosa que el sistema de tipos de Entity Framework advierte.

### Solución Paso a Paso
1. Ve a `InvoiceRepository.cs` en la línea 27.
2. Añade el operador de indulgencia (`!`) para indicarle a Entity Framework que maneje la inclusión con cuidado o divide la consulta si es estrictamente necesario.
   ```csharp
   // Opción rápida (para silenciar el warning de EF Core, ya que EF Core maneja nulls en Includes de forma segura internamente):
   .Include(i => i.Guest!).ThenInclude(g => g.Reservations)
   ```

---

## 3. Paquete Obsoleto o Innecesario
**Ubicación:** `hotel-erp.Api.csproj` (y el archivo de solución `hotel-erp.slnx`)
**Código del Error:** `NU1510`

### Descripción
El archivo del proyecto contiene una referencia al paquete `System.Text.Encoding.CodePages` que no está siendo utilizado directamente como una dependencia principal requerida, o bien, está mal referenciado.

### Solución Paso a Paso
1. Abre el archivo `hotel-erp.Api.csproj`.
2. Busca la línea:
   ```xml
   <PackageReference Include="System.Text.Encoding.CodePages" ... />
   ```
3. Bórrala.
4. Si necesitas habilitar codificaciones antiguas (como la `IBM858` que usas en `EscPosService.cs`), solo asegúrate de llamar a `Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);` al inicio de tu `Program.cs`, sin necesidad de obligar la retención del paquete si .NET ya lo resuelve internamente de otras dependencias, o asegúrate de que su versión sea correcta para .NET.

---

## Conclusión del Backend
El backend está en excelente estado general y no posee errores bloqueantes. La advertencia sobre la plataforma no es un problema dado el entorno (Windows 11). Se recomienda aplicar las 2 correcciones restantes (Null Reference y Paquete Innecesario) para tener una compilación 100% limpia.
