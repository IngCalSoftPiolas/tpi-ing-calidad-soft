# Deudas Técnicas — Revisión del Proyecto

**Proyecto:** TPI · Desarrollo de Software
**Revisión:** 27 de agosto de 2026
**Alcance:** Backend (.NET, arquitectura por capas) y Frontend (React + Vite)

---

Este documento es un borrador de trabajo para el equipo. Reúne los hallazgos detectados en una revisión del código, pensados para que cada integrante los tome y decida cómo resolverlos. No incluye soluciones en código: cada punto indica qué se encontró, dónde está la evidencia y una idea breve de hacia dónde debería ir la corrección.

---

## 1. Backend

### 1.1 Paginación "en memoria" (ineficiente) — Importancia alta

**Qué se encontró:**
Los listados traen todos los registros de la base de datos y recién después se aplica la paginación y el filtrado en memoria del servidor, en lugar de dejar que la base de datos devuelva solo la página pedida.

**Evidencia:**
- `EfRepository.cs:46-49` — el `GetFiltered` ejecuta `.ToListAsync()`, materializando todo el conjunto en memoria.
- `ProductService.cs:121-126` — el `Skip`/`Take` se aplica después, sobre la lista ya materializada.
- `ProductModel.cs:66-72` — `PageNumber`/`PageSize` no tienen valores por defecto ni validación.
- `ProductService.cs:69-77` (`GET api/products`) y `OrderService.cs:123-137` (`GET api/orders`) no pagan en absoluto.
- `OrderService.cs:36` — consulta adicional por cliente dentro de `GetAllOrders` (N+1).

**Idea de corrección:**
Mover el filtrado y la paginación a la consulta de base de datos (devolver solo la página solicitada y un conteo total), y definir valores por defecto razonables para página y tamaño. También revisar el acceso a datos en bucle para evitar consultas repetidas.

### 1.2 Ausencia de trazabilidad / auditoría — Importancia alta

**Qué se encontró:**
No hay forma de saber quién creó o modificó un registro, y casi no hay marcas temporales, lo que impide auditar cambios.

**Evidencia:**
- `EntityBase.cs:3-10` — la base de entidades solo tiene `Id`.
- Ninguna entidad (`Product`, `Order`, `OrderItem`, `Customer`) tiene `CreatedBy`, `CreatedAt`, `UpdatedBy` o `UpdatedAt`.
- No hay interceptor de `SaveChanges` ni tabla de auditoría (`EfRepository.cs:17-56` es CRUD directo).
- `Order.cs:23` — única fecha del sistema, `DateTime.Now`, sin quién la generó.
- Confirmado en las migraciones: las tablas no tienen columnas de auditoría.

**Idea de corrección:**
Agregar campos de auditoría (creación y última modificación con usuario y fecha) a las entidades, e idealmente un mecanismo central que los rellene automáticamente al guardar (por ejemplo, aprovechando el contexto de persistencia y el usuario autenticado).

### 1.3 Lógica de negocio en la capa de presentación — Importancia alta

**Qué se encontró:**
Los controladores contienen reglas de negocio e inyectan directamente infraestructura de datos, en lugar de delegar el dominio a la capa de aplicación.

**Evidencia:**
- `AuthenticateController.cs:88-92` — el controlador crea un `Customer` e inyecta `IRepository`.
- `AuthenticateController.cs:22-33` — inyección directa de `IRepository`.
- `AuthenticateController.cs:72-86` — decide y asigna el rol dentro del controlador.
- `Application.csproj:11` — la capa `Application` depende de `Data`; la interfaz `IRepository` vive en `Data` (inversión de dependencia rota).
- `OrderService.cs:18-22` — dependencia muerta (`IProductService` inyectado que no se usa).

**Idea de corrección:**
Trasladar la lógica de negocio a servicios de aplicación y dejar los controladores como orquestadores de entrada/salida HTTP. Revisar hacia dónde apuntan las dependencias para que las capas internas no dependan de la infraestructura.

### 1.4 Manejo de errores inconsistente — Importancia alta

**Qué se encontró:**
Cada endpoint maneja errores de forma distinta, se convierten errores internos en respuestas engañosas y los códigos HTTP no son consistentes.

**Evidencia:**
- `ProductsController.cs:106-109` — cualquier excepción se convierte en `NotFound` (404), incluso errores internos reales (500, caídas de DB).
- `OrdersController.cs:51-53` — chequea `order == null` pero el servicio lanza excepción, por lo que una orden inexistente da 500 en vez de 404.
- `Program.cs:166-188` — el handler de excepciones solo produce 500 genérico, sin mapear las excepciones propias al código correcto.
- Respuestas inconsistentes entre `GetAllProducts` (`NoContent`) y `GetAllOrders` (`Ok`).
- Doble validación: `[ApiController]` + `ModelState.IsValid` manual.

**Idea de corrección:**
Centralizar el manejo de errores (por ejemplo, en un middleware que traduzca cada tipo de excepción a su código HTTP) y homogeneizar el formato de las respuestas de error y de los listados.

### 1.5 Escalada de privilegios en el registro — Importancia crítica

**Qué se encontró:**
Cualquier persona anónima puede registrarse con el rol que elija (incluso `MASTER` o `ADMIN`), porque el registro acepta el rol desde el cuerpo de la petición sin verificar permisos.

**Evidencia:**
- `AuthenticateController.cs:67-68` — `register` es `[AllowAnonymous]`.
- `AuthenticateController.cs:72-75` — parsea el rol del body.
- `AuthenticateController.cs:86` — asigna el rol directamente.
- `RegisterModel.cs:9` — el rol llega como string libre, sin validación.
- Relacionado: `ProductsController.cs:96` — `GET api/products/admin` está `[AllowAnonymous]`.

**Idea de corrección:**
No permitir que el cliente elija el rol en el registro; asignar siempre un rol bajo por defecto y dejar la asignación de roles superiores a una acción administrativa autenticada. Revisar además los endpoints marcados como anónimos.

### 1.6 Falta de transacciones — consistencia de datos — Importancia crítica

**Qué se encontró:**
Crear una orden persiste en varias transacciones separadas sin una transacción contenedora. Si falla un paso intermedio, queda una orden guardada con stock parcialmente descontado.

**Evidencia:**
- `OrderService.cs:82-90` — guarda la orden y luego descuenta stock producto por producto, cada uno con su propio `SaveChanges`.
- No existe `BeginTransaction`/`TransactionScope` (búsqueda global sin coincidencias).
- `AuthenticateController.cs:79-94` — el registro crea el usuario y después asigna el rol sin rollback.

**Idea de corrección:**
Agrupar las operaciones relacionadas (crear orden + descontar stock) en una misma transacción para que, ante un error, todo retroceda de forma consistente. Aplicar el mismo criterio en el registro de usuarios con asignación de rol.

### 1.7 Magic strings y reglas duplicadas — Importancia media

**Qué se encontró:**
Roles, estados y rutas de relación se repiten como literales en todo el código, en formas distintas, lo que propicia errores y dificulta el mantenimiento.

**Evidencia:**
- Roles definidos de tres formas: `"Master, Admin"` en controladores, enum `ADMIN/CLIENT/MASTER` (`ValidRoles.cs:9-14`), seed `"Admin"` (`Program.cs:206`).
- Estados de producto con dos convenciones: `"enabled/disabled"` (`ProductService.cs:108-112`) y `"active/disabled"` (`ProductService.cs:36-40`).
- Rutas de include como strings (`"OrderItems"`, `"OrderItems.Product"`) en `OrderService.cs:116,125,151`.
- Mapeos manuales duplicados (el `OrderResponseGenerator` se reescribe a mano en `CreateOrder`, `OrderService.cs:94-111`).
- Typo `BillingAdress` vs `BillingAddress` (`OrderModel.cs:20`); columna `EMail` (`Customer.cs:17`); estado como string en los DTOs de respuesta.

**Idea de corrección:**
Definir valores únicos (roles, estados) en un solo lugar y reutilizarlos; unificar las convenciones y consolidar los mapeos a un único método que se reutilice en todas partes.

### 1.8 Secretos, configuración frágil y residuos — Importancia media

**Qué se encontró:**
La configuración deja secretos en el repositorio, el arranque falla fuera del entorno de desarrollo y hay código y esqueleto sin usar.

**Evidencia:**
- `appsettings.Development.json:9` — clave de firma JWT en el código fuente.
- `appsettings.json` no tiene sección `Jwt` y `Program.cs:81` lanza una excepción en producción por configuración incompleta.
- `Program.cs:210-214` — llamadas bloqueantes en el arranque (sync-over-async).
- Residuos: `ClassLibrary1` (huérfana), `.github/workflows/` vacío, DbSets comentados (`Dsw2025TpiContext.cs:11-14`), paquete `JWT` sin usar, `using Azure.Core;` innecesario, encodings corruptos (errores en `Program.cs` y seed `clients.json`).
- No existe proyecto de pruebas.

**Idea de corrección:**
Sacar los secretos de los archivos versionados y dejar la configuración lista para producción, además de limpiar los proyectos, paquetes y archivos sin uso, y cuidar la codificación de los textos. Evaluar agregar pruebas automatizadas.

---

## 2. Frontend

### 2.1 Listados traen todo y filtran en el cliente — Importancia alta

**Qué se encontró:**
El listado de órdenes descarga todos los registros y recién filtra en el navegador, en lugar de paginar y filtrar en el servidor. En la home, además, la paginación se anula en móviles.

**Evidencia:**
- `listOrders.js:3-10` — `getOrders()` hace `GET /api/orders` sin parámetros de página ni búsqueda.
- `ListOrdersPage.jsx:24-64` — filtra con `orders.filter(...)` en el cliente.
- `ListOrdersPage.jsx:37-42` — el "debaunce" con `setTimeout` no envía nada al servidor (llamada sin argumentos).
- `HomePage.jsx:94-108` — en móvil fuerza `pageSize = total`, cancelando la paginación.
- `AdminHomePage.jsx:15-26` — usa un tamaño por defecto que puede dar un total incorrecto si hay más de 20 productos.

**Idea de corrección:**
Enviar la página y los criterios de búsqueda al servidor y renderizar lo que este devuelva, en lugar de traer todo y filtrar localmente. Evitar el comportamiento que desactiva la paginación en móviles.

### 2.2 Lógica de negocio en componentes y fetch duplicado — Importancia media

**Qué se encontró:**
La lógica de carga de datos está mezclada con los componentes y se repite casi idéntica en varias páginas, en lugar de estar centralizada.

**Evidencia:**
- El patrón `useCallback + useEffect + setTimeout + console.error + loading` se repite en 4 archivos: `HomePage.jsx:72-92`, `ListProductsPage.jsx:25-46`, `ListOrdersPage.jsx:24-42`, `AdminHomePage.jsx:15-44`.
- No hay un hook o capa de data-fetching reutilizable.
- `CartPage.jsx:23-68` — arma el payload de negocio y lee `localStorage` directo en el componente.
- `CreateProductsForm.jsx:15-23` — arma el payload y hace log del mismo en el componente.

**Idea de corrección:**
Extraer la lógica de carga de datos a un hook o servicio reutilizable y dejar los componentes enfocados en presentar y capturar la interacción. Mover el armado de payloads de negocio a la capa de servicios.

### 2.3 Errores tragados o sin feedback al usuario — Importancia media

**Qué se encontró:**
Muchos errores solo se registran con `console.error`; el usuario no se entera de qué falló, y el manejo es inconsistente entre servicios.

**Evidencia:**
- Errores tragados en `ListProductsPage.jsx:32-33`, `HomePage.jsx:79-80`, `ListOrdersPage.jsx:31`, `AdminHomePage.jsx:22,35`.
- `axiosInstance.js:24-36` — ante 401 (fuera de `/admin`) solo borra el token sin redirigir ni avisar: la sesión expira en silencio.
- Los servicios devuelven errores con formas distintas (`login.js:12`, `register.js:14`, `listOrders.js:8`).
- `listOrders.js:8` — accede a `error.response.data` sin protección; ante un error de red esto lanza.

**Idea de corrección:**
Estandarizar el formato de error (un único "envelope") en el interceptor o en los servicios, mostrar feedback claro al usuario cuando una acción falla, y proteger el acceso a los campos de error.

### 2.4 Autorización débil / token expuesto — Importancia alta

**Qué se encontró:**
El acceso a las rutas administrativas se decide solo por la existencia de un token en `localStorage`, sin verificar rol ni expiración, y el token queda expuesto.

**Evidencia:**
- `ProtectedRoute.jsx:4-13` — solo comprueba que exista el token; cualquier cliente con un token puede entrar a `/admin/**`.
- `AuthProvider.jsx:8-11` — `isAuthenticated` no valida expiración.
- `AuthProvider.jsx:26-27` — token y `customerId` en `localStorage` (expuestos a XSS).
- `axiosInstance.js:5` — `withCredentials` activo siempre.

**Idea de corrección:**
Verificar el rol y la validez del token antes de permitir el acceso a rutas administrativas, además de revisar dónde se almacena el token y qué mecanismo se usa para conservar la sesión de forma más segura.

### 2.5 Bug de persistencia del carrito — Importancia alta

**Qué se encontró:**
El carrito no persiste al quitar un ítem por un error en la escritura a `localStorage`.

**Evidencia:**
- `CartProvider.jsx:84` — `localStorage.setItem(JSON.stringify(newItems))`: usa `setItem` con un solo argumento y la clave mal formada (debería ser `setItem('cartItems', JSON.stringify(newItems))`).
- Contrasta con `clearCart` (`CartProvider.jsx:93`) que sí usa `removeItem('cartItems')` correctamente.

**Idea de corrección:**
Corregir la llamada a `localStorage` para que guarde con la clave correcta y el valor serializado, y así el carrito persista entre vistas.

### 2.6 Código muerto y residuos — Importancia baja/media

**Qué se encontró:**
Quedan archivos, módulos y bloques sin uso que confunden el mantenimiento y ensucian el proyecto.

**Evidencia:**
- Módulo `clients` completo sin uso (`clients/components/ProductCard.jsx`).
- `HomePageStyles.css` sin importar.
- Varios `useEffect` comentados en `CartProvider.jsx:16-48`.
- `console.log`/`console.debug` dejados, incluido el payload del producto (`CreateProductsForm.jsx:22`) y debug en producción (`ProtectedRoute.jsx:7`).
- 13 errores + 3 warnings de ESLint (imports y variables sin usar).
- Dos CSS duplicados idénticos (`DashboardPageAdminStyles.css` y `HomePageStyles.css`).

**Idea de corrección:**
Eliminar archivos, módulos, bloques comentados y logs sin uso, y resolver las advertencias del linter.

### 2.7 `new Error` sin `throw` — Importancia baja

**Qué se encontró:**
Una guardia de contexto no funciona porque el error se crea pero nunca se lanza.

**Evidencia:**
- `useAuth.js:7-9` — `new Error('useAuth no debe ser usado por fuera de AuthProvider')` sin `throw`.

**Idea de corrección:**
Agregar el `throw` para que, si el hook se usara fuera del proveedor, falle con un mensaje claro en lugar de un crash confuso.

### 2.8 Sin tipos, duplicación y configuración — Importancia baja/media

**Qué se encontró:**
Falta tipado, hay componentes duplicados y la configuración de entorno/producción está incompleta.

**Evidencia:**
- Sin TypeScript ni PropTypes, aunque `@types/react` está en dependencias; los componentes adivinan la forma de los datos (p. ej. `ProductCard.jsx:20`, `CartProvider.jsx:7`).
- `ProductCard` duplicado; header y `getLinkStyles` copiados en `HomePage.jsx`, `CartPage.jsx` y `DashboardPageAdmin.jsx`.
- Inconsistencia de moneda: carrito en `USD` (`CartPage.jsx:19`) vs el resto en `ARS` (`ProductCard.jsx:9`).
- URLs de íconos remotas hardcodeadas en vez de `lucide-react` (instalado).
- No hay `.env` ni `.env.example` para `VITE_BACKEND_URL` (`axiosInstance.js:4`); `.gitignore` no contempla `.env`.
- `README.md` es el template por defecto de Vite.

**Idea de corrección:**
Acordar si se incorpora tipado en los datos que cruzan componentes, extraer los componentes y estilos repetidos, unificar la moneda, usar la librería de íconos y documentar/configurar la URL base de la API por entorno.

---

---
*Borrador de revisión sobre el código de las carpetas `Dsw2025Tpi` (backend) y `frontTpiDsw` (frontend). Listo para repartir entre el equipo.*
