# Ticketeer

Ticketeer — веб-приложение для обучения и проверки знаний билетных кассиров железнодорожных перевозчиков.

## Возможности

По требованиям система должна поддерживать обучение по тематическим вопросам и билетам, проведение экзаменов, сохранение результатов, просмотр личной и групповой статистики, а также администрирование пользователей, групп и учебных материалов.

Подробные согласованные требования приведены в [docs/requirements.md](docs/requirements.md), а фактические backend-маршруты — в [docs/api.md](docs/api.md).

## Текущее состояние

Проект находится на начальной стадии разработки.

- Frontend представлен базовым каркасом Vue-приложения: подключены Vue Router и Pinia, но прикладные страницы и маршруты ещё не реализованы.
- Backend реализует REST API, доменную модель, PostgreSQL/EF Core, JWT-аутентификацию, прохождение, каталоги, историю и статистику; актуальные маршруты описаны в [docs/api.md](docs/api.md).
- В корневом `compose.yaml` описан локальный экземпляр PostgreSQL.
- Frontend-прикладные страницы и административная панель пока не реализованы; backend-функциональность для них доступна через API.
- UnoCSS входит в согласованный технологический стек, но пока не подключён к frontend.

## Технологический стек

Frontend:

- Vue 3;
- TypeScript;
- Vite;
- Vue Router;
- Pinia;
- UnoCSS (запланирован, пока не подключён).

Backend:

- C#;
- ASP.NET Core;
- .NET 10;
- REST API;
- Entity Framework Core и Npgsql.

База данных:

- PostgreSQL.

## Структура репозитория

```text
Ticketeer/
├── backend/          # ASP.NET Core backend
├── frontend/         # Vue frontend
├── docs/             # Требования и проектная документация
├── compose.yaml      # Локальный PostgreSQL
└── README.md
```

## Требования к окружению

- .NET SDK 10;
- Node.js `^22.18.0` или `>=24.12.0` (согласно `frontend/package.json`);
- pnpm 11.27.0;
- PostgreSQL либо Docker с поддержкой Docker Compose.

## Локальный запуск

### База данных

Для запуска PostgreSQL через Docker Compose из корня репозитория:

```sh
docker compose up -d db
```

По умолчанию база данных доступна на `localhost:5432`. Compose задаёт локальные параметры подключения для базы `ticketeer`; они предназначены только для локального контейнера и не записываются в настройки приложения.

Строку подключения передавайте приложению через стандартную переменную конфигурации ASP.NET Core:

```sh
export ConnectionStrings__Default='Host=localhost;Port=5432;Database=ticketeer;Username=<local-user>;Password=<local-password>'
```

Замените placeholders значениями локального Compose из `compose.yaml`. Безопасный шаблон строки подключения также находится в `backend/appsettings.Development.example.json`. Не добавляйте реальные пароли в отслеживаемые файлы.

### Backend

Из корня репозитория:

```sh
dotnet restore backend/backend.csproj
dotnet run --project backend/backend.csproj
```

Профили разработки настроены на `http://localhost:5046` и `https://localhost:7277`. Backend запускается через `backend/Program.cs` и требует строки подключения и JWT-настроек. Полный перечень endpoint и ролей приведён в [docs/api.md](docs/api.md).

### Frontend

```sh
cd frontend
pnpm install
pnpm dev
```

Команда запускает Vite-сервер разработки. Адрес будет выведен в терминале.

Дополнительные проверки frontend:

```sh
pnpm type-check
pnpm lint
pnpm build
```

Команда `pnpm build` включает проверку типов через отдельный скрипт `type-check` и сборку Vite.
