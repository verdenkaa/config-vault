# API ConfigVault

## 1. Общие сведения

API ConfigVault предоставляет REST-интерфейс для управления проектами и ключами, а также для аутентификации пользователей. Все эндпоинты (кроме `/api/auth/*`) требуют аутентификации через JWT-токен. Запросы и ответы передаются в формате JSON.

**Аутентификация:**  
После успешного входа клиент получает JWT-токен, который необходимо передавать в заголовке `Authorization: Bearer <token>` для всех защищённых запросов.

**Формат ошибок:**  
При возникновении ошибок сервер возвращает JSON с полями `message` (строка с описанием) и `code` (числовой HTTP-статус). Пример:

```json
{
  "message": "Проект не найден",
  "code": 404
}
```


## 2. Аутентификация

### 2.1. Регистрация пользователя

Создаёт новую учётную запись.

**Запрос:**

```HTTP
POST /api/auth/register
Content-Type: application/json

{
  "login": "alice",
  "password": "securepass123"
}
```

**Ответ (201 Created):**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "login": "alice",
  "created_at": "2026-05-13T10:30:00Z"
}
```

**Возможные ошибки:**

- `409 Conflict` — пользователь с таким логином уже существует.
- `400 Bad Request` — невалидные данные (короткий пароль и т.п.).

### 2.2. Вход в систему

Аутентифицирует пользователя и возвращает JWT-токен.

**Запрос:**
```HTTP
POST /api/auth/login
Content-Type: application/json

{
  "login": "alice",
  "password": "securepass123"
}
```

**Ответ (200 OK):**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "user": {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "login": "alice"
  }
}
```

**Возможные ошибки:**

- `401 Unauthorized` — неверный логин или пароль.


## 3. Пользователи

### 3.1. Информация о текущем пользователе

Возвращает профиль аутентифицированного пользователя.

**Запрос:**
```HTTP
GET /api/users/me
Authorization: Bearer <token>
```

**Ответ (200 OK):**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "login": "alice",
  "created_at": "2026-05-13T10:30:00Z"
}
```
**Возможные ошибки:**

- `401 Unauthorized` — токен отсутствует или недействителен.


## 4. Проекты

Все операции с проектами требуют аутентификации. Доступ к конкретному проекту разрешён только его участникам (роль `viewer` и выше).

### 4.1. Создание проекта

Создаёт новый проект. Пользователь, создавший проект, автоматически становится его владельцем (`owner`).

**Запрос:**
```HTTP
POST /api/projects
Authorization: Bearer <token>
Content-Type: application/json

{
  "name": "My Production Bot"
}
```

**Ответ (201 Created):**
```json
{
  "id": "660e8400-e29b-41d4-a716-446655440001",
  "name": "My Production Bot",
  "created_at": "2026-05-13T11:00:00Z",
  "version": 1
}
```

### 4.2. Список проектов пользователя

Возвращает все проекты, в которых пользователь состоит.

**Запрос:**
```HTTP
GET /api/projects
Authorization: Bearer <token>
```

**Ответ (200 OK):**
```json
[
  {
    "id": "660e8400-e29b-41d4-a716-446655440001",
    "name": "My Production Bot",
    "role": "owner",
    "created_at": "2026-05-13T11:00:00Z",
    "version": 3
  },
  {
    "id": "770e8400-e29b-41d4-a716-446655440002",
    "name": "Sandbox",
    "role": "editor",
    "created_at": "2026-05-12T09:20:00Z",
    "version": 5
  }
]
```
> Поле `role` указывает роль текущего пользователя в проекте.

### 4.3. Получение информации о проекте

**Запрос:**
```HTTP
GET /api/projects/{projectId}
Authorization: Bearer <token>
```

**Ответ (200 OK):**

```json
{
  "id": "660e8400-e29b-41d4-a716-446655440001",
  "name": "My Production Bot",
  "created_at": "2026-05-13T11:00:00Z",
  "version": 3
}
```

**Возможные ошибки:**

- `404 Not Found` — проект не существует.
- `403 Forbidden` — пользователь не является участником проекта.

### 4.4. Удаление проекта

> Требует роль `owner`.

**Запрос:**
```HTTP
DELETE /api/projects/{projectId}
Authorization: Bearer <token>
```

**Ответ (204 No Content)**

**Возможные ошибки:**

- `404 Not Found`
- `403 Forbidden` — недостаточно прав (не `owner`).
    

### 4.5. Управление участниками

#### 4.5.1. Список участников проекта

**Запрос:**
```http
GET /api/projects/{projectId}/members
Authorization: Bearer <token>
```

**Ответ (200 OK):**
```json
[
  {
    "userId": "550e8400-e29b-41d4-a716-446655440000",
    "login": "alice",
    "role": "owner"
  },
  {
    "userId": "880e8400-e29b-41d4-a716-446655440003",
    "login": "bob",
    "role": "editor"
  }
]
```

#### 4.5.2. Добавление участника

>Требует роль `owner`.

**Запрос:**
```http
POST /api/projects/{projectId}/members
Authorization: Bearer <token>
Content-Type: application/json

{
  "login": "bob",
  "role": "editor"
}
```

**Ответ (201 Created):**
```json
{
  "userId": "880e8400-e29b-41d4-a716-446655440003",
  "login": "bob",
  "role": "editor"
}
```

**Возможные ошибки:**

- `404 Not Found` — пользователь с таким логином не найден.
- `400 Bad Request` — недопустимая роль или пользователь уже в проекте.
- `403 Forbidden` — недостаточно прав.

#### 4.5.3. Изменение роли участника

Требует роль `owner`.

**Запрос:**
```http
PUT /api/projects/{projectId}/members/{userId}
Authorization: Bearer <token>
Content-Type: application/json

{
  "role": "viewer"
}
```

**Ответ (200 OK):**
```json
{
  "userId": "880e8400-e29b-41d4-a716-446655440003",
  "login": "bob",
  "role": "viewer"
}
```

#### 4.5.4. Удаление участника

Требует роль `owner`.

**Запрос:**
```http
DELETE /api/projects/{projectId}/members/{userId}
Authorization: Bearer <token>
```

**Ответ (204 No Content)**



## 5. Ключи

Все операции с ключами требуют аутентификации и соответствующей роли в проекте (`viewer` для чтения, `editor` для изменения).

### 5.1. Список ключей проекта

Возвращает все ключи проекта. Значения секретных ключей (`is_secret: true`) возвращаются замаскированными (`***`), если запрос не содержит специального флага (см. примечание).

**Запрос:**
```http
GET /api/projects/{projectId}/keys
Authorization: Bearer <token>
```

**Ответ (200 OK):**
```json
[
  {
    "name": "DATABASE_URL",
    "is_secret": false,
    "value": "https://db.example.com",
    "version": 2,
    "updated_at": "2026-05-13T11:30:00Z"
  },
  {
    "name": "API_KEY",
    "is_secret": true,
    "value": "***",
    "version": 5,
    "updated_at": "2026-05-13T12:00:00Z"
  }
]
```
>**Примечание:** для получения реальных значений секретов необходимо запросить конкретный ключ с параметром `?reveal=true`

### 5.2. Получение значения ключа

**Запрос:**
```http
GET /api/projects/{projectId}/keys/{keyName}?reveal=true
Authorization: Bearer <token>
```
Параметр `reveal` опциональный. Если `true` значение секрета возвращается в открытом виде. В противном случае значение маскируется.

**Ответ (200 OK):**
```json
{
  "name": "API_KEY",
  "is_secret": true,
  "value": "sk-1234567890abcdef",
  "version": 5,
  "updated_at": "2026-05-13T12:00:00Z"
}
```
