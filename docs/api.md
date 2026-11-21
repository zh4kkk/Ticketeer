# Ticketeer Backend API

Документ описывает фактический API текущей реализации backend.

## Подключение

В локальной разработке backend использует адреса из `backend/Properties/launchSettings.json`:

- `http://localhost:5046`;
- `https://localhost:7277`.

Frontend Vite проксирует запросы `/api/*` на `http://localhost:5046`. Цель можно изменить переменной `VITE_API_PROXY_TARGET`. Для frontend-кода рекомендуется использовать относительные URL, например `/api/auth/login`.

Проверка доступности базы данных:

```http
GET /health
```

Ответ при доступной БД:

```json
{"status":"ok","database":"ok"}
```

При недоступной БД возвращается `503`.

## Аутентификация и роли

Вход выполняется через `POST /api/auth/login`. В защищённых запросах передавайте:

```http
Authorization: Bearer <token>
```

JWT проверяется по подписи, `Issuer`, `Audience`, сроку действия и активности пользователя. Старый токен деактивированного пользователя отклоняется.

Роли:

- `Cashier` — прохождение, собственная история, статистика и каталог;
- `Admin` — управление пользователями, группами, учебными материалами и административная статистика.

Самостоятельная регистрация отсутствует.

### Представление enum

ASP.NET Core сериализует текущие enum числовыми значениями:

| Enum | Значения |
|---|---|
| `UserRole` | `Cashier = 0`, `Admin = 1` |
| `AttemptKind` | `Training = 0`, `Exam = 1` |
| `AttemptSource` | `Prepared = 0`, `Random = 1` |
| `AttemptStatus` | `InProgress = 0`, `Completed = 1`, `Failed = 2`, `TimedOut = 3` |

Все `DateTimeOffset` передаются в ISO 8601; сервер хранит и формирует время в UTC. `DateOnly` в активности имеет формат `YYYY-MM-DD`.

## Формат ошибок

Ошибки сервисного слоя обычно возвращаются так:

```json
{"error":"Human-readable message"}
```

Основные статусы: `400` — ошибка параметров, `401` — нет корректной аутентификации, `403` — недостаточная роль, `404` — ресурс не найден, `409` — ожидаемый конфликт. Ошибки валидации модели ASP.NET Core возвращаются стандартным `ValidationProblemDetails`.

## Аутентификация и профиль

### `POST /api/auth/login`

Доступен без JWT.

```json
{"login":"cashier1","password":"password"}
```

Успешный ответ `200`:

```json
{"token":"<jwt>","expiresAt":"2026-11-21T12:00:00Z"}
```

Неверные данные — `401`.

### `GET /api/auth/me`

Доступен любому активному авторизованному пользователю.

```json
{
  "id": 2,
  "login": "cashier1",
  "fullName": "Иван Иванов",
  "role": "Cashier",
  "groupId": 1,
  "groupName": "Смена A"
}
```

## Административное управление

Все следующие endpoints доступны только `Admin`. Списки возвращают массивы, не `PagedResponse`.

### Пользователи — `/api/users`

- `GET /api/users` — список кассиров;
- `GET /api/users/{id}` — кассир по идентификатору;
- `POST /api/users` — создание кассира;
- `PUT /api/users/{id}` — полное обновление кассира;
- `POST /api/users/{id}/reset-password` — смена пароля кассира.

Создание и обновление используют поля `login`, `fullName`, `password` (только при создании/сбросе), `groupId`, `isActive` (при обновлении). Администраторов нельзя изменять через эти endpoints.

### Группы — `/api/groups`

- `GET /api/groups`;
- `GET /api/groups/{id}`;
- `POST /api/groups`;
- `PUT /api/groups/{id}`;
- `DELETE /api/groups/{id}`.

Тело создания/обновления: `name`, необязательное `description`.

### Темы — `/api/topics`

- `GET /api/topics?search=...` — список, поиск по названию;
- `GET /api/topics/{id}`;
- `POST /api/topics`;
- `PUT /api/topics/{id}`;
- `DELETE /api/topics/{id}`.

Тело создания/обновления: `name`, необязательное `description`.

### Вопросы — `/api/questions`

- `GET /api/questions?topicId=1&search=...` — список вопросов;
- `GET /api/questions/{id}` — административная деталь с правильными ответами;
- `POST /api/questions`;
- `PUT /api/questions/{id}`;
- `DELETE /api/questions/{id}`.

Тело создания/обновления:

```json
{
  "topicId": 1,
  "text": "Текст вопроса",
  "answerOptions": [
    {"text":"Вариант 1","isCorrect":true},
    {"text":"Вариант 2","isCorrect":false}
  ]
}
```

### Билеты — `/api/tickets`

- `GET /api/tickets?search=...&topicId=1`;
- `GET /api/tickets/{id}`;
- `POST /api/tickets`;
- `PUT /api/tickets/{id}`;
- `DELETE /api/tickets/{id}`.

Тело создания/обновления: `title`, необязательное `description`, `questionIds`.

### Экзамены — `/api/exams`

- `GET /api/exams?search=...`;
- `GET /api/exams/{id}`;
- `POST /api/exams`;
- `PUT /api/exams/{id}`;
- `DELETE /api/exams/{id}`.

Тело создания/обновления: `title`, необязательное `description`, ровно 10 `questionIds`.

Для административной детали вопросов билета/экзамена правильность вариантов доступна через существующий DTO административного вопроса.

## Каталог кассира

Все endpoints раздела доступны только `Cashier` и не возвращают правильность вариантов или состав экзамена.

Для `search` используется регистронезависимый поиск по подстроке через PostgreSQL `ILIKE`. Пробелы по краям удаляются, пустой поиск игнорируется, максимальная длина — 200 символов. `%`, `_` и `\\` экранируются.

### Темы

```http
GET /api/catalog/topics?search=касс
```

Возвращаются `id`, `name`, `description`, `questionCount`.

### Билеты

```http
GET /api/catalog/tickets?search=льготы&topicId=1&isCompleted=false&page=1&pageSize=20
GET /api/catalog/tickets/{id}
```

Список и деталь содержат `id`, `title`, `description`, `questionCount`, `isCompleted`. Деталь также содержит уникальные `topics` с `id` и `name`. `isCompleted` означает наличие хотя бы одной завершённой подготовленной тренировки текущего кассира.

Ответ списка:

```json
{
  "items":[{"id":3,"title":"Билет 3","description":null,"questionCount":5,"isCompleted":false}],
  "page":1,
  "pageSize":20,
  "totalCount":1
}
```

### Экзамены

```http
GET /api/catalog/exams?search=базовый&page=1&pageSize=20
GET /api/catalog/exams/{id}
```

Возвращаются `id`, `title`, `description`, `questionCount`, `timeLimitSeconds` (`1200`) и `maxWrongAnswers` (`2`). Состав экзамена не возвращается.

### Вопросы

```http
GET /api/catalog/questions?search=багаж&topicId=1&page=1&pageSize=20
GET /api/catalog/questions/{id}
```

Список содержит `id`, `topicId`, `topicName`, `text`. Деталь дополнительно содержит варианты с `position` и `text`, но не содержит `isCorrect`.

## Прохождение попыток

Все endpoints раздела доступны только `Cashier`; пользователь определяется из JWT.

### Запуск

- `POST /api/attempts/training/tickets/{ticketId}` — подготовленный билет;
- `POST /api/attempts/training/random` — случайная тренировка;
- `POST /api/attempts/exams/{examId}` — подготовленный экзамен;
- `POST /api/attempts/exams/random` — случайный экзамен.

Тело случайной тренировки:

```json
{"topicId":1,"questionCount":5}
```

Успешный запуск возвращает `201 Created` и `AttemptResponse`.

### Состояние и ответы

- `GET /api/attempts/{id}` — собственная попытка;
- `GET /api/attempts` — собственная история с `page`, `pageSize`, фильтрами `kind`, `source`, `status`;
- `POST /api/attempts/{id}/questions/{questionId}/answer` — ответ на `AttemptQuestion`.

Тело ответа:

```json
{"selectedOptionPosition":2}
```

Ответ проверяется по историческому `OptionsSnapshot`. Повторный ответ, ответ после завершения и несуществующая позиция возвращают `409` или `400` согласно типу ошибки. Для неотвеченных вопросов правильность вариантов скрывается.

Тренировка завершается после всех вопросов. Экзамен содержит 10 вопросов, длится 1200 секунд, допускает две ошибки и завершается провалом на третьей. Просроченный экзамен получает `TimedOut`, `IsPassed=false`, а `FinishedAt` равен моменту истечения.

## История и статистика

### Кассир

- `GET /api/statistics/me`;
- `GET /api/statistics/me/activity?days=30`.

`days` находится в диапазоне 1..365. Активность использует UTC-дни, учитывает входы по `LoginEvent.CreatedAt`, тренировки и экзамены по `FinishedAt`.

### Администратор

- `GET /api/statistics/users?page=1&pageSize=20&groupId=1`;
- `GET /api/statistics/users/{userId}`;
- `GET /api/statistics/users/{userId}/activity?days=30`;
- `GET /api/statistics/users/{userId}/attempts`;
- `GET /api/statistics/users/{userId}/attempts/{attemptId}`;
- `GET /api/statistics/groups`;
- `GET /api/statistics/groups/{groupId}`.

Пагинация использует поля `items`, `page`, `pageSize`, `totalCount`, максимальный `pageSize` — 100.

Статистика учитывает все попытки в общем количестве, но ответы и итоговые результаты — только `Completed`, `Failed`, `TimedOut`. Формулы:

- `CorrectAnswerPercentage = CorrectAnswers / (CorrectAnswers + WrongAnswers) * 100`;
- `ExamPassPercentage = PassedExamCount / FinishedExamCount * 100`;
- `PreparedTicketsPercentage = CompletedPreparedTickets / TotalPreparedTickets * 100`.

При нулевом знаменателе результат равен `0`; проценты округляются до двух знаков. Группы используют текущий состав пользователей и исторический `Attempt.GroupIdSnapshot` для результатов.

## Frontend-подключение

При `pnpm dev` используйте относительные запросы `/api/...`; Vite proxy направит их на backend. Bearer-токен добавляется frontend в заголовок `Authorization`. Отдельная CORS-конфигурация backend не требуется для локальной разработки через proxy. Для production необходимо настроить reverse proxy или явно разрешить конкретный origin в конфигурации инфраструктуры.
