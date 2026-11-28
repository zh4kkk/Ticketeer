# Ticketeer

Ticketeer — веб-приложение для обучения и проверки знаний билетных кассиров железнодорожных перевозчиков.

## Возможности

По требованиям система должна поддерживать обучение по тематическим вопросам и билетам, проведение экзаменов, сохранение результатов, просмотр личной и групповой статистики, а также администрирование пользователей, групп и учебных материалов.

Подробные согласованные требования приведены в [docs/requirements.md](docs/requirements.md), а фактические backend-маршруты — в [docs/api.md](docs/api.md).

## Текущее состояние

Проект находится на начальной стадии разработки.

- Frontend содержит базовый прикладной фундамент: типизированный Fetch-клиент с безопасными API-URL и обработкой 401, вход для обеих ролей, одноразовое восстановление JWT-сессии с повторной проверкой после временного сбоя, защищённые маршруты, layouts и стартовые страницы кассира и администратора. Для кассира доступны каталоги тем, подготовленных билетов, экзаменов и вопросов с серверным поиском, фильтрами, пагинацией и подробными страницами.
- Backend реализует REST API, доменную модель, PostgreSQL/EF Core, JWT-аутентификацию, прохождение, каталоги, историю и статистику; актуальные маршруты описаны в [docs/api.md](docs/api.md).
- В корневом `compose.yaml` описан локальный экземпляр PostgreSQL.
- Прохождение билетов и экзаменов, статистика, профиль и административный CRUD будут добавляться отдельными этапами; текущий каталог не запускает попытки и не раскрывает правильные ответы.
- UnoCSS подключён через `uno.config.ts` и Vite plugin.
- Интерфейс использует согласованную палитру РЖД: основной красный `#E21A1A`, тёмный красный `#C4151C`, дополнительный синий `#0066A1` и графитовый `#3F4443`.

## Технологический стек

Frontend:

- Vue 3;
- TypeScript;
- Vite;
- Vue Router;
- Pinia;
- UnoCSS.

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

Создайте локальную конфигурацию из безопасного шаблона. Файл `backend/.env` игнорируется Git и должен иметь ограниченные права доступа:

```sh
cp backend/.env.example backend/.env
chmod 600 backend/.env
```

Заполните `Jwt__Key` случайным ключом длиной не менее 32 байт и задайте локальные значения bootstrap-администратора. Реальные секреты не добавляйте в Git.

Запустите миграции и backend из корня репозитория:

```sh
dotnet restore backend/backend.csproj
dotnet tool restore --tool-manifest backend/dotnet-tools.json
set -a; source backend/.env; set +a
cd backend
dotnet ef database update --project backend.csproj
cd ..
set -a; source backend/.env; set +a
ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS=http://localhost:5046 dotnet run --project backend/backend.csproj --no-launch-profile
```

Профили разработки настроены на `http://localhost:5046` и `https://localhost:7277`. Backend запускается через `backend/Program.cs` и требует строки подключения и JWT-настроек. Полный перечень endpoint и ролей приведён в [docs/api.md](docs/api.md).

После запуска проверьте backend:

```sh
curl http://localhost:5046/health
```

Ожидаемый ответ: `{"status":"ok","database":"ok"}`. Bootstrap-администратор создаётся только если в базе ещё нет администратора. Для остановки backend используйте `Ctrl+C`, а PostgreSQL остановите без удаления данных:

```sh
docker compose stop db
```

### Frontend

```sh
cd frontend
pnpm install
pnpm dev --host 127.0.0.1
```

Команда запускает Vite-сервер разработки; обычно приложение доступно по адресу `http://127.0.0.1:5173`. Запросы `/api/...` проходят через сохранённый Vite proxy к backend на `http://localhost:5046`. Для остановки frontend используйте `Ctrl+C`.

Дополнительные проверки frontend:

```sh
pnpm type-check
pnpm lint
pnpm build
```

Команда `pnpm build` включает проверку типов через отдельный скрипт `type-check` и сборку Vite.

В cashier-интерфейсе доступны запуск подготовленных и случайных тренировок, запуск подготовленных и случайных экзаменов, прохождение вопросов с серверной проверкой ответов, список незавершённых попыток с продолжением по URL, информационный таймер экзамена и страница результата. Backend остаётся источником истины для статуса, правильности ответов и итоговой оценки; полноценные профиль, история и статистика относятся к следующим этапам.

В разработке frontend использует Vite proxy: запросы к относительным адресам `/api/...` направляются на backend `http://localhost:5046` (цель можно изменить через `VITE_API_PROXY_TARGET`). Для production необходимо отдельно настроить reverse proxy веб-сервера так, чтобы `/api` обслуживался backend; frontend не передаёт JWT на сторонние адреса.
