# TODO - Módulo de Bonos de desempeño

.NET 10 + Angular 22 + SQL Server.

Marca `[x]` cada paso al terminarlo. Ve en orden: cada fase se apoya en la
anterior. Los pasos marcados **(Usuario)** los ejecuta el usuario.

## Fase 1 - Base de datos (`database/02_bonus.sql`)

- [x] **1.1** Crear `database/02_bonus.sql` (mismo estilo que el `01`: PK y FK
      simples en línea).
- [x] **1.2** Tabla `dbo.Bonus`:

  | Columna        | Tipo          | Nulos    | Notas                              |
  | -------------- | ------------- | -------- | ---------------------------------- |
  | BonusId        | INT IDENTITY  | NOT NULL | PK                                 |
  | DepartmentCode | CHAR(5)       | NOT NULL | FK compuesta -> Employee           |
  | EmployeeNumber | INT           | NOT NULL | FK compuesta -> Employee           |
  | Period         | CHAR(7)       | NOT NULL | YYYY-MM + CHECK de formato         |
  | BonusTypeId    | INT           | NOT NULL | FK -> BonusType                    |
  | BonusValue     | DECIMAL(16,2) | NOT NULL | Monto fijo o porcentaje, CHECK > 0 |
  | ReasonCode     | CHAR(6)       | NOT NULL | FK -> BonusReason                  |
  | MonthlySalary  | DECIMAL(16,2) | NOT NULL | Foto del sueldo al registrar       |
  | TaxRate        | DECIMAL(5,2)  | NOT NULL | Foto de la retención al registrar  |
  | GrossAmount    | DECIMAL(16,2) | NOT NULL | Bruto                              |
  | TaxAmount      | DECIMAL(16,2) | NOT NULL | Retención                          |
  | NetAmount      | DECIMAL(16,2) | NOT NULL | Neto                               |
  | BonusStateCode | CHAR(4)       | NOT NULL | DEFAULT '0201', FK -> State        |
  | CreatedBy      | VARCHAR(50)   | NOT NULL |                                    |
  | CreatedAt      | DATETIME2(0)  | NOT NULL | DEFAULT GETDATE()                  |
  | ModifiedBy     | VARCHAR(50)   | NULL     |                                    |
  | ModifiedAt     | DATETIME2(0)  | NULL     |                                    |
  | DeletedBy      | VARCHAR(50)   | NULL     | Eliminación lógica (NULL = vigente) |
  | DeletedAt      | DATETIME2(0)  | NULL     | CHECK: ambos NULL o ambos con valor |

- [x] **1.3** Índice único filtrado `UX_Bonus_Employee_Period_Active`:
  - Columnas `(DepartmentCode, EmployeeNumber, Period)`
  - `WHERE DeletedAt IS NULL AND BonusStateCode IN ('0201','0202')`
  - Garantiza en BD "un solo bono activo por periodo" y sirve para filtrar por
    empleado.
- [x] **1.4** **(Usuario)** Ejecutar el script en el contenedor:

  ```bash
  podman cp database/02_bonus.sql bonustest-sql:/tmp/02_bonus.sql
  podman exec bonustest-sql /opt/mssql-tools18/bin/sqlcmd -C -S localhost \
    -U sa -P Password2026 -b -i /tmp/02_bonus.sql
  ```

## Fase 2 - Domain (`backend/src/BonusTest.Domain`)

- [x] **2.1** `Entities/Bonus.cs`: propiedades de la tabla y navegaciones
      `Employee`, `BonusType`, `Reason`, `State`.
- [x] **2.2** `Entities/Employee.cs`: agregar la navegación `Department` (para
      traer el nombre del área en el listado).
- [x] **2.3** Método `CalculateAmounts()` en `Bonus` (sin carpeta `Services`):
  - Bruto = monto fijo, o `MonthlySalary * % / 100`
  - Retención = `Bruto * TaxRate / 100`
  - Neto = `Bruto - Retención`
  - Redondeo `Math.Round(x, 2, MidpointRounding.AwayFromZero)` (0.125 -> 0.13)

## Fase 3 - Application (`backend/src/BonusTest.Application/Bonuses`)

- [x] **3.1** `BonusDtos.cs`:
  - `EmployeeDto`: área, número, nombre, fecha de ingreso, sueldo, % retención
  - `CreateBonusRequest`: DepartmentCode, EmployeeNumber, Period, BonusTypeId,
    BonusValue, ReasonCode
  - `BonusListItemDto`: id, estado + descripción, periodo, área + nombre del
    área, número, nombre, tipo + descripción, valor, motivo, bruto, retención,
    neto
  - `BonusFilter.cs` (archivo propio) `: PaginationRequest`: DepartmentCode?,
    EmployeeNumber?, Period?, StateCode?
- [x] **3.2** `IBonusRepository.cs`: `GetEmployeeAsync`, `IsReasonActiveAsync`,
      `BonusTypeExistsAsync`, `HasActiveBonusAsync(dept, emp, period)`, `Add`,
      `GetByIdAsync`, `GetPagedAsync(filter)`, `SaveChangesAsync`.
- [x] **3.3** `BonusService.cs`: aquí van TODAS las reglas; el controlador no
      tiene lógica.
  - `GetEmployeeAsync`:
    - No existe -> `EMPLOYEE_NOT_FOUND`
    - Cesado (0102) -> `EMPLOYEE_TERMINATED`
  - `CreateAsync(request, userCode)`:
    1. Empleado activo (reusa lo anterior)
    2. Periodo con formato YYYY-MM válido -> `PERIOD_INVALID`; posterior al mes
       actual -> `PERIOD_IN_FUTURE`
    3. Tipo existe -> `BONUS_TYPE_INVALID`; motivo activo -> `REASON_INACTIVE`
    4. Monto fijo > 0 y <= sueldo -> `AMOUNT_INVALID`; porcentaje > 0 y <= 100
       -> `PERCENTAGE_INVALID`
    5. Sin otro bono activo en el periodo -> `BONUS_DUPLICATED`
    6. `bonus.CalculateAmounts()` y guardar en estado 0201 con `CreatedBy`/`CreatedAt`
  - `CancelAsync` / `DeleteAsync(bonusId, userCode)`:
    - No existe o está eliminado -> `BONUS_NOT_FOUND`
    - Estado distinto de 0201 -> `BONUS_NOT_REGISTERED`
    - Anular: `BonusStateCode = 0203` + `ModifiedBy` / `ModifiedAt`
    - Eliminar: `DeletedBy` / `DeletedAt` (eliminación lógica)
  - `GetPagedAsync(filter)` -> `PagedResponse<BonusListItemDto>`
  - Fecha actual con `DateTime.Now`
- [x] **3.4** `DependencyInjection.cs`: `services.AddScoped<BonusService>();`

## Fase 4 - Infrastructure (`backend/src/BonusTest.Infrastructure`)

- [x] **4.1** `Persistence/Configurations/BonusConfiguration.cs`:
      `ToTable("Bonus")`, PK, precisiones decimales, FK compuesta a `Employee`,
      FKs a `BonusType` / `BonusReason` / `State`.
- [x] **4.2** `EmployeeConfiguration`: relación `Employee -> Department`.
- [x] **4.3** `AppDbContext`: `DbSet<Bonus> Bonuses` (reemplaza el comentario
      "agrega aquí...").
- [x] **4.4** `Repositories/BonusRepository.cs`:
  - `GetPagedAsync`: `Where(x => x.DeletedAt == null)` + filtros opcionales -> `CountAsync` ->
    `OrderByDescending` -> `Skip((PageIndex-1)*PageSize).Take(PageSize)` ->
    `Select` a `BonusListItemDto` (paginación en BD)
  - Lecturas con `AsNoTracking`; `GetByIdAsync` con tracking para poder
    actualizar
  - `SaveChangesAsync`: si SQL lanza violación del índice único (2601/2627) ->
    `BusinessException BONUS_DUPLICATED` (cubre dos registros simultáneos)
- [x] **4.5** `DependencyInjection.cs`:
      `services.AddScoped<IBonusRepository, BonusRepository>();`

## Fase 5 - Api (`backend/src/BonusTest.Api`)

- [x] **5.1** `Controllers/BonusController.cs` (`Route api/v1/[controller]`),
      delgado, solo delega:

  | Método | Ruta                                                       | Entrada                         | Respuesta |
  | ------ | ---------------------------------------------------------- | ------------------------------- | --------- |
  | GET    | `/api/v1/Bonus`                                            | `[FromQuery] BonusFilter`       | 200       |
  | GET    | `/api/v1/Bonus/employee/{departmentCode}/{employeeNumber}` | ruta                            | 200       |
  | POST   | `/api/v1/Bonus`                                            | `[FromBody] CreateBonusRequest` | 201       |
  | PUT    | `/api/v1/Bonus/{bonusId}/cancel`                           | ruta                            | 204       |
  | DELETE | `/api/v1/Bonus/{bonusId}`                                  | ruta                            | 204       |
  - Usuario: `[FromHeader(Name = "UserCode")]`, obligatorio en POST/PUT/DELETE.

- [x] **5.2** **(Usuario)** Compilar: `dotnet build backend/BonusTest.slnx`
- [x] **5.3** **(Usuario)** Levantar la API y probar en Swagger:
  - TECH1-101, 10 % -> bruto 500.00, retención 40.00, neto 460.00
  - VENTA-202 (Cesado) -> 409 `EMPLOYEE_TERMINATED`
  - Periodo futuro, monto > sueldo, % > 100, motivo 000004, segundo bono en el
    mismo periodo -> 409
  - Anular / eliminar un bono ya anulado -> 409; un bono eliminado no aparece en
    el listado
  - Listado con filtros y paginación (`pageIndex` / `pageSize`)

## Fase 6 - Frontend (`frontend/src/app`)

- [ ] **6.0** Agregar Angular Material como librería de UI:
  - `ng add @angular/material` (tema, tipografía y estilos globales)
  - Usar sus componentes en la pantalla y el modal (ver 6.4 y 6.5)
- [x] **6.1** `core/models/bonus.ts`: interfaces `Employee`, `BonusListItem`,
      `CreateBonusRequest`, `BonusFilter`.
- [x] **6.2** `core/services/bonus-api.ts`: `getBonuses(filter)`,
      `getEmployee`, `create`, `cancel`, `delete` (`HttpParams` solo con filtros
      no vacíos). Sin manejo de 409: ya lo hace el interceptor.
- [x] **6.3** Vista previa con `computed` dentro del modal (misma fórmula y
      redondeo half-up que el backend; sin archivo aparte).
- [x] **6.4** Pantalla `/bonuses` (`bonuses.ts` / `.html` / `.css`):
  - Filtros con `mat-form-field` / `mat-select`: área, número de empleado,
    periodo (`input type="month"`), estado; botones Buscar y Limpiar
  - Tabla con `mat-table`: acciones, estado, periodo, área-número, nombre,
    tipo, monto o % (con "%"), motivo, bruto, retención, neto (pipe
    `number:'1.2-2'`)
  - Paginación de 10 con `mat-paginator` (muestra el total de registros); llama
    a la API en cada cambio de página
  - Acciones Anular / Eliminar habilitadas solo en 0201, con confirmación
    (`MatDialog`) antes de llamar
  - El botón "Nuevo registro" abre el modal
- [x] **6.5** `features/bonuses/bonus-form-modal` (componente standalone abierto
      con `MatDialog`):
  - Paso 1: área + número (`Validators.required`) -> Buscar -> muestra nombre,
    fecha de ingreso, sueldo, % retención
  - Paso 2: periodo (máx. mes actual), tipo, monto/porcentaje (la etiqueta
    cambia según el tipo; validadores > 0 y <= sueldo o <= 100), motivo (solo
    activos)
  - Vista previa de bruto / retención / neto mientras escribe
  - Guardar -> al éxito emite `saved`, cierra y el padre refresca el listado
- [ ] **6.6** **(Usuario)** Levantar el front (`npm start`) y probar el flujo
      completo.

## Fase 7 - Entrega

- [ ] **7.0** Revisar el rendimiento del listado paginado (`GetPagedAsync`):
  - Hoy el listado no usa índices para los filtros: el índice único filtrado
    solo cubre bonos activos y el listado también muestra anulados
  - Opción: índice `(Period, BonusStateCode) WHERE DeletedAt IS NULL`
  - Opción a futuro: paginación por cursor (keyset) en vez de `OFFSET`
  - Decidir si se implementa o solo se menciona en el README

- [ ] **7.1** Sacar la contraseña de `appsettings.json` con User Secrets:
  - `dotnet user-secrets init` en `backend/src/BonusTest.Api` (agrega
    `UserSecretsId` al `.csproj`)
  - `dotnet user-secrets set "ConnectionStrings:BonusTest" "<cadena con sa>"`
  - Restaurar en `appsettings.json` la cadena original (`Trusted_Connection`)
- [ ] **7.2** `README.md`: cómo levantar (BD 01 + 02, User Secrets, API, front),
      decisiones técnicas (foto de sueldo/retención, índice único filtrado,
      cálculo en Domain, paginación en BD) y pendientes.

## Extra (opcional, solo si todo lo anterior funciona)

- [ ] **E.1** Pruebas unitarias xUnit de `Bonus.CalculateAmounts` (ejemplo del PDF,
      redondeo 0.125 -> 0.13, retención 0 %).
- [ ] **E.2** Pagar bono:
  - Tabla `BonusProjectDistribution` en `03_*.sql`
  - `POST /api/v1/Bonus/{id}/pay`
  - Reparto proporcional a las horas (el último proyecto cuadra la diferencia)
  - Estado 0202, todo en una transacción
  - Botón "Pagar" en la grilla
- [ ] **E.3** Levantar todo con `podman compose up` (también sirve con
      `docker compose`):
  - `compose.yaml` en la raíz con los servicios `sqlserver`, `db-init`, `api` y
    `frontend`
  - `db-init`: espera a SQL Server y ejecuta `01_schema_seed.sql` y
    `02_bonus.sql` con `sqlcmd` (la imagen de SQL Server no corre scripts de
    inicio sola)
  - `backend/Dockerfile`: imagen `sdk` para compilar y `aspnet` para correr;
    cadena de conexión por variable de entorno `ConnectionStrings__BonusTest`
  - `frontend/Dockerfile`: `ng build` y servir con nginx
  - Documentar el comando en el README
