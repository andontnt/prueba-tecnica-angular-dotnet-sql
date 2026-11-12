# Bonos de desempeño

Módulo de Recursos Humanos para registrar bonos de desempeño: se busca un
empleado, se le registra un bono para un periodo (monto fijo o porcentaje del
sueldo) con un motivo, y el sistema calcula el monto bruto, la retención y el
neto a pagar.

| Capa          | Tecnología                                            |
| ------------- | ----------------------------------------------------- |
| Base de datos | SQL Server 2022 (contenedor con Podman o Docker)      |
| Backend       | .NET 10, ASP.NET Core Web API, Entity Framework Core  |
| Frontend      | Angular 22 (componentes standalone), Angular Material |

## Cómo levantar el proyecto

### Requisitos

- Podman (o Docker, cambiando `podman` por `docker` en `scripts/bonus.sh`)
- .NET 10 SDK
- Node.js 22 o superior

### 1. Base de datos

```bash
./scripts/bonus.sh db      # crea y levanta el contenedor de SQL Server (solo la primera vez)
./scripts/bonus.sh seed    # ejecuta database/01_schema_seed.sql
./scripts/bonus.sh bonus   # ejecuta database/02_bonus.sql
```

Si el contenedor ya existe (por ejemplo, después de reiniciar la máquina):

```bash
./scripts/bonus.sh start
```

Los scripts `01` y `02` se ejecutan una sola vez: si las tablas ya existen,
fallan.

### 2. API

```bash
dotnet run --project backend/src/BonusTest.Api
```

La API queda en `http://localhost:5106` y Swagger en
`http://localhost:5106/swagger`. La cadena de conexión está en
`backend/src/BonusTest.Api/appsettings.json` (`ConnectionStrings:BonusTest`) y
apunta al contenedor (`localhost,1433`, usuario `sa`).

### 3. Frontend

```bash
cd frontend
npm install
npm start
```

La aplicación queda en `http://localhost:4200`. Envía el usuario que opera en el
header `UserCode` (configurado en `src/environments/environment.ts`).

## Endpoints

| Método | Ruta                                                       | Qué hace                     |
| ------ | ---------------------------------------------------------- | ---------------------------- |
| GET    | `/api/v1/Bonus`                                            | Listado paginado con filtros |
| GET    | `/api/v1/Bonus/employee/{departmentCode}/{employeeNumber}` | Busca un empleado activo     |
| POST   | `/api/v1/Bonus`                                            | Registra un bono (201)       |
| PUT    | `/api/v1/Bonus/{bonusId}/cancel`                           | Anula un bono (0201 -> 0203) |
| DELETE | `/api/v1/Bonus/{bonusId}`                                  | Elimina un bono (lógico)     |

Los errores de reglas de negocio responden **409** con `{ code, description }`.

## Decisiones técnicas

### Base de datos (`database/02_bonus.sql`)

- **Foto del sueldo y la retención:** `Bonus` guarda `MonthlySalary` y `TaxRate`
  del momento del registro. `Employee` solo tiene los valores actuales y un bono
  es un hecho histórico de dinero: si mañana cambia el sueldo, los bonos
  anteriores no deben cambiar.
- **Montos calculados guardados:** bruto, retención y neto se calculan una vez
  en el backend y se guardan (`DECIMAL(16,2)`, nunca `FLOAT`).
- **Clave foránea compuesta** hacia `Employee` (`DepartmentCode` +
  `EmployeeNumber`), porque esa es su clave primaria.
- **Eliminación lógica con `DeletedAt` / `DeletedBy`:** en lugar de un `BIT`, la
  fecha indica si está eliminado y además registra quién y cuándo. Anular usa
  `ModifiedBy` / `ModifiedAt`, así cada acción tiene su propia auditoría.
- **Índice único filtrado** `UX_Bonus_Employee_Period_Active`
  (`DepartmentCode, EmployeeNumber, Period` donde `DeletedAt IS NULL` y el
  estado es Registrado o Pagado): garantiza "un solo bono activo por periodo"
  incluso con dos peticiones simultáneas.
- **CHECK constraints** como última línea de defensa: formato del periodo,
  valores según el tipo, neto = bruto - retención, solo estados de bono y
  `DeletedAt` / `DeletedBy` siempre juntos.
- Enfoque **database-first**: las tablas se crean con los scripts SQL y EF Core
  solo las mapea (sin migraciones).

### Backend

- **Clean Architecture** respetando las capas del proyecto base, con carpetas
  por funcionalidad (`Bonuses/`) dentro de cada capa.
- **Reglas de negocio en `BonusService`** (Application); el controlador solo
  recibe la petición y delega. El cálculo vive en la entidad
  (`Bonus.CalculateAmounts()`), con redondeo a 2 decimales y el medio hacia
  arriba (`MidpointRounding.AwayFromZero`).
- **Un repositorio por módulo** (`IBonusRepository`), como `ICatalogRepository`
  en el proyecto base: incluye la lectura del empleado que necesita el registro.
- **Paginación en la base de datos:** filtros opcionales, `CountAsync` y `Skip`
  / `Take` (`OFFSET ... FETCH`), con proyección directa al DTO (`AsNoTracking` +
  `Select`) y las descripciones resueltas con JOIN.
- El sueldo y la retención se leen siempre de la base de datos al registrar; el
  front solo envía lo que el usuario elige.

### Frontend

- **Angular Material** como librería de UI: `mat-table`, `mat-paginator`,
  `MatDialog`, campos `outline`.
- **Signals** para el estado de la pantalla y **Reactive Forms** para filtros y
  registro, con validaciones de obligatorios, mínimo y máximo según el tipo.
- **Vista previa** de bruto, retención y neto con `computed()`, usando la misma
  fórmula y redondeo que el backend (el valor guardado siempre lo calcula el
  backend).
- **Diálogo de confirmación reutilizable** (`shared/confirm-dialog`) para anular
  y eliminar; las acciones solo se habilitan en estado Registrado.
- Los errores 409 los muestra el interceptor del proyecto base; los componentes
  no repiten ese manejo.

## Pendientes y posibles mejoras

- **Rendimiento del listado con muchos registros:** hoy los filtros del listado
  no usan un índice (el índice único solo cubre bonos activos). Se podría
  agregar `(Period, BonusStateCode) WHERE DeletedAt IS NULL` y evaluar
  paginación por cursor en lugar de `OFFSET`.
- **Secretos:** la cadena de conexión con la contraseña está en
  `appsettings.json`; lo correcto es moverla a User Secrets o variables de
  entorno.
- Si dos registros del mismo bono llegan al mismo tiempo, el índice único evita
  el duplicado, pero la API responde 500 en lugar de 409.
- Los textos del paginador están en inglés (se traducen con `MatPaginatorIntl`).
- Puntos extra no implementados: pago del bono con reparto por proyecto, pruebas
  unitarias del cálculo y levantar todo con `podman compose`.
