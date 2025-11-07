## WebShop (Monolith + Microservices)

Интернет‑магазин: монолит (Clean Architecture) и набор микросервисов. Реализованы JWT‑аутентификация, обмен событиями через RabbitMQ (MassTransit), кэширование Redis, БД PostgreSQL, фронтенд на React (Vite + Chakra UI), полная контейнеризация Docker.

![.NET](https://img.shields.io/badge/.NET-9-512BD4?logo=dotnet&logoColor=white) ![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-Web%20API-512BD4) ![RabbitMQ](https://img.shields.io/badge/RabbitMQ-3.x-FF6600?logo=rabbitmq&logoColor=white) ![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-4169E1?logo=postgresql&logoColor=white) ![Redis](https://img.shields.io/badge/Redis-7-DC382D?logo=redis&logoColor=white) ![Docker](https://img.shields.io/badge/Docker-Compose-2496ED?logo=docker&logoColor=white) ![React](https://img.shields.io/badge/React-18-61DAFB?logo=react&logoColor=061A23)
[![CI](https://github.com/A-Mak7u/web2/actions/workflows/ci.yml/badge.svg)](https://github.com/A-Mak7u/web2/actions/workflows/ci.yml)

## Содержание
- **[Архитектура и компоненты](#архитектура-и-компоненты)**
- **[Как это работает](#как-это-работает)**
- **[Сервисы и API](#сервисы-и-api)**
- **[Фронтенд](#фронтенд)**
- **[Трассировка](#трассировка)**
- **[Требования задания и реализация](#требования-задания-и-реализация)**
- **[Состояние проекта](#состояние-проекта)**
- **[Технологии](#технологии)**

## Архитектура и компоненты

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
- **Монолит**: [`src/Monolith/Api`](./src/Monolith/Api)
- **Микросервисы**: [`src/Services`](./src/Services)
- **Контракты событий**: [`src/Shared/Contracts`](./src/Shared/Contracts)
- **Фронтенд**: [`src/WebApp`](./src/WebApp)

## Как это работает

- **Аутентификация (IdentityService)**: регистрация и вход пользователя с выдачей JWT. Токен используется клиентом при обращении к защищённым API (в проекте показана выдача и потребление токена; строгие политики можно включить атрибутами `[Authorize]`).
- **Каталог (CatalogService)**: чтение товаров из PostgreSQL, ответы кэшируются в Redis на несколько минут. При добавлении товара кэш инвалидацируется.
- **Оформление заказа (OrderService ↔ PaymentService)**: после создания заказа публикуется событие `OrderCreated` (RabbitMQ/MassTransit). Платёжный сервис получает событие, имитирует обработку и публикует `PaymentCompleted`. Сервис заказов принимает событие и обновляет статус заказа. Это пример хореографии распределённой транзакции.
- **Оформление заказа (OrderService ↔ PaymentService)**: после создания заказа публикуется событие `OrderCreated` (RabbitMQ/MassTransit). Платёжный сервис получает событие, имитирует обработку и публикует `PaymentCompleted`. Сервис заказов принимает событие и обновляет статус заказа. Публикация событий в `OrderService` выполняется через транзакционный outbox (MassTransit EF Outbox + Bus Outbox) для атомарности. Это пример хореографии распределённой транзакции.

```
Identity (JWT)
   |          
   |  login/register → accessToken
   v          
Frontend ———— create order ———→ OrderService
                               |  (save Order; Outbox Publish: OrderCreated)
                               v
                          RabbitMQ
                               |
                               v
                         PaymentService
                               |  (process; Publish: PaymentCompleted)
                               v
                             RabbitMQ
                               |
                               v
                           OrderService (update status: Paid/Failed)
```
- **Монолит**: содержит схожие функции (продукты, заказы) внутри одного приложения и общей БД/кэша, демонстрируя подход Clean Architecture.
- **Трассировка**: сервисы принимают заголовок `X-Trace-Id` и пишут сообщения в локальное хранилище событий; фронтенд собирает ленту из всех сервисов и показывает тайм‑линию.
 - **Безопасность**: write‑эндпойнты защищены JWT (`[Authorize]`) в CatalogService, OrderService и Монолите; чтение (GET) оставлено публичным для удобства демонстрации.

 

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

## Фронтенд
- Путь: [`src/WebApp`](./src/WebApp)
- Стек: React 18, Vite 5, TypeScript, Chakra UI
- Возможности UI:
  - Регистрация/вход (JWT) через IdentityService
  - Каталог из CatalogService, добавление в корзину, оформление заказа (OrderService)
  - Вкладка Flow: пошаговый сценарий с `X-Trace-Id` и тайм‑линией событий (сбор из `/api/trace` всех сервисов)

Ключевые файлы:
- [`src/WebApp/src/api.ts`](./src/WebApp/src/api.ts) — вызовы API, сценарии с `X-Trace-Id`
- [`src/WebApp/src/App.tsx`](./src/WebApp/src/App.tsx) — UI магазина + корзина
- [`src/WebApp/src/Flow.tsx`](./src/WebApp/src/Flow.tsx) — демонстрация трассировки

 

## Трассировка

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
  - Реализовано: транзакционный Outbox в `OrderService` (MassTransit EF Outbox + Bus Outbox) — см. [`OrderService/Program.cs`](./src/Services/OrderService/Program.cs) и модель контекста с сущностями inbox/outbox — [`OrderService/Data/OrderDbContext.cs`](./src/Services/OrderService/Data/OrderDbContext.cs).
- Подключить шину данных (RabbitMQ)
  - Реализовано: MassTransit + RabbitMQ — конфиг в `Program.cs` сервисов: [`OrderService`](./src/Services/OrderService/Program.cs), [`PaymentService`](./src/Services/PaymentService/Program.cs).
- Авторизация/аутентификация через единый сервис (JWT)
  - Реализовано: `IdentityService` (ASP.NET Identity) — регистрация/логин, выдача JWT — см. [`AuthController.cs`](./src/Services/IdentityService/Controllers/AuthController.cs) и [`appsettings.json`](./src/Services/IdentityService/appsettings.json). Write‑эндпойнты защищены `[Authorize]` (Catalog, Order, Монолит).
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
- [x] Transactional Outbox (MassTransit EF Outbox + Bus Outbox) в `OrderService`
- [x] JWT‑защита write‑эндпойнтов (`[Authorize]`)
- [x] CI (GitHub Actions)
- [ ] Тесты

## Технологии

- .NET 9 / ASP.NET Core Web API
- EF Core + PostgreSQL
- MassTransit + RabbitMQ
- Redis (StackExchange.Redis Cache)
- Docker / Docker Compose
- React 18 + Vite 5 + TypeScript + Chakra UI
- OpenAPI/Swagger

