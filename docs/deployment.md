## Deployment Manual: ConfigVault в Yandex Cloud

### 1. Подготовка облака

1. Зарегистрируйтесь в Yandex Cloud и создайте платежный аккаунт (даже для бесплатного использования нужно привязать карту — вас не спишут при использовании бесплатных квот).
2. В консоли управления создайте каталог (folder), в котором будут все ресурсы проекта.
3. Установите и настройте Yandex Cloud CLI (`yc`). Инструкция: [](https://cloud.yandex.com/ru/docs/cli/quickstart#install.%5Breference:1%5D)
4. Создайте сервисный аккаунт с ролями `functions.functionInvoker` и `ydb.viewer` (позже добавим `ydb.editor`).

### 2. Создание базы данных YDB (Serverless)

**Через консоль управления:**

1. В списке сервисов выберите **Managed Service for YDB**.
2. Нажмите **Создать базу данных**.
3. Введите имя БД (например, `configvault-db`).
4. В блоке **Тип базы данных** выберите опцию **Serverless**.[](https://cloud.yandex.com/ru/docs/ydb/operations/manage-databases)
5. Нажмите **Создать базу данных** и дождитесь статуса `Running`.
    

**Через CLI:**
```bash
yc ydb database create configvault-db --serverless
```

После создания запишите эндпоинт базы данных: `grpcs://ydb.serverless.yandexcloud.net:2135/?database=/ru-central1/<folder-id>/<db-id>`[](https://cloud.yandex.com/ru/docs/ydb/operations/manage-databases) 
Document API эндпоинт: 
`https://docapi.serverless.yandexcloud.net/ru-central1/<folder-id>/<db-id>`[](https://cloud.yandex.com/ru/docs/ydb/operations/manage-databases).



### 3. Локальная разработка с Docker

Используйте официальный образ `local-ydb` с поддержкой PostgreSQL-протокола.[](https://ydb.tech/docs/en/postgresql/docker-connect)

`docker-compose.yml`:
```yaml
services:
  ydb:
    image: ghcr.io/ydb-platform/local-ydb:nightly
    ports:
      - "5432:5432"   # PostgreSQL-протокол
      - "8765:8765"   # Web-интерфейс
    environment:
      - YDB_USE_IN_MEMORY_PDISKS=true
      - POSTGRES_USER=${YDB_PG_USER:-root}
      - POSTGRES_PASSWORD=${YDB_PG_PASSWORD:-1234}
      - YDB_EXPERIMENTAL_PG=1
```

Запуск: `docker compose up -d --pull=always`[](https://ydb.tech/docs/en/postgresql/docker-connect). 
После этого можно подключаться к `localhost:5432` к базе `local` пользователем `root` с паролем `1234`. Веб-интерфейс доступен по адресу `http://localhost:8765`


### 4. Переменные окружения

Функция должна получать три обязательные переменные через окружение Yandex Cloud Functions. Вот как их сгенерировать:

```bash
# JWT_SECRET (любая длинная случайная строка)
openssl rand -base64 64

# ENCRYPTION_KEY (256-битный ключ AES в Base64)
openssl rand -base64 32
```

`CONNECTION_STRING` — эндпоинт базы данных, полученный при создании YDB. Настройка в консоли — в разделе **Редактор** → **Параметры**, затем кнопка **Добавить** переменную окружения.[](https://cloud.yandex.com/en-ru/docs/functions/operations/function/environment-variables-add)

В `Program.cs` эти переменные читаются стандартным образом:
```csharp
var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET");
var encryptionKey = Environment.GetEnvironmentVariable("ENCRYPTION_KEY");
var connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING");
```

### 5. Развертывание Cloud Functions для .NET

#### Особенности .NET в Yandex Cloud Functions

Cloud Functions поддерживают .NET 8 (runtime `dotnet8`). Приложение должно быть опубликовано как самодостаточное (self-contained), чтобы избежать проблем с зависимостями.

**Сборка проекта в self-contained режиме:**
```bash
dotnet publish -c Release -r linux-x64 --self-contained true
```


### 6. Настройка API Gateway

API Gateway предоставляет красивый постоянный URL для функции и позволяет маршрутизировать запросы.

1. В консоли управления перейдите в **API Gateway** и нажмите **Создать API-шлюз**.[](https://yandex.cloud/ru/docs/api-gateway/quickstart)
2. В поле **Имя** введите `configvault-gw`.
3. Добавьте OpenAPI-спецификацию (заготовка ниже).
4. После создания в информации о шлюзе появится поле **Служебный домен** — это и есть базовый URL вашего API.

**Заготовка OpenAPI-спецификации** (детали эндпоинтов возьмите из `docs/api.md`):
```yaml
openapi: "3.0.0"
info:
  version: 1.0.0
  title: ConfigVault API
paths:
  /api/auth/login:
    post:
      x-yc-apigateway-integration:
        type: cloud_functions
        function_id: <ID вашей функции>
        service_account_id: <ID сервисного аккаунта>
      operationId: login
  /api/projects:
    get:
      x-yc-apigateway-integration:
        type: cloud_functions
        function_id: <ID вашей функции>
        service_account_id: <ID сервисного аккаунта>
      operationId: listProjects
```
Полный список эндпоинтов описан в [API](/docs/API.md).

### 7. Тестирование локального окружения

После запуска Docker-контейнера с YDB и .NET-приложения:
```bash
# Создание таблиц через веб-интерфейс YDB
open http://localhost:8765

# Или через psql
psql postgresql://root:1234@localhost:5432/local
```

Выполните скрипты из `docs/schema.sql`.

**Проверка функции локально:**
```bash
curl -X POST https://<api-gateway-url>/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"login":"admin","password":"secret123"}'
```
Ожидаемый ответ: `201 Created` с JSON-объектом нового пользователя.

**Проверка CLI:**
```bash
cv auth login
cv projects create "Test Project"
cv keys set TestProject DATABASE_URL "postgresql://localhost/test" --secret
cv keys list TestProject
```


В Yandex Cloud Billing можно настроить лимиты потребления и получать уведомления при их превышении — это предотвратит случайные расходы.