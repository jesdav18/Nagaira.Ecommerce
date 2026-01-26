# Nagaira Ecommerce

Documentacion profunda del proyecto. Cubre backend y frontend, con el objetivo de explicar arquitectura, flujos y puntos clave de mantenimiento. No se incluye una seccion de base de datos.

## Resumen

Nagaira Ecommerce es un sistema de comercio electronico con API administrativa y un storefront web. La solucion sigue una arquitectura por capas para separar dominio, casos de uso y persistencia, y un cliente Angular para la experiencia publica y el panel admin.

## Estructura del repositorio

- `Nagaira.Ecommerce.Api`: API HTTP (ASP.NET Core), controllers y configuracion.
- `Nagaira.Ecommerce.Application`: casos de uso, servicios y DTOs.
- `Nagaira.Ecommerce.Domain`: entidades del dominio y contratos (interfaces).
- `Nagaira.Ecommerce.Infrastructure`: persistencia, repositorios e integraciones externas.
- `nagaira-storefront`: app Angular (storefront y panel admin).

## Backend (API)

### Stack y arquitectura

- ASP.NET Core (.NET 9) + Swagger.
- EF Core como ORM, repositorios y `UnitOfWork`.
- Autenticacion JWT y roles.
- Integracion con Cloudinary para imagenes.
- Capas:
  - `Domain`: entidades y contratos.
  - `Application`: servicios y DTOs.
  - `Infrastructure`: data, repos y servicios externos.
  - `Api`: controllers, middleware, Swagger.

### Configuracion y ejecucion local

Archivo principal: `Nagaira.Ecommerce.Api/appsettings.json`.

Valores esperados:
- `Jwt:Secret`, `Jwt:Issuer`, `Jwt:ExpirationDays`
- `Cloudinary:CloudName`, `Cloudinary:ApiKey`, `Cloudinary:ApiSecret`, `Cloudinary:UploadPreset`
- `ConnectionStrings:DefaultConnection` (solo si deseas ejecutar localmente)

Ejecucion:

```powershell
dotnet run --project Nagaira.Ecommerce.Api
```

Por defecto, la API expone `http://localhost:5098` (ver `Nagaira.Ecommerce.Api/Properties/launchSettings.json`).

### Seguridad

- JWT Bearer en `Authorization: Bearer <token>`.
- Roles usados en controllers: `Admin` y `SuperAdmin`.
- CORS permitido para `http://localhost:4200` y `http://localhost:4201`.
- Headers de seguridad agregados en `Nagaira.Ecommerce.Api/Program.cs`.

### Modulos funcionales (mapa rapido)

Publico:
- `Auth`: registro, login, cambio de contrasena.
- `Products`: listado, detalle, busqueda, categorias.
- `Categories`: categorias activas.
- `Orders`: ordenes del usuario autenticado.
- `PaymentMethods`: metodos activos.

Administrativo:
- `Dashboard`: estadisticas y listados paginados.
- `Products`: CRUD, activacion/desactivacion.
- `ProductPrices`: precios por nivel.
- `ProductImages`: imagenes y principal.
- `Inventory`: kardex, balances, movimientos.
- `Offers`: ofertas y aplicaciones.
- `Reports`: ventas e inventario (export CSV).
- `Audit`: logs administrativos.
- `Suppliers` y `ProductSuppliers`: proveedores y costos.
- `AppSettings`: configuraciones del sistema.
- `PaymentMethods` y `PaymentMethodTypes`: configuracion de pagos.
- `Upload`: subida de imagenes.

### Servicios y contratos

Servicios (Application):
- En `Nagaira.Ecommerce.Application/Services` se concentran reglas de negocio y validaciones.

Repositorios (Infrastructure):
- En `Nagaira.Ecommerce.Infrastructure/Repositories` se implementa el acceso a datos.

Entidades (Domain):
- `Nagaira.Ecommerce.Domain/Entities` define el modelo del negocio.

### Flujos clave

Creacion y edicion de producto:
- Se crea/actualiza el producto base.
- Precios e imagenes se administran en endpoints dedicados.
- Inventario se mueve con movimientos (kardex).

Inventario:
- `InventoryMovement` registra entradas/salidas.
- `InventoryBalance` refleja disponible y reservado.

Ofertas:
- Incluyen criterios por productos o categorias.
- Se exponen para admin y se validan por estado/fechas.

### Observabilidad y depuracion

- Swagger disponible en entorno Development.
- Respuestas incluyen errores controlados en controllers.
- Logs basicos en subida de imagenes.

## Frontend (storefront)

### Stack

- Angular 17 (standalone components).
- Router con rutas publicas y seccion admin protegida por `adminGuard`.
- Servicios centralizados en `src/app/core/services`.

### Configuracion

Archivo de entorno: `nagaira-storefront/src/environments/environment.ts`

- `apiUrl`: URL base de la API.
- `cloudinary`: datos usados para la subida de imagenes en frontend.

### Ejecucion local

```powershell
cd nagaira-storefront
npm install
npm run start
```

App en `http://localhost:4200`.

### Estructura interna

- `core/`:
  - `services/`: acceso a API.
  - `guards/`: protecciones de ruta.
  - `interceptors/`: manejo de auth y errores.
  - `models/`: interfaces de datos.
  - `pipes/` y `utils/`: utilidades.
- `features/`:
  - Publico: home, products, categories, cart, checkout, orders, profile.
  - Admin: dashboard, products, offers, inventory, reports, audit, settings, suppliers.
- `shared/`: componentes reutilizables.

### Rutas principales

Definidas en `nagaira-storefront/src/app/app.routes.ts`.

Publico:
- `/` home
- `/products`, `/products/:id`
- `/categories`
- `/cart`, `/checkout`
- `/login`, `/register`
- `/orders`, `/profile`

Admin (con `adminGuard`):
- `/admin`
- `/admin/products` y formularios
- `/admin/kardex`
- `/admin/offers`
- `/admin/reports`
- `/admin/audit`
- `/admin/categories`
- `/admin/payment-methods`
- `/admin/settings`
- `/admin/suppliers`

### Scripts utiles

```powershell
npm run start
npm run build
npm run test
```

Nota: `ng build` usa `baseHref: /ecommerce/` en produccion (ver `nagaira-storefront/angular.json`).

## Desarrollo y mantenimiento

### Convenciones y validaciones

- DTOs en backend validan entradas con `DataAnnotations`.
- Errores se devuelven con mensajes descriptivos en controllers.
- En frontend, los formularios validan campos obligatorios y rangos.

### Debug rapido

- Backend: habilita `ASPNETCORE_ENVIRONMENT=Development` para Swagger.
- Frontend: revisa Network para errores 4xx/5xx y payloads.
- Auth: asegura token JWT valido en localStorage.

## Checklist de puesta en marcha

- Configurar `appsettings.json` con secrets y valores minimos.
- Levantar API.
- Configurar `environment.ts` con `apiUrl` correcto.
- Levantar frontend.
