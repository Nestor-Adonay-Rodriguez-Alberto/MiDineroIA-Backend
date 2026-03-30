# 🏗️ Patrones Arquitectónicos - MiDineroIA Backend

**Documento vivo** que documenta los patrones aplicados en el refactor de autenticación para replicarlos en otros servicios.

---

## 📌 Resumen de Patrones Aplicados

El proyecto ha sido refactorizado siguiendo **Clean Architecture + SOLID Principles**. Los patrones se dividen en capas:

| Capa | Patrón | Beneficio |
|------|--------|----------|
| **Domain** | Value Objects | Validación en límite del dominio |
| **Domain** | Rich Entities | Lógica de negocio encapsulada |
| **Application** | Use Cases (Interactors) | Una responsabilidad, una clase |
| **Application** | Presenters | Transformación HTTP consistente |
| **Infrastructure** | Interfaces de seguridad | Inversión de dependencias |
| **Functions** | Humble Objects | Funciones "tontas" que delegan |

---

## 🎯 Patrón 1: Value Objects

### Qué es
Objeto de dominio que encapsula un concepto primitivo con validación y comportamiento.

### Cuándo usarlo
Para cualquier concepto del dominio que tenga reglas de validación o comportamiento especial.

### Ejemplos en el proyecto

```csharp
// ❌ ANTES: Tipos primitivos sin validación
public class User
{
    public string Email { get; set; }  // Puede ser inválido
    public string PasswordHash { get; set; }  // Expone el hash
    public decimal MonthlyIncome { get; set; }  // Sin moneda
}

// ✅ DESPUÉS: Value Objects con validación
public class User
{
    public Email Email { get; set; }  // Validado en Create()
    public PasswordHash PasswordHash { get; set; }  // No expone el hash
    public Money MonthlyIncome { get; set; }  // Incluye moneda
}
```

### Estructura de un Value Object

```csharp
public sealed class Email : IEquatable<Email>
{
    public string Value { get; }

    // Constructor privado - no se crea directamente
    private Email(string value) => Value = value;

    // Factory method con validación
    public static Email Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Email inválido");
        if (!IsValidFormat(value))
            throw new ArgumentException("Email inválido");
        return new Email(value);
    }

    // Inmutable - no hay setters
    // Comparable - implementa IEquatable
    public override bool Equals(object? obj) => obj is Email e && Value == e.Value;

    // Nunca expone detalles internos
    public override string ToString() => Value;
}
```

### Value Objects en el proyecto

| VO | Propósito | Validación |
|----|-----------|-----------|
| `Email` | Representa email único | Formato regex, única occurrencia |
| `PasswordHash` | Encapsula BCrypt | Min 8 chars, verificación |
| `UserName` | Nombre del usuario | 2-100 caracteres |
| `UserId` | ID de usuario tipado | > 0 |
| `Money` | Dinero con moneda | >= 0, operaciones seguras |

### Cómo aplicar a otros servicios

**Ejemplo: Transaction (Transacción)**
```csharp
// Crear value objects para concepto de transacción
public sealed class TransactionAmount : IEquatable<TransactionAmount>
{
    public decimal Value { get; }
    public static TransactionAmount Create(decimal amount)
    {
        if (amount <= 0) throw new ArgumentException("Monto debe ser positivo");
        return new(amount);
    }
}

public sealed class TransactionDescription : IEquatable<TransactionDescription>
{
    public string Value { get; }
    public static TransactionDescription Create(string description)
    {
        if (description.Length > 500) throw new ArgumentException("Descripción muy larga");
        return new(description);
    }
}

// En la entidad Transaction
public class Transaction
{
    public TransactionAmount Amount { get; private set; }
    public TransactionDescription Description { get; private set; }

    public static Transaction CreateExpense(
        UserId userId,
        CategoryId categoryId,
        TransactionAmount amount,
        TransactionDescription description,
        TransactionDate date)
    {
        // Validación de dominio
        return new Transaction
        {
            UserId = userId,
            CategoryId = categoryId,
            Amount = amount,
            Description = description,
            Date = date,
            CreatedAt = DateTime.UtcNow
        };
    }
}
```

---

## 🎯 Patrón 2: Rich Entities (Entidades Ricas)

### Qué es
Una entidad que contiene lógica de negocio además de estado.

### Cuándo usarlo
Para cualquier entidad que represente un concepto del dominio con comportamiento.

### Ejemplo: User

```csharp
public class User
{
    // Estado
    public UserId Id { get; private set; }
    public Email Email { get; private set; }
    public PasswordHash PasswordHash { get; private set; }
    public bool IsActive { get; private set; }

    // ✅ Comportamiento de negocio
    public static User CreateNewUser(UserName name, Email email, PasswordHash hash)
    {
        return new User
        {
            Email = email,
            PasswordHash = hash,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public bool VerifyPassword(PasswordHash password) => PasswordHash.Equals(password);

    public void UpdatePassword(PasswordHash newPassword)
    {
        PasswordHash = newPassword;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
```

### Cómo aplicar a otros servicios

**Ejemplo: Transaction**
```csharp
public class Transaction
{
    public TransactionId Id { get; private set; }
    public UserId UserId { get; private set; }
    public CategoryId CategoryId { get; private set; }
    public TransactionAmount Amount { get; private set; }
    public bool IsConfirmed { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Factory para crear transacción pendiente de confirmación
    public static Transaction CreateFromChat(
        UserId userId,
        CategoryId categoryId,
        TransactionAmount amount,
        ChatMessageId messageId)
    {
        return new Transaction
        {
            UserId = userId,
            CategoryId = categoryId,
            Amount = amount,
            ChatMessageId = messageId,
            IsConfirmed = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    // Comportamiento: confirmar transacción
    public void Confirm()
    {
        if (IsConfirmed)
            throw new InvalidOperationException("Ya está confirmada");
        IsConfirmed = true;
    }

    // Comportamiento: cambiar categoría
    public void ChangeCategory(CategoryId newCategoryId)
    {
        if (IsConfirmed)
            throw new InvalidOperationException("No se puede cambiar categoría de confirmada");
        CategoryId = newCategoryId;
    }
}
```

---

## 🎯 Patrón 3: Use Cases (Interactors)

### Qué es
Una clase que orquesta un caso de uso completo del usuario. Una responsabilidad = Una clase.

### Cuándo usarlo
Para cada operación que el usuario ejecuta en la UI.

### Estructura

```csharp
public class RegisterUserUseCase
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenGenerator _tokenGenerator;

    // Inyectar dependencias
    public RegisterUserUseCase(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ITokenGenerator tokenGenerator)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
    }

    // UN MÉTODO PÚBLICO
    public async Task<AuthResponseDto> ExecuteAsync(RegisterRequest request)
    {
        // 1. Validar inputs (value objects lanzan excepciones)
        var name = UserName.Create(request.Name);
        var email = Email.Create(request.Email);
        var hash = PasswordHash.Create(request.Password);

        // 2. Verificar precondiciones
        var existing = await _userRepository.GetByEmailAsync(email);
        if (existing is not null)
            throw new InvalidOperationException("Email ya registrado");

        // 3. Crear entidad (dominio)
        var user = User.CreateNewUser(name, email, hash);

        // 4. Persistir
        await _userRepository.CreateAsync(user);

        // 5. Generar respuesta
        var token = _tokenGenerator.GenerateToken(user);
        return new AuthResponseDto(token, new UserDto(user.Id, user.NameString, user.EmailString));
    }
}
```

### Cómo aplicar a otros servicios

**Ejemplo: CreateTransactionUseCase**
```csharp
public class CreateTransactionUseCase
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IMonthlyBudgetRepository _budgetRepository;

    public async Task<CreateTransactionResponse> ExecuteAsync(CreateTransactionRequest request)
    {
        // 1. Validar
        var userId = new UserId(request.UserId);
        var categoryId = new CategoryId(request.CategoryId);
        var amount = TransactionAmount.Create(request.Amount);
        var description = TransactionDescription.Create(request.Description);

        // 2. Verificar categoría existe y pertenece al usuario
        var category = await _categoryRepository.GetByIdAsync(categoryId);
        if (category is null || category.UserId != userId)
            throw new InvalidOperationException("Categoría no existe o no pertenece al usuario");

        // 3. Crear transacción
        var transaction = Transaction.CreateFromChat(userId, categoryId, amount, null);

        // 4. Persistir
        await _transactionRepository.CreateAsync(transaction);

        // 5. Calcular impacto en presupuesto
        var budget = await _budgetRepository.GetAsync(
            userId, categoryId, DateTime.UtcNow.Year, DateTime.UtcNow.Month);

        var impact = budget?.Remaining - amount.Value ?? -amount.Value;

        return new CreateTransactionResponse(
            TransactionId: transaction.Id,
            Amount: amount.Value,
            BudgetImpact: impact,
            BudgetStatus: budget is not null ? "Dentro" : "Sin presupuesto"
        );
    }
}
```

### Lista de Use Cases por Servicio

#### Authentication
- ✅ `RegisterUserUseCase` — Registrar usuario
- ✅ `LoginUserUseCase` — Autenticar usuario
- [ ] `ResetPasswordUseCase` — Cambiar contraseña
- [ ] `RefreshTokenUseCase` — Renovar JWT

#### Chat
- [ ] `SendChatMessageUseCase` — Enviar mensaje texto
- [ ] `UploadReceiptUseCase` — Cargar factura
- [ ] `GetChatHistoryUseCase` — Obtener historial

#### Transactions
- [ ] `CreateTransactionUseCase` — Crear transacción desde chat
- [ ] `ConfirmTransactionUseCase` — Confirmar transacción pendiente
- [ ] `EditTransactionUseCase` — Editar transacción confirmada
- [ ] `DeleteTransactionUseCase` — Eliminar transacción

#### Dashboard
- [ ] `GetDashboardDataUseCase` — Obtener datos para dashboard
- [ ] `GetMonthlyReportUseCase` — Generar reporte mensual

#### Budget
- [ ] `SetBudgetUseCase` — Establecer presupuesto mensual
- [ ] `GetBudgetStatusUseCase` — Obtener estado de presupuesto

#### Categories
- [ ] `CreateCategoryUseCase` — Crear categoría personalizada
- [ ] `GetCategoriesUseCase` — Listar categorías

---

## 🎯 Patrón 4: Presenters

### Qué es
Transformador que convierte resultados de use cases en respuestas HTTP.

### Cuándo usarlo
En todas las Azure Functions para consistencia.

### Estructura

```csharp
public class AuthPresenter
{
    // ✅ Éxito
    public static IActionResult Success(AuthResponseDto response)
        => new OkObjectResult(response);

    // ✅ Manejo de errores centralizado
    public static IActionResult HandleException(Exception ex) => ex switch
    {
        ArgumentException => new BadRequestObjectResult(...),
        InvalidOperationException opEx => MapOperationError(opEx),
        _ => new ObjectResult(...) { StatusCode = 500 }
    };
}
```

### Beneficios
- Consistencia en formatos de respuesta
- Mapeo de excepciones a códigos HTTP
- Reutilización en múltiples functions

### Cómo aplicar a otros servicios

```csharp
// TransactionPresenter
public class TransactionPresenter
{
    public static IActionResult TransactionCreated(CreateTransactionResponse response)
        => new CreatedAtRouteObjectResult(
            "GetTransaction",
            new { id = response.TransactionId },
            response);

    public static IActionResult BudgetWarning(CreateTransactionResponse response)
        => new OkObjectResult(new
        {
            message = "Transacción creada",
            warning = $"Presupuesto se agotará en ${-response.BudgetImpact}",
            data = response
        });

    public static IActionResult HandleException(Exception ex) => ex switch
    {
        ArgumentException argEx => new BadRequestObjectResult(new { error = argEx.Message }),
        InvalidOperationException opEx => new UnprocessableEntityObjectResult(...),
        _ => new ObjectResult(...) { StatusCode = 500 }
    };
}
```

---

## 🎯 Patrón 5: Interfaces de Seguridad (Dependency Inversion)

### Qué es
Abstracciones para servicios externos que pueden cambiar.

### Cuándo usarlo
Para cualquier librería o servicio externo (BCrypt, JWT, Azure, etc.)

### Estructura

```csharp
// ✅ DOMINIO: Solo interfaz
public interface IPasswordHasher
{
    string Hash(string rawPassword);
    bool Verify(string rawPassword, string hash);
}

// ✅ INFRAESTRUCTURA: Implementación concreta
public class BcryptPasswordHasher : IPasswordHasher
{
    public string Hash(string rawPassword) => BCrypt.Net.BCrypt.HashPassword(rawPassword);
    public bool Verify(string rawPassword, string hash) => BCrypt.Net.BCrypt.Verify(rawPassword, hash);
}

// ✅ INYECCIÓN: En Program.cs
builder.Services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();

// ✅ USO: En use cases
public class RegisterUserUseCase
{
    public RegisterUserUseCase(IPasswordHasher passwordHasher)
    {
        _passwordHasher = passwordHasher; // Abstracción, no concreción
    }
}
```

### Ventajas
- Fácil cambiar BCrypt por Argon2
- Fácil testear con mocks
- Inversión de dependencias (SOLID-DIP)

### Interfaces para otros servicios

```csharp
// OCR
public interface IOcrService
{
    Task<OcrResult> ExtractTextAsync(Stream imageStream);
}

// Implementación: Azure Vision
public class AzureVisionOcrService : IOcrService { }

// IA
public interface IAiService
{
    Task<ChatResponse> ProcessMessageAsync(string message, string context);
}

// Implementación: Claude API
public class ClaudeAiService : IAiService { }

// Blob Storage
public interface IBlobStorage
{
    Task<string> UploadAsync(string containerName, Stream stream, string fileName);
    Task DeleteAsync(string containerName, string fileName);
}

// Implementación: Azure Blob
public class AzureBlobStorage : IBlobStorage { }
```

---

## 🎯 Patrón 6: Humble Objects (Functions)

### Qué es
Funciones que son lo más simples posible: parsean request y delegan.

### Cuándo usarlo
En todas las Azure Functions.

### Estructura ANTES (❌ Mala)

```csharp
[Function("CreateTransaction")]
public async Task<IActionResult> CreateTransaction(HttpRequest req)
{
    // ❌ Lógica de negocio
    var body = await req.ReadFromJsonAsync<CreateTransactionDto>();
    var category = await _categoryRepository.GetByIdAsync(body.CategoryId);
    if (category is null) return BadRequest("Categoría no existe");

    // ❌ Validación
    if (body.Amount <= 0) return BadRequest("Monto inválido");

    // ❌ Transformación
    var transaction = new Transaction { Amount = body.Amount, ... };

    // ❌ Manejo de errores mixto
    try {
        var result = await _transactionService.CreateAsync(transaction);
        return Ok(result);
    } catch (ConflictException ex) {
        return Conflict(ex.Message);
    }
}
```

### Estructura DESPUÉS (✅ Buena)

```csharp
[Function("CreateTransaction")]
public async Task<IActionResult> CreateTransaction(HttpRequest req)
{
    try
    {
        // 1. Parsear request (simple)
        var request = await req.ReadFromJsonAsync<CreateTransactionRequest>();
        if (request is null)
            return TransactionPresenter.HandleException(
                new ArgumentException("Request inválido"));

        // 2. Delegar a use case (toda la lógica está aquí)
        var result = await _createTransactionUseCase.ExecuteAsync(request);

        // 3. Presentar respuesta (consistente)
        return TransactionPresenter.TransactionCreated(result);
    }
    catch (Exception ex)
    {
        return TransactionPresenter.HandleException(ex);
    }
}
```

### Beneficios
- Fácil de testear (sin Azure Functions runtime)
- Lógica aislada en use cases
- Responsabilidades claras

---

## 📋 Checklist: Implementar Un Nuevo Servicio

Sigue estos pasos para implementar `ChatService`:

### 1. Domain Layer

- [ ] Crear value objects para conceptos clave
  - `MessageContent.cs` — Validar length
  - `ChatMessageId.cs` — ID tipado

- [ ] Crear/enriquecer entidades
  - `ChatMessage.cs` — Con factory methods

- [ ] Crear interfaces
  - `IChatRepository.cs`
  - `IAiService.cs`
  - `IBlobStorage.cs`

### 2. Application Layer

- [ ] Crear DTOs
  - `SendMessageRequest.cs`
  - `ChatMessageDto.cs`

- [ ] Crear use cases
  - `SendChatMessageUseCase.cs`
  - `GetChatHistoryUseCase.cs`

- [ ] Crear presenter
  - `ChatPresenter.cs`

- [ ] Crear tests
  - `SendChatMessageUseCaseTests.cs`

### 3. Infrastructure Layer

- [ ] Implementar interfaces
  - `ChatRepository.cs`
  - `ClaudeAiService.cs`
  - `AzureBlobStorage.cs`

- [ ] Configurar EF Core
  - `ChatMessageConfiguration.cs`

### 4. Functions Layer

- [ ] Crear Azure Function
  - `ChatFn.cs`

- [ ] Inyectar dependencias en `Program.cs`

### 5. Testing

- [ ] Escribir tests unitarios para use cases
- [ ] Escribir tests de integración para repositorio
- [ ] Escribir tests E2E para function

---

## 🎓 Referencias

### Clean Architecture
- Dependency Inversion (dep-inward-only)
- Entity Design (entity-rich-not-anemic)
- Use Case Isolation (usecase-single-responsibility)
- Humble Object Pattern (bound-humble-object)

### SOLID Principles
- **S**RP — Un use case = una clase
- **O**CP — Extensible sin modificación (factories, estrategias)
- **L**SP — Value objects intercambiables
- **I**SP — Interfaces pequeñas y focalizadas
- **D**IP — Depender de abstracciones

### TDD
- RED — Escribir test fallido
- GREEN — Implementar mínimo
- REFACTOR — Mejorar

---

## 📝 Notas Finales

- **Consistencia**: Todos los servicios siguen el mismo patrón
- **Testabilidad**: El 100% del código puede ser testeado sin dependencias externas
- **Mantenibilidad**: Cambios localizados, bajo acoplamiento
- **Claridad**: La estructura del proyecto grita el dominio (finanzas), no el framework

**Objetivo**: Código que sea fácil de mantener, extender y entender después de 6 meses.
