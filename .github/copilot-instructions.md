# MiDineroIA Backend

## Descripción del Proyecto

MiDineroIA es un asistente financiero inteligente que utiliza IA para ayudar a las personas a registrar, organizar y analizar sus gastos mensuales de forma automática. El usuario puede registrar gastos de dos formas: escribiendo mensajes en un chat en lenguaje natural ("gasté $12 en uber") o enviando fotos de facturas/recibos. La IA procesa esta información para clasificar el gasto, identificar el monto y comercio, y registrarlo en la base de datos.

La app tiene 2 pantallas principales:
1. **Dashboard** — Muestra resumen financiero del mes: saldo, ingresos/egresos totales, tablas de Presupuesto vs Real por categoría, y gráfico de dona de distribución de egresos
2. **Chat** — Interfaz tipo WhatsApp donde el usuario escribe gastos o sube fotos de facturas. La IA responde con tarjetas de confirmación

### Contexto Regional
La app está orientada a usuarios de **El Salvador**. La moneda es USD ($). Los usuarios escriben en español con jerga salvadoreña que la IA debe entender (ver sección de Prompts).

---

## Stack Tecnológico

| Capa | Tecnología | Hosting |
|------|-----------|---------|
| Frontend | Angular | Vercel |
| Backend | Azure Functions (.NET / C#) | Azure |
| ORM | Entity Framework Core | — |
| Base de Datos | SQL Server | Azure SQL |
| IA - Texto | Claude API (Haiku 4.5) | Anthropic |
| IA - OCR | Azure Computer Vision (S1) | Azure |
| Storage | Azure Blob Storage (Hot tier, LRS) | Azure |
| Mapper | Mapperly | — |
| Auth | JWT + BCrypt | — |

---

## Arquitectura

El proyecto sigue una arquitectura por capas con separación clara de responsabilidades:

```
MiDineroIA/
├── Functions/                    # Azure Function entry points (HTTP-triggered)
│   ├── ChatFn.cs                # POST /api/chat — flujo principal de IA
│   ├── TransactionFn.cs         # PUT confirm + edit, DELETE transactions
│   ├── DashboardFn.cs           # GET /api/dashboard
│   ├── BudgetFn.cs              # PUT /api/budgets — inline edit
│   ├── CategoryFn.cs            # GET + POST categorías
│   └── AuthFn.cs                # POST register + login
├── Application/                  # Lógica de negocio
│   ├── Services/                # ChatService, DashboardService, BudgetService, etc.
│   ├── DTOs/                    # Request/Response objects
│   └── Mapping/                 # Mapperly mapper configurations
├── Domain/                       # Entidades e interfaces
│   ├── Entities/                # User, Transaction, Category, etc.
│   ├── Interfaces/              # IClaudeService, IOcrService, IBlobService, repositories
│   └── Constants/               # TransactionTypes, MessageTypes, IntentTypes
├── Infrastructure/              # Servicios externos y persistencia
│   ├── Database/                # AppDbContext
│   ├── Configurations/          # EF Core fluent configurations
│   ├── Repositories/            # Data access implementations
│   └── ExternalServices/        # ClaudeService, OcrService, BlobService
├── CrossCutting/                # Concerns transversales
│   ├── Prompts/                 # SystemPrompt.cs, OcrPrompt.cs
│   ├── Auth/                    # JwtHelper.cs
│   └── Helpers/                 # Utilidades generales
└── Program.cs                   # DI, EF, Services registration
```

---

## Base de Datos — YA CREADA MANUALMENTE

**IMPORTANTE: La base de datos ya fue creada ejecutando un script SQL directamente en Azure SQL. NO usar EF Migrations. Las tablas, vistas, índices, constraints y datos semilla ya existen.**

Entity Framework se usa en modo "mapeo a DB existente": las entidades mapean a las tablas existentes, las Configurations definen las relaciones y propiedades, pero NUNCA se ejecuta `dotnet ef migrations add` ni `dotnet ef database update`.

### Tablas (7 total)

#### 1. Users
```
- Id              INT IDENTITY PK
- Name            NVARCHAR(100) NOT NULL
- Email           NVARCHAR(255) NOT NULL UNIQUE
- PasswordHash    NVARCHAR(500) NOT NULL
- MonthlyIncome   DECIMAL(12,2) NULL
- Currency        NVARCHAR(10) DEFAULT 'USD'
- IsActive        BIT DEFAULT 1
- CreatedAt       DATETIME2 DEFAULT GETUTCDATE()
- UpdatedAt       DATETIME2 DEFAULT GETUTCDATE()
```

#### 2. CategoryGroups
```
- Id                INT IDENTITY PK
- Name              NVARCHAR(50) NOT NULL        -- 'Ingresos', 'Servicios', 'Gastos'
- TransactionType   NVARCHAR(10) NOT NULL        -- 'INGRESO' o 'EGRESO'
- DisplayOrder      INT DEFAULT 0
- Icon              NVARCHAR(50) NULL
CHECK: TransactionType IN ('INGRESO', 'EGRESO')
```

#### 3. Categories
```
- Id                INT IDENTITY PK
- CategoryGroupId   INT NOT NULL FK → CategoryGroups
- UserId            INT NULL FK → Users           -- NULL = categoría del sistema
- Name              NVARCHAR(100) NOT NULL
- IsDefault         BIT DEFAULT 1
- IsActive          BIT DEFAULT 1
- DisplayOrder      INT DEFAULT 0
```

#### 4. MonthlyBudgets
```
- Id            INT IDENTITY PK
- UserId        INT NOT NULL FK → Users
- CategoryId    INT NOT NULL FK → Categories
- Year          INT NOT NULL
- Month         INT NOT NULL                     -- 1 a 12
- Amount        DECIMAL(12,2) NOT NULL DEFAULT 0
- CreatedAt     DATETIME2 DEFAULT GETUTCDATE()
- UpdatedAt     DATETIME2 DEFAULT GETUTCDATE()
UNIQUE: (UserId, CategoryId, Year, Month)
CHECK: Month BETWEEN 1 AND 12
```

#### 5. ChatMessages
```
- Id            INT IDENTITY PK
- UserId        INT NOT NULL FK → Users
- MessageType   NVARCHAR(20) NOT NULL            -- 'USER_TEXT', 'USER_IMAGE', 'AI_RESPONSE'
- Content       NVARCHAR(MAX) NULL
- ImageUrl      NVARCHAR(500) NULL
- AiProcessed   BIT DEFAULT 0
- CreatedAt     DATETIME2 DEFAULT GETUTCDATE()
CHECK: MessageType IN ('USER_TEXT', 'USER_IMAGE', 'AI_RESPONSE')
```

#### 6. Transactions
```
- Id                INT IDENTITY PK
- UserId            INT NOT NULL FK → Users
- CategoryId        INT NOT NULL FK → Categories
- ChatMessageId     INT NULL FK → ChatMessages
- Amount            DECIMAL(12,2) NOT NULL
- Description       NVARCHAR(500) NULL
- Merchant          NVARCHAR(200) NULL
- TransactionDate   DATE NOT NULL
- Source            NVARCHAR(20) DEFAULT 'TEXT'   -- 'TEXT', 'IMAGE', 'MANUAL'
- IsConfirmed       BIT DEFAULT 1
- CreatedAt         DATETIME2 DEFAULT GETUTCDATE()
- UpdatedAt         DATETIME2 DEFAULT GETUTCDATE()
CHECK: Source IN ('TEXT', 'IMAGE', 'MANUAL')
```

#### 7. Receipts
```
- Id                INT IDENTITY PK
- TransactionId     INT NOT NULL FK → Transactions
- ImageUrl          NVARCHAR(500) NOT NULL
- RawOcrText        NVARCHAR(MAX) NULL
- AiExtractedJson   NVARCHAR(MAX) NULL
- ConfidenceScore   DECIMAL(5,2) NULL
- CreatedAt         DATETIME2 DEFAULT GETUTCDATE()
```

### Vistas SQL (3 — ya creadas en la DB)

- **vw_MonthlySummary**: Tarjetas de resumen (Saldo, Ingresos, Egresos). JOIN Transactions → Categories → CategoryGroups, GROUP BY user_id, year, month.
- **vw_CategoryDetail**: Tablas Ppto vs Real. Mismo JOIN + LEFT JOIN MonthlyBudgets.
- **vw_ExpenseDistribution**: Gráfico de dona. Solo EGRESO, calcula porcentaje por grupo.

### Datos Semilla (ya insertados en la DB)

**3 CategoryGroups**: Ingresos (INGRESO), Servicios (EGRESO), Gastos (EGRESO)

**Categorías de Ingresos**: Sueldo, Negocio, Otros

**Categorías de Servicios**: Renta, Seguro del carro, Recibo de Luz, Seguro de salud, Recibo de Agua, Gimnasio, Recibo de Gas, Internet

**Categorías de Gastos**: Despensa, Compras, Cine, Salidas, Comidas, Entretenimiento, Regalos, Cerveza

---

## Endpoints API (11 total)

Todos los endpoints excepto auth requieren JWT en header: `Authorization: Bearer {token}`

### ChatFn.cs

**POST /api/chat** — Flujo principal
```
Request:  { "message": "string", "image_base64": "string|null" }
Response: {
  "intent": "REGISTER_TRANSACTION|SET_BUDGET|GENERAL_QUERY",
  "transaction_id": int|null,
  "message": "string",
  "data": { ... },
  "needs_confirmation": bool,
  "budget_info": { "budget": decimal, "spent": decimal, "remaining": decimal } | null,
  "suggested_alternatives": [{ "category_name": string, "category_id": int }] | null
}
```

**GET /api/chat/history?page=1&pageSize=20** — Historial de mensajes paginado

### TransactionFn.cs

**PUT /api/transactions/{id}/confirm** — Confirmar transacción (is_confirmed = true)

**PUT /api/transactions/{id}** — Editar transacción
```
Request: { "category_id": int, "amount": decimal, "description": "string" }
```

**DELETE /api/transactions/{id}** — Eliminar transacción

### DashboardFn.cs

**GET /api/dashboard?year=2026&month=3** — Toda la data del dashboard
```
Response: {
  "summary": { "total_income": decimal, "total_expenses": decimal, "balance": decimal },
  "income_detail": [{ "category": string, "budget": decimal, "real": decimal }],
  "expense_groups": [{
    "group_name": string,
    "categories": [{ "category": string, "budget": decimal, "real": decimal }]
  }],
  "expense_distribution": [{ "group": string, "total": decimal, "percentage": decimal }]
}
```

### BudgetFn.cs

**PUT /api/budgets** — Crear/actualizar presupuesto (UPSERT)
```
Request: { "category_id": int, "year": int, "month": int, "amount": decimal }
```

### CategoryFn.cs

**GET /api/categories** — Lista categorías agrupadas (sistema + personalizadas del usuario)

**POST /api/categories** — Crear categoría personalizada
```
Request: { "category_group_id": int, "name": "string" }
```

### AuthFn.cs

**POST /api/auth/register** — Sin auth requerida
```
Request:  { "name": "string", "email": "string", "password": "string" }
Response: { "token": "jwt_string", "user": { "id": int, "name": string, "email": string } }
```

**POST /api/auth/login** — Sin auth requerida
```
Request:  { "email": "string", "password": "string" }
Response: { "token": "jwt_string", "user": { "id": int, "name": string, "email": string } }
```

---

## Flujos Principales

### Flujo 1: Registro de gasto por texto
1. Usuario escribe "gasté $12 en uber"
2. Angular envía POST /api/chat con { message: "gasté $12 en uber" }
3. ChatService guarda mensaje en ChatMessages (USER_TEXT)
4. ChatService obtiene categorías del usuario (Categories + CategoryGroups)
5. ClaudeService construye system prompt con fecha actual + categorías JSON
6. ClaudeService envía a Claude API Haiku 4.5 → recibe JSON con intent
7. Claude responde: { intent: "REGISTER_TRANSACTION", data: { amount: 12, category_name: "Servicios", ... } }
8. ChatService crea Transaction (is_confirmed: false) + consulta presupuesto restante si existe
9. ChatService guarda AI_RESPONSE en ChatMessages
10. Respuesta al frontend con tarjeta de confirmación

### Flujo 2: Registro de gasto por imagen
1. Usuario sube foto de factura
2. Angular valida (max 5MB, JPG/PNG/WEBP), convierte a base64
3. POST /api/chat con { message: "Registra esta factura", image_base64: "..." }
4. ChatService guarda en ChatMessages (USER_IMAGE)
5. BlobService sube imagen a Azure Blob → devuelve URL con SAS token
6. OcrService envía URL a Azure Vision OCR → devuelve texto crudo
7. ClaudeService procesa texto OCR con prompt específico de facturas
8. Claude estructura los datos → ChatService crea Transaction + Receipt
9. Respuesta al frontend con datos extraídos para confirmar

### Flujo 3: Dashboard
1. Usuario abre Pantalla 1 o cambia selector de mes
2. Angular hace GET /api/dashboard?year=2026&month=3
3. DashboardService ejecuta 3 consultas en paralelo usando las vistas SQL
4. Devuelve JSON con summary, income_detail, expense_groups, expense_distribution
5. Angular renderiza tarjetas, tablas y gráfico de dona

### Flujo 4: Definición de presupuesto por chat
1. Usuario escribe "mi presupuesto de despensa es $1200"
2. Claude detecta intent: SET_BUDGET
3. ChatService hace UPSERT en MonthlyBudgets
4. Respuesta con tarjeta de confirmación de presupuestos

### Flujo 5: Definición de presupuesto por inline edit
1. Usuario toca celda de Ppto en el dashboard
2. Angular envía PUT /api/budgets con { category_id, year, month, amount }
3. BudgetService hace UPSERT en MonthlyBudgets

---

## Sistema de Prompts para Claude API

### System Prompt (se envía en cada request de texto)

El system prompt contiene:
- Instrucciones generales (responder en español, solo JSON, fecha actual, moneda USD)
- **Contexto regional de El Salvador** con jerga local:
  - Dinero: "pisto" = dinero, "rojos" = dólares, "cora" = $0.25, "luca" = $1,000, "feria" = dinero, "chamba" = trabajo
  - Expresiones: "bolado" = cosa, "chivo"/"tuani" = genial, "vergo de" = mucho, "birria" = cerveza, "pupusas" = comida típica, "boquitas" = snacks
  - Comercios locales mapeados a categorías:
    - DESPENSA: Super Selectos, Despensa de Don Juan, Despensa Familiar, PriceSmart, Walmart
    - COMIDAS: Pollo Campero, Biggest, Pizza Hut, Wendy's, McDonald's, Burger King, Subway
    - SERVICIOS (gasolina): Texaco, Puma, Shell, Uno, Esso
    - INTERNET: Claro, Tigo, Movistar, Digicel
    - RECIBO DE LUZ: AES, CAESS, CLESA, EEO, DEUSEM, Del Sur
    - RECIBO DE AGUA: ANDA
    - ENTRETENIMIENTO: Cinemark, Cinépolis
  - Palabras clave INGRESO: "me pagaron", "recibí", "me depositaron", "cobré", "gané", "sueldo", "quincena", "me cayó", "chamba"
  - Palabras clave EGRESO: "gasté", "pagué", "compré", "me cobró", "eché", "tanqueé"
- Definición de 3 intenciones: REGISTER_TRANSACTION, SET_BUDGET, GENERAL_QUERY
- Reglas para cada intención
- Placeholder dinámico {categories_json} con categorías del usuario
- Placeholder dinámico {current_date} con fecha actual
- Tono: amigable y profesional, NO usar jerga ni voseo en las respuestas

### OCR Prompt (se usa después de Azure Vision OCR)

Incluye todo lo anterior más:
- Contexto fiscal salvadoreño: CCF, NIT, NRC, IVA = 13%, formatos de fecha DD/MM/YYYY
- Moneda USD (El Salvador usa dólar estadounidense)
- Instrucciones para encontrar el Total de la factura
- Campo opcional ocr_items para listar artículos individuales

### Formato de respuesta JSON de Claude

Para REGISTER_TRANSACTION:
```json
{
  "intent": "REGISTER_TRANSACTION",
  "data": {
    "transaction_type": "EGRESO",
    "amount": 12.00,
    "category_name": "Servicios",
    "category_id": 8,
    "description": "Viaje en Uber",
    "merchant": "Uber",
    "transaction_date": "2026-03-21",
    "confidence_score": 95
  },
  "message": "Registré tu gasto: $12.00 en Uber (Servicios). ¿Es correcto?",
  "needs_confirmation": true
}
```

Para SET_BUDGET:
```json
{
  "intent": "SET_BUDGET",
  "data": {
    "budgets": [
      { "category_name": "Despensa", "category_id": 13, "amount": 1200.00, "year": 2026, "month": 3 }
    ]
  },
  "message": "Configuré presupuesto Despensa: $1,200.00. ¿Confirmas?",
  "needs_confirmation": true
}
```

Para GENERAL_QUERY:
```json
{
  "intent": "GENERAL_QUERY",
  "data": { "query_type": "GREETING" },
  "message": "¡Hola! Soy tu asistente financiero..."
}
```

Query types disponibles: GREETING, MONTHLY_SUMMARY, BUDGET_STATUS, TOP_EXPENSES, CATEGORY_DETAIL

### Modelo recomendado
Usar **Claude Haiku 4.5** (modelo: claude-haiku-4-5-20251001). max_tokens: 1024.

---

## Mapping Pattern (Mapperly)

Este proyecto usa **Mapperly** para el mapeo entre entidades y DTOs. Mapperly genera código en tiempo de compilación usando source generators.

### Configuración
```
dotnet add package Riok.Mapperly
```

### Crear un Mapper en Application/Mapping/

```csharp
using Riok.Mapperly.Abstractions;

[Mapper]
public partial class TransactionMapper
{
    public partial TransactionDto ToDto(Transaction entity);
    public partial List<TransactionDto> ToDtoList(List<Transaction> entities);
    public partial Transaction ToEntity(CreateTransactionDto dto);
}
```

### Mapeos con propiedades personalizadas

```csharp
[Mapper]
public partial class DashboardMapper
{
    [MapProperty(nameof(Transaction.Category.Name), nameof(TransactionDetailDto.CategoryName))]
    public partial TransactionDetailDto ToDetailDto(Transaction entity);

    [MapperIgnoreTarget(nameof(DashboardSummaryDto.Balance))]
    public partial DashboardSummaryDto ToSummaryDto(MonthlySummaryView view);
    
    public DashboardSummaryDto ToSummaryDtoWithBalance(MonthlySummaryView view)
    {
        var dto = ToSummaryDto(view);
        dto.Balance = dto.TotalIncome - dto.TotalExpenses;
        return dto;
    }
}
```

### Registrar en DI (Program.cs)
```csharp
builder.Services.AddSingleton<TransactionMapper>();
builder.Services.AddSingleton<DashboardMapper>();
```

### Usar en Services
```csharp
public class TransactionService
{
    private readonly TransactionMapper _mapper;
    public TransactionService(TransactionMapper mapper) { _mapper = mapper; }

    public TransactionDto GetById(int id)
    {
        var entity = _repository.GetById(id);
        return _mapper.ToDto(entity);
    }
}
```

---

## Autenticación

- **Registro**: Validar email único → BCrypt.HashPassword() → INSERT User → generar JWT
- **Login**: Buscar por email → BCrypt.Verify() → generar JWT
- **JWT Claims**: sub (user_id), email, exp (7 días)
- **Middleware**: Validar JWT en cada request excepto /api/auth/*. Extraer user_id del token.
- **Seguridad**: Cada Service filtra TODO por user_id. Un usuario nunca ve datos de otro.

---

## Azure Blob Storage

- **Contenedor**: "receipts"
- **Estructura de archivos**: receipts/user_{id}/{fecha}_{guid}.jpg
- **Tier**: Hot, Redundancia: LRS
- **SAS Token**: URLs temporales (30 min) para que Azure Vision pueda descargar la imagen
- **Validaciones**: Max 5MB, solo JPG/PNG/WEBP

---

## Manejo de Errores

| Escenario | Acción |
|-----------|--------|
| Claude API timeout/caída | Mensaje ya guardado. Reintentar. Responder "No pude procesar, intenta de nuevo" |
| Claude responde JSON inválido | Reintentar 1 vez. Si falla, pedir al usuario que reformule |
| OCR no puede leer imagen | Responder "No pude leer la factura. Intenta con mejor luz o escribe el gasto" |
| Blob Storage falla | No continuar flujo. Responder "No pude procesar la imagen" |
| Imagen muy grande/formato inválido | Validar en frontend Y backend |
| JWT expirado | Devolver 401. Frontend redirige a login |
| Mensaje ambiguo | Claude responde GENERAL_QUERY pidiendo más información |

---

## Variables de Entorno (local.settings.json)

```json
{
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "SQL_CONNECTION_STRING": "Server=...;Database=MiDineroIA;...",
    "CLAUDE_API_KEY": "sk-ant-api03-...",
    "CLAUDE_MODEL": "claude-haiku-4-5-20251001",
    "OCR_KEY": "...",
    "OCR_ENDPOINT": "https://....cognitiveservices.azure.com/",
    "BLOB_CONNECTION_STRING": "DefaultEndpointsProtocol=https;AccountName=...",
    "BLOB_CONTAINER_NAME": "receipts",
    "JWT_SECRET": "...",
    "JWT_ISSUER": "MiDineroIA",
    "JWT_EXPIRATION_DAYS": "7"
  }
}
```
