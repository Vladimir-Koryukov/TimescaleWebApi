## Технологии
- .NET 8 (ASP.NET Core Web API)
- EF Core 8
- PostgreSQL (через Docker, образ `timescale/timescaledb:latest-pg16`)
- Swagger (Swashbuckle)

---

## Быстрый старт

### 1) Поднять базу данных в Docker
В корне решения (где `docker-compose.yml`):

```bash
docker compose up -d
docker ps
```

База поднимается на порту **5433** (в контейнере 5432).

### 2) Настроить строку подключения
В `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Port=5433;Database=timescale_db;Username=postgres;Password=postgres"
  }
}
```

### 3) Применить миграции
В папке проекта (где `.csproj`):

```bash
dotnet ef database update
```

### 4) Запустить API
- Visual Studio: `F5`
- или через CLI:

```bash
dotnet run
```

Swagger будет доступен по адресу:

```
https://localhost:<port>/swagger
```

---

## API

### Загрузка CSV
**POST** `/api/files/upload`

Формат строки: `Date;ExecutionTime;Value`

Ограничения:
- 1–10000 строк
- `Date` ≥ 2000-01-01 и ≤ текущего времени
- `ExecutionTime` ≥ 0
- `Value` ≥ 0

---

### Получение Results
**GET** `/api/results`

Поддерживаются фильтры и пагинация (`page`, `pageSize`).

---

### Последние 10 Values
**GET** `/api/files/{fileName}/values/latest`

---

## Архитектура
- Controllers — HTTP слой
- Services — бизнес-логика
- Data / Entities — EF Core
- Middleware — обработка ошибок

---

## Сброс БД
```bash
docker compose down -v
docker compose up -d
dotnet ef database update
```
