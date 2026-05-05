# Структура базы данных

::: mermaid
erDiagram

USERS {
    int id PK
    string login
    string password
    int permission
}

USERS ||--o{ PROJECTS : contains

PROJECTS {
    int id PK
    string name
    int[] users_id FK
    timestamp created_at
}

PROJECTS ||--o{ KEYS : contains

KEYS {
    int id PK
    string project_id FK
    strint key_name
    boolean is_secret
    string secret_key
}

KEYS ||--|| KEYS_HISTORY : equal

KEYS_HISTORY {
    int id PK
    int key_id FK
    timestamp created_at
    string changed_by
    timestamp changed_at

}
:::