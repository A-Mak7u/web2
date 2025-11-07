## WebShop (Monolith + Microservices)

Интернет‑магазин: монолит (Clean Architecture) и набор микросервисов. Реализованы JWT‑аутентификация, обмен событиями через RabbitMQ (MassTransit), кэширование Redis, БД PostgreSQL, фронтенд на React (Vite + Chakra UI), полная контейнеризация Docker.

![.NET](https://img.shields.io/badge/.NET-9-512BD4?logo=dotnet&logoColor=white) ![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-Web%20API-512BD4) ![RabbitMQ](https://img.shields.io/badge/RabbitMQ-3.x-FF6600?logo=rabbitmq&logoColor=white) ![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-4169E1?logo=postgresql&logoColor=white) ![Redis](https://img.shields.io/badge/Redis-7-DC382D?logo=redis&logoColor=white) ![Docker](https://img.shields.io/badge/Docker-Compose-2496ED?logo=docker&logoColor=white) ![React](https://img.shields.io/badge/React-18-61DAFB?logo=react&logoColor=061A23)

## Содержание
- **[Архитектура и структура проекта](#архитектура-и-структура-проекта)**
- **[Быстрый старт (Docker)](#быстрый-старт-docker)**
- **[Локальная разработка](#локальная-разработка)**
- **[Сервисы и API](#сервисы-и-api)**
- **[Фронтенд (WebApp)](#фронтенд-webapp)**
- **[Скрипты](#скрипты)**
- **[Переменные окружения и конфигурация](#переменные-окружения-и-конфигурация)**
- **[Трассировка и сценарии](#трассировка-и-сценарии)**
- **[Требования задания и реализация](#требования-задания-и-реализация)**
- **[Состояние проекта](#состояние-проекта)**
- **[Дальнейшие планы](#дальнейшие-планы)**

## Архитектура и структура проекта

```
WebShop.sln
docker-compose.yml
docker/postgres/init.sql
scripts/
  start.ps1
  open-swagger.ps1
src/
  Monolith/
    Api/              ← ASP.NET Core монолит (Products, Orders)
    Application/      ← бизнес‑логика
    Domain/           ← доменные сущности
    Infrastructure/   ← EF Core + Redis
  Services/
    IdentityService/  ← регистрация/логин, JWT
    CatalogService/   ← продукты/категории, Redis‑кэш
    OrderService/     ← создание заказов, публикация OrderCreated
    PaymentService/   ← потребление OrderCreated, публикация PaymentCompleted
  Shared/
    Contracts/        ← общие контракты событий (MassTransit)
  WebApp/             ← фронтенд (React + Vite + Chakra UI)
```

- **Репозиторий решений**: `WebShop.sln`
- **Docker Compose**: [`docker-compose.yml`](./docker-compose.yml)
- **Инициализация БД**: [`docker/postgres/init.sql`](./docker/postgres/init.sql)
- **Скрипты**: [`scripts/start.ps1`](./scripts/start.ps1), [`scripts/open-swagger.ps1`](./scripts/open-swagger.ps1)
- **Монолит**: [`src/Monolith/Api`](./src/Monolith/Api)
- **Микросервисы**: [`src/Services`](./src/Services)
- **Контракты событий**: [`src/Shared/Contracts`](./src/Shared/Contracts)
- **Фронтенд**: [`src/WebApp`](./src/WebApp)

## Быстрый старт (Docker)
1. Установите Docker Desktop
2. В корне проекта выполните:

```bash
docker compose up -d --build
```

Откроются сервисы и порты:

- **Monolith**: `http://localhost:5000` (Swagger: `http://localhost:5000/swagger`)
- **Identity**: `http://localhost:5001` (Swagger: `http://localhost:5001/swagger`)
- **Catalog**: `http://localhost:5002` (Swagger: `http://localhost:5002/swagger`)
- **Order**: `http://localhost:5003` (Swagger: `http://localhost:5003/swagger`)
- **Payment**: `http://localhost:5004` (Swagger: `http://localhost:5004/swagger`)
- **WebApp**: `http://localhost:5173`
- **RabbitMQ UI**: `http://localhost:15672` (логин/пароль: `guest/guest`)
- **PostgreSQL**: `localhost:5432` (`postgres/postgres`)
- **Redis**: `localhost:6379`

Полезно: открыть Swagger для всех сервисов:

```powershell
./scripts/open-swagger.ps1 -Start
```

Авто‑скрипт запуска и проверки потока:

```powershell
./scripts/start.ps1
```

## Локальная разработка

Вариант с Docker‑инфраструктурой и локальным запуском приложений:

1) Поднять инфраструктуру:

```bash
docker compose up -d postgres rabbitmq redis
```

2) Запустить сервисы (в отдельных терминалах):

```bash
dotnet run --project ./src/Services/IdentityService/IdentityService.Api.csproj
dotnet run --project ./src/Services/CatalogService/CatalogService.Api.csproj
dotnet run --project ./src/Services/OrderService/OrderService.Api.csproj
dotnet run --project ./src/Services/PaymentService/PaymentService.Api.csproj
dotnet run --project ./src/Monolith/Api/Monolith.Api.csproj
```

3) Запустить фронтенд:

```bash
cd ./src/WebApp
npm i
npm run dev
```

## Сервисы и API

### IdentityService (JWT)
- Путь: [`src/Services/IdentityService`](./src/Services/IdentityService)
- Эндпойнты:
  - `POST /api/auth/register`
  - `POST /api/auth/login` → `{ accessToken }`
  - `GET /api/trace`, `GET /api/trace/{id}` — события трассировки
- Конфигурация JWT: [`appsettings.json`](./src/Services/IdentityService/appsettings.json)

### CatalogService (продукты, Redis‑кэш)
- Путь: [`src/Services/CatalogService`](./src/Services/CatalogService)
- Эндпойнты:
  - `GET /api/products` — с кэшированием Redis (5 мин)
  - `POST /api/products` — создание
  - `GET /api/trace`, `GET /api/trace/{id}`
- Кэш: [`Redis` в appsettings](./src/Services/CatalogService/appsettings.json)

### OrderService (заказы, MassTransit)
- Путь: [`src/Services/OrderService`](./src/Services/OrderService)
- Эндпойнты:
  - `GET /api/orders`
  - `POST /api/orders` — создаёт заказ, публикует `OrderCreated`
  - `GET /api/trace`, `GET /api/trace/{id}`
- Подписчик: `PaymentCompleted` (меняет статус заказа) — реализация упрощена

### PaymentService (платежи, MassTransit)
- Путь: [`src/Services/PaymentService`](./src/Services/PaymentService)
- HTTP‑интерфейса нет, только Swagger и трассировка:
  - `GET /swagger`
  - `GET /api/trace`, `GET /api/trace/{id}`
- Подписчик: `OrderCreated`, публикует `PaymentCompleted`

### Монолит (Products, Orders)
- Путь: [`src/Monolith/Api`](./src/Monolith/Api)
- Эндпойнты:
  - `GET /api/products`, `POST /api/products`
  - `GET /api/orders`, `POST /api/orders`
- Инфраструктура: EF Core + Redis, конфиг в [`appsettings.json`](./src/Monolith/Api/appsettings.json)

### Контракты событий (MassTransit)
- Путь: [`src/Shared/Contracts`](./src/Shared/Contracts)

```csharp
public interface OrderCreated { Guid OrderId { get; } Guid CustomerId { get; } decimal Total { get; } }
public interface PaymentCompleted { Guid OrderId { get; } bool Success { get; } }
```

## Фронтенд (WebApp)
- Путь: [`src/WebApp`](./src/WebApp)
- Стек: React 18, Vite 5, TypeScript, Chakra UI
- Скрипты: `npm run dev`, `npm run build`, `npm run preview`
- Возможности UI:
  - Регистрация/вход (JWT) через IdentityService
  - Каталог из CatalogService, добавление в корзину, оформление заказа (OrderService)
  - Вкладка Flow: пошаговый сценарий с `X-Trace-Id` и тайм‑линией событий (сбор из `/api/trace` всех сервисов)

Ключевые файлы:
- [`src/WebApp/src/api.ts`](./src/WebApp/src/api.ts) — вызовы API, сценарии с `X-Trace-Id`
- [`src/WebApp/src/App.tsx`](./src/WebApp/src/App.tsx) — UI магазина + корзина
- [`src/WebApp/src/Flow.tsx`](./src/WebApp/src/Flow.tsx) — демонстрация трассировки

## Скрипты
- **`scripts/start.ps1`** — поднимает контейнеры, проверяет готовность, выполняет сценарий (регистрация, логин, каталог, создание заказа).
- **`scripts/open-swagger.ps1`** — открывает Swagger всех сервисов. Ключ `-Start` предварительно запустит контейнеры.

## Переменные окружения и конфигурация

PostgreSQL создаётся ини‑скриптом [`docker/postgres/init.sql`](./docker/postgres/init.sql).

- **Identity** [`appsettings.json`](./src/Services/IdentityService/appsettings.json)
  - `ConnectionStrings:DefaultConnection = Host=postgres;Port=5432;Database=identity_db;Username=postgres;Password=postgres`
  - `Jwt:Issuer = webshop-identity`
  - `Jwt:Audience = webshop-clients`
  - `Jwt:Key = SuperSecretDevelopmentKey_ChangeInProd_1234567890`
- **Catalog** [`appsettings.json`](./src/Services/CatalogService/appsettings.json)
  - `ConnectionStrings:DefaultConnection = Host=postgres;Port=5432;Database=catalog_db;Username=postgres;Password=postgres`
  - `Redis:Configuration = redis:6379`, `Redis:InstanceName = catalog:`
- **Order** [`appsettings.json`](./src/Services/OrderService/appsettings.json)
  - `ConnectionStrings:DefaultConnection = Host=postgres;Port=5432;Database=order_db;Username=postgres;Password=postgres`
  - `RabbitMq:Host = rabbitmq`, `Username = guest`, `Password = guest`
- **Payment** [`appsettings.json`](./src/Services/PaymentService/appsettings.json)
  - `RabbitMq:Host = rabbitmq`, `Username = guest`, `Password = guest`
- **Монолит** [`appsettings.json`](./src/Monolith/Api/appsettings.json)
  - `ConnectionStrings:DefaultConnection = Host=postgres;Port=5432;Database=monolith_db;Username=postgres;Password=postgres`
  - `Redis:Configuration = redis:6379`, `Redis:InstanceName = monolith:`

Примечания:
- Для локального запуска используется `EnsureCreated()` вместо миграций EF Core.
- Секреты (JWT key и т.п.) хранятся в конфиге только для дев‑сценария; в продакшене используйте переменные окружения или Secret Manager.

## Трассировка и сценарии

- Все сервисы принимают заголовок `X-Trace-Id` и пишут события в локальный `TraceStore`.
- В каждом сервисе доступны:
  - `GET /api/trace` — последние события
  - `GET /api/trace/{id}` — события по конкретному Trace Id
- Фронтенд‑вкладка Flow позволяет запустить сценарий: регистрация → вход → каталог → создание заказа; тайм‑линия собирается из всех сервисов.

## Требования задания и реализация

- Написать монолит с 5–10 доменными сущностями (REST API, модули по чистой архитектуре)
  - Реализовано: монолит `Monolith.Api` с сущностями `Product`, `Category`, `Customer`, `Order`, `OrderItem`, `Cart`, `CartItem`, `Payment`, `InventoryItem` — см. [`src/Monolith/Domain/Entities.cs`](./src/Monolith/Domain/Entities.cs); модули `Application`/`Infrastructure`.
- Попилить на микросервисы
  - Реализовано: `IdentityService`, `CatalogService`, `OrderService`, `PaymentService` — см. [`src/Services`](./src/Services).
- Обернуть в Docker контейнеры
  - Реализовано: `docker-compose.yml` + Dockerfile для каждого сервиса — см. [`docker-compose.yml`](./docker-compose.yml), `src/*/*/Dockerfile`.
- Реализовать пример распределённой транзакции (оркестрация/хореография)
  - Реализовано: хореография через события `OrderCreated` → `PaymentCompleted` на RabbitMQ. Публикация — [`OrderService/Controllers/OrdersController.cs`](./src/Services/OrderService/Controllers/OrdersController.cs), обработка — [`PaymentService/Consumers/OrderCreatedConsumer.cs`](./src/Services/PaymentService/Consumers/OrderCreatedConsumer.cs), фиксация статуса — [`OrderService/Consumers/PaymentCompletedConsumer.cs`](./src/Services/OrderService/Consumers/PaymentCompletedConsumer.cs).
- Реализовать асинхронное взаимодействие с использованием паттерна transaction outbox или transaction inbox
  - Реализовано: асинхронное взаимодействие через шину; демонстрация упрощённого варианта без отдельной таблицы EF Outbox. Переход на полноценный Outbox обозначен в коде `OrderService`.
- Подключить шину данных (RabbitMQ)
  - Реализовано: MassTransit + RabbitMQ — конфиг в `Program.cs` сервисов: [`OrderService`](./src/Services/OrderService/Program.cs), [`PaymentService`](./src/Services/PaymentService/Program.cs).
- Авторизация/аутентификация через единый сервис (JWT)
  - Реализовано: `IdentityService` (ASP.NET Identity) — регистрация/логин, выдача JWT — см. [`AuthController.cs`](./src/Services/IdentityService/Controllers/AuthController.cs) и [`appsettings.json`](./src/Services/IdentityService/appsettings.json).
- Кэширование данных в Redis
  - Реализовано: кэширование каталога в `CatalogService` — см. [`ProductsController.cs`](./src/Services/CatalogService/Controllers/ProductsController.cs), настройки Redis — [`appsettings.json`](./src/Services/CatalogService/appsettings.json). Монолит также настроен на Redis.
- Подключение к БД
  - Реализовано: PostgreSQL через EF Core — строки подключения в `appsettings.json` каждого сервиса и монолита; инициализация баз — [`docker/postgres/init.sql`](./docker/postgres/init.sql).

## Состояние проекта

- [x] Монолит (Products, Orders) + доменные сущности (10 шт.)
- [x] Микросервисы: Identity, Catalog, Order, Payment
- [x] Контейнеризация (Docker Compose)
- [x] Асинхронный обмен событиями (RabbitMQ, MassTransit)
- [x] Хореография распределённой транзакции (OrderCreated → PaymentCompleted)
- [x] JWT‑аутентификация (IdentityService)
- [x] Кэш Redis (Catalog, Monolith)
- [x] База данных PostgreSQL (EF Core)
- [x] Swagger/OpenAPI, React UI, трассировка `X-Trace-Id`
- [ ] Transactional Outbox (EF) — не включён, возможна активация при необходимости
- [ ] `[Authorize]` на бизнес‑эндпойнтах — по требованию
- [ ] Тесты и CI/CD

## Дальнейшие планы

- Добавить EF‑миграции и включить Outbox (EF) для надёжной доставки событий
- Закрыть публичные эндпойнты атрибутами `[Authorize]` и скопами
- Добавить конфигурацию через переменные окружения (prod/dev)
- Вынести UI‑конфиг эндпойнтов в `.env`
- Автоматические тесты, линтинг и GitHub Actions

---

Если что‑то сломалось или есть идеи улучшений — создавайте issue/PR. Удачи!

