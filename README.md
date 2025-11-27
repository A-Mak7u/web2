## WebShop

Монолит и микросервисы для интернет‑магазина. Интеграция событиями (RabbitMQ/MassTransit), аутентификация по JWT, PostgreSQL + EF Core, кэш Redis, фронтенд на React. Оркестрация через Docker Compose.

[![CI](https://github.com/A-Mak7u/web2/actions/workflows/ci.yml/badge.svg)](https://github.com/A-Mak7u/web2/actions/workflows/ci.yml)

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

- Аутентификация: `IdentityService` выдает JWT; сервисы валидируют Bearer‑токен. Запись в `CatalogService`, `OrderService` и в монолите защищена JWT; чтение открыто.
- Каталог: данные в PostgreSQL; чтение кэшируется в Redis (TTL). На запись выполняется инвалидация кэша.
- Оформление заказа: `OrderService` сохраняет заказ и публикует событие `OrderCreated` через транзакционный outbox (MassTransit EF Outbox + Bus Outbox) в RabbitMQ. `PaymentService` потребляет `OrderCreated`, имитирует обработку, публикует `PaymentCompleted`. `OrderService` потребляет `PaymentCompleted` и обновляет статус заказа. Модель — хореография, обеспечивается конечная согласованность.
- Трассировка: клиент передает `X-Trace-Id`; сервисы логируют события в локальное хранилище трассировки. Фронтенд агрегирует ленту по trace id и отображает тайм‑линию.
- Монолит: реализует аналогичный функционал в одном процессе по слоям (Clean Architecture) с общей БД и Redis.

```
Client —(JWT)→ IdentityService
Client —(Bearer)→ OrderService —(save + Outbox: OrderCreated)→ RabbitMQ
RabbitMQ → PaymentService —(PaymentCompleted)→ RabbitMQ → OrderService (update status)
```

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