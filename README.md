## WebShop

Монолит и микросервисы для интернет‑магазина. Интеграция событиями (RabbitMQ/MassTransit), аутентификация по JWT, PostgreSQL + EF Core, кэш Redis, фронтенд на React. Оркестрация через Docker Compose.

[![CI](https://github.com/A-Mak7u/web2/actions/workflows/ci.yml/badge.svg)](https://github.com/A-Mak7u/web2/actions/workflows/ci.yml)

### Компоненты

- IdentityService — единый сервис аутентификации/авторизации (ASP.NET Identity, выдача JWT).
- CatalogService — каталог товаров (PostgreSQL), read‑through кэширование ответов в Redis, инвалидация на запись.
- OrderService — создание и изменение заказов, публикация доменных событий через outbox.
- PaymentService — обработка событий `OrderCreated`, моделирование платёжного провайдера, публикация результата.
- WebApp — SPA на React/Vite; взаимодействует с сервисами, прокидывает `X-Trace-Id` для трассировки.
- Monolith — альтернативная реализация функциональности в одном процессе с разделением на слои (Clean Architecture).
- Shared/Contracts — контракты сообщений для MassTransit.

### Структура репозитория

```
WebShop.sln
docker-compose.yml
docker/postgres/init.sql
scripts/
  start.ps1
  open-swagger.ps1
src/
  Monolith/            # монолит: API, Application, Domain, Infrastructure (EF + Redis)
    Api/
    Application/
    Domain/
    Infrastructure/
  Services/            # микросервисы
    IdentityService/   # JWT (регистрация/логин), ASP.NET Identity
    CatalogService/    # каталог товаров, Redis read-through cache
    OrderService/      # создание заказа, outbox + публикация событий
    PaymentService/    # обработка платежа, публикация результата
  Shared/
    Contracts/         # контракты событий (MassTransit)
  WebApp/              # фронтенд (React + Vite + Chakra UI)
```

### Архитектура и поток событий

- Аутентификация и доступ: `IdentityService` выдает JWT; сервисы валидируют Bearer‑токен. Запись в `CatalogService`, `OrderService` и монолите защищена JWT; публичное чтение оставлено открытым.
- Каталог и кэш: данные в PostgreSQL; чтение кэшируется в Redis с TTL. На запись — инвалидация ключей/префиксов.
- Обработка заказа (хореография): `OrderService` сохраняет заказ и публикует доменное событие `OrderCreated` через транзакционный outbox (MassTransit EF Outbox + Bus Outbox) в RabbitMQ. `PaymentService` потребляет `OrderCreated`, выполняет обработку и публикует `PaymentCompleted`. `OrderService` потребляет `PaymentCompleted` и переводит заказ в конечное состояние. Обеспечивается конечная согласованность между сервисами.
- Трассировка: клиент присваивает `X-Trace-Id`; сервисы пишут события в локальное хранилище трассировки. Фронтенд агрегирует события по trace id и визуализирует тайм‑линию.
- Монолит: реализует функционал каталога и заказов в одном приложении с общими слоями `Domain / Application / Infrastructure` и тем же стеком БД/кэша.

```
Client —(JWT)→ IdentityService
Client —(Bearer)→ OrderService —(save + Outbox: OrderCreated)→ RabbitMQ
RabbitMQ → PaymentService —(PaymentCompleted)→ RabbitMQ → OrderService (update status)
```

### Контракты событий (MassTransit)

- `OrderCreated { Guid OrderId; Guid CustomerId; decimal Total }`
- `PaymentCompleted { Guid OrderId; bool Success }`

Контракты размещены в `src/Shared/Contracts` и разделяются между сервисами для типобезопасной публикации/подписки.

### Последовательности взаимодействий

1) Регистрация/логин
   - `POST /api/auth/register`, затем `POST /api/auth/login` → `accessToken` (JWT).
   - Клиент сохраняет токен и передаёт его в `Authorization: Bearer ...` к защищённым endpoint’ам.

2) Просмотр каталога (кэш read‑through)
   - Клиент вызывает `GET /api/products` (CatalogService).
   - При кэш‑мисс сервис читает из PostgreSQL, возвращает ответ и записывает его в Redis с TTL.
   - При изменении каталога (POST) выполняется инвалидация соответствующих ключей/префиксов.

3) Создание заказа и оплата (события, outbox)
   - Клиент вызывает `POST /api/orders` (OrderService) с Bearer‑токеном.
   - OrderService записывает заказ в БД в транзакции и вносит событие в outbox.
   - Outbox публикует `OrderCreated` в RabbitMQ атомарно относительно записи заказа.
   - PaymentService потребляет `OrderCreated`, выполняет обработку и публикует `PaymentCompleted`.
   - OrderService потребляет `PaymentCompleted` и обновляет статус заказа (Paid/Failed).

### Надёжность и идемпотентность

- Outbox: MassTransit EF Outbox + Bus Outbox исключают потерю событий при сбоях.
- Повторы/ретраи: на уровне потребителей настроены политики повторных попыток (exponential/backoff) и dead‑letter (см. конфигурацию MassTransit в `Program.cs` сервисов).
- Идемпотентность потребителей: обработчики проверяют текущее состояние заказа перед изменением для избежания повторной обработки.

### Доступ и безопасность

- JWT в `IdentityService`; в остальных сервисах — валидация токена и авторизация на write‑операциях.
- Профили/ключи конфигурации JWT находятся в `appsettings.json` каждого сервиса.

### Данные и хранилища

- PostgreSQL: доменные сущности каталога и заказов (Product, Category, Order, OrderItem и т.д.).
- Redis: кэш запросов каталога; ключи сгруппированы по префиксам для целевой инвалидации.
- Трассировка: локальное событийное хранилище per‑service; выборка через `GET /api/trace` и `GET /api/trace/{id}`.

### API (кратко)

- Auth: `POST /api/auth/register`, `POST /api/auth/login`
- Catalog: `GET /api/products`, `POST /api/products`
- Orders: `GET /api/orders`, `POST /api/orders`
- Trace: `GET /api/trace`, `GET /api/trace/{id}` (в каждом сервисе)

### Запуск локально

- Требования: Docker, Docker Compose. (Для разработки: .NET 9 SDK, Node.js 18+)
- Поднять весь стек:

```bash
docker compose up -d
```

- Остановить:

```bash
docker compose down
```

- Swagger доступен у сервисов на `/swagger`. Фронтенд (`WebApp`) — через Vite (порт по умолчанию 5173) либо в контейнере из compose.

### Конфигурация и порты

- Конкретные порты и переменные окружения заданы в `docker-compose.yml` и `appsettings.json` каждого сервиса.
- Подключения к PostgreSQL/Redis и параметры MassTransit/RabbitMQ конфигурируются через переменные окружения и конфигурационные файлы.

### Монолит vs микросервисы

- Монолит: единое приложение с разбиением по слоям, единая база и кэш. Подходит для простого развёртывания и локальной разработки.
- Микросервисы: границы по доменам (Identity, Catalog, Order, Payment), обмен событиями, слабая связанность, независимое масштабирование.