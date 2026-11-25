# DeployForge — Project Context

> Рабочая техническая документация проекта.
> Последнее обновление: 25 ноября 2026 года.

## 1. Назначение проекта

**DeployForge** — pet-проект: self-hosted платформа для автоматизации сборки и развертывания приложений.

Цель — создать собственную CI/CD-платформу, которая позволяет:
- подключать Git-репозитории;
- создавать проекты и pipeline;
- описывать последовательность шагов выполнения;
- запускать pipeline через фоновый Runner;
- выполнять команды в Docker-контейнерах;
- отслеживать состояние выполнения;
- в перспективе поддерживать приватные GitHub-репозитории.

Проект разрабатывается постепенно, с упором на понятную архитектуру, безопасность, тестирование и практическое изучение .NET.

## 2. Технологический стек

| Технология | Назначение |
|---|---|
| C# | Основной язык |
| .NET 10 | Платформа |
| ASP.NET Core Minimal API | HTTP API |
| Entity Framework Core 10 | ORM |
| PostgreSQL 17 | Основная БД |
| RabbitMQ 4 | Очередь задач |
| Docker | Изолированное выполнение pipeline |
| Docker.DotNet | Управление Docker из C# |
| xUnit | Тестирование |
| GitHub Actions | CI |
| Swagger / Swashbuckle | API-документация |
| Serilog | Логирование |

Среда разработки: macOS, Apple Silicon.

Установленный .NET SDK на момент проверки: 10.0.403.

## 3. Структура решения

Решение: `DeployForge.sln`.

### src/DeployForge.Domain

Доменный слой. Содержит бизнес-сущности и правила.

Основные сущности:
- `Project`
- `Pipeline`
- `PipelineStep`
- `PipelineRun`
- `RepositoryCredential`

Перечисления:
- `Enums/PipelineRunStatus.cs`

### src/DeployForge.Application

Сценарии использования и интерфейсы взаимодействия между слоями.

Основные направления:
- Projects
- Pipelines
- PipelineRuns

Содержит интерфейсы репозиториев, очереди и исполнителя pipeline.

### src/DeployForge.Infrastructure

Реализации инфраструктурных зависимостей:
- Persistence — Entity Framework Core и PostgreSQL;
- Messaging — RabbitMQ;
- Execution — Docker Executor.

Основные файлы:
- `Persistence/AppDbContext.cs`
- `Persistence/ProjectRepository.cs`
- `Persistence/PipelineRepository.cs`
- `Persistence/PipelineRunRepository.cs`
- `Messaging/RabbitMqPipelineRunQueue.cs`
- `Execution/DockerPipelineStepExecutor.cs`
- `Execution/DockerExecutorOptions.cs`

### src/DeployForge.Api

ASP.NET Core Minimal API.

Отвечает за HTTP-запросы и регистрацию зависимостей API.

Основные файлы:
- `Program.cs`
- `Endpoints/ProjectsEndpoints.cs`
- `Endpoints/PipelinesEndpoints.cs`

### src/DeployForge.Runner

Фоновое приложение, обрабатывающее задачи из RabbitMQ.

Основные файлы:
- `Program.cs`
- `Worker.cs`

### tests/DeployForge.UnitTests

Модульные тесты Domain и Application.

### tests/DeployForge.IntegrationTests

Интеграционные тесты API и работы с PostgreSQL.

### infrastructure

Содержит локальную инфраструктуру:

- `infrastructure/docker-compose.yml`

### .github

CI-конфигурация:

- `.github/workflows/ci.yml`

### docs

Техническая документация проекта.

## 4. Архитектура и выполнение pipeline

Основной сценарий работы:

1. Пользователь создаёт Project через API.
2. Для проекта создаётся Pipeline.
3. Pipeline содержит упорядоченные PipelineStep.
4. Пользователь запускает Pipeline.
5. Создаётся PipelineRun.
6. Запрос на выполнение публикуется в RabbitMQ.
7. Runner получает сообщение из очереди.
8. Runner загружает Pipeline и Project из PostgreSQL.
9. Runner вызывает `IPipelineStepExecutor`.
10. Docker Executor клонирует репозиторий и выполняет команды шагов.
11. Результат сохраняется в PipelineRun.

Текущая очередь RabbitMQ называется `pipeline-runs`.

Основной исполнитель:
`DockerPipelineStepExecutor`.

Интерфейс:
`IPipelineStepExecutor`.

## 5. Доменная модель

### Project

Содержит:
- Id;
- Name;
- RepositoryUrl;
- CreatedAt;
- Pipelines;
- RepositoryCredential (опциональная связь, добавлена в текущей незакоммиченной работе).

### Pipeline

Принадлежит Project.

Содержит:
- Id;
- ProjectId;
- Name;
- CreatedAt;
- Steps.

Поддерживает добавление шагов с проверкой уникальности Order.

### PipelineStep

Шаг pipeline с порядком выполнения и командой.

### PipelineRun

Экземпляр запуска pipeline.

Использует статус из `PipelineRunStatus`.

### RepositoryCredential

Сущность для зашифрованных учётных данных доступа к Git-репозиторию.

Текущие свойства:
- Id;
- ProjectId;
- Provider;
- EncryptedSecret;
- CreatedAt.

Есть метод `UpdateEncryptedSecret()`.

Связь: один Project — не более одной RepositoryCredential.

Секрет шифруется через ASP.NET Core Data Protection до сохранения в БД. API не возвращает ни исходное, ни зашифрованное значение.

## 6. База данных и миграции

Используется PostgreSQL 17.

EF Core DbContext:
`src/DeployForge.Infrastructure/Persistence/AppDbContext.cs`.

Обнаруженные миграции:

1. `20261107211542_InitialCreate`
2. `20261108164938_AddPipelines`
3. `20261108175230_AddPipelineRuns`
4. `20261125162443_AddRepositoryCredentials`

Миграции находятся в:
`src/DeployForge.Infrastructure/Persistence/Migrations/`.

Отношения:
- Project → Pipelines: один ко многим;
- Pipeline → PipelineSteps: один ко многим;
- Pipeline → PipelineRuns: один ко многим;
- Project → RepositoryCredential: один к одному, с уникальным индексом по ProjectId и каскадным удалением.

## 7. HTTP API

API работает локально по адресу:
`http://localhost:5148`.

Swagger включён в Development.

Известные маршруты:

- `POST /api/projects` — создание проекта;
- `GET /api/projects` — получение проектов;
- `GET /api/projects/{projectId}` — получение проекта по ID;
- `POST /api/projects/{projectId}/pipelines` — создание pipeline;
- `GET /api/projects/{projectId}/pipelines` — список pipeline;
- `GET /api/projects/{projectId}/pipelines/{pipelineId}` — получение pipeline;
- `DELETE /api/projects/{projectId}/pipelines/{pipelineId}` — удаление pipeline;
- `POST /api/projects/{projectId}/pipelines/{pipelineId}/runs` — запуск pipeline;
- GET-маршруты для получения списка запусков и отдельного запуска;
- `PUT /api/projects/{projectId}/repository-credential` — добавление или ротация токена;
- `GET /api/projects/{projectId}/repository-credential` — только метаданные credential;
- `DELETE /api/projects/{projectId}/repository-credential` — удаление credential.

## 8. Runner и Docker Executor

Runner — отдельное фоновое приложение.

Он:
- получает задания из RabbitMQ;
- загружает данные запуска из PostgreSQL;
- переводит PipelineRun в Running;
- вызывает исполнитель;
- сохраняет Success или Failed;
- подтверждает обработку сообщения RabbitMQ.

Docker Executor:
- проверяет URL репозитория;
- требует абсолютный HTTPS URL;
- запрещает UserInfo, query и fragment в URL;
- клонирует репозиторий через `git clone --depth 1`;
- выполняет команды шагов в порядке Order;
- ограничивает память, CPU и количество процессов;
- отключает Privileged;
- сбрасывает capabilities;
- включает `no-new-privileges`;
- удаляет контейнер после выполнения;
- использует таймаут выполнения.

Клонирование и выполнение разделены:
- доверенный clone-контейнер получает токен через переменную окружения и Git `GIT_ASKPASS`;
- токен не попадает в аргументы процесса и clone script;
- после клонирования clone-контейнер удаляется;
- исходный код передаётся через краткоживущий named Docker volume;
- pipeline-контейнер не получает токен;
- после запуска удаляются оба контейнера и volume.

## 9. Локальная инфраструктура

Docker Compose:
`infrastructure/docker-compose.yml`.

Используемые сервисы:
- PostgreSQL 17;
- RabbitMQ 4 Management.

Локальные переменные окружения хранятся в `.env`.

Шаблон:
`.env.example`.

Файл `.env` игнорируется Git. Его содержимое нельзя добавлять в документацию, коммиты или сообщения.

Проверка конфигурации Compose:

```bash
docker compose --env-file .env -f infrastructure/docker-compose.yml config --quiet
```

Запуск инфраструктуры:

```bash
docker compose --env-file .env -f infrastructure/docker-compose.yml up -d
```

## 10. Тестирование

Модульные тесты:
`tests/DeployForge.UnitTests/`.

Интеграционные тесты:
`tests/DeployForge.IntegrationTests/`.

Интеграционные тесты используют PostgreSQL.

`CustomWebApplicationFactory`:
- создаёт тестовый WebApplication;
- использует отдельную тестовую строку подключения;
- выполняет миграции;
- синхронизирует миграции через `SemaphoreSlim`, чтобы избежать конфликтов при параллельном запуске тестов;
- использует тестовую in-memory реализацию очереди вместо RabbitMQ;
- использует отдельное временное хранилище ключей Data Protection.

Последний локально подтверждённый результат:
- сборка Release: успешно, без предупреждений;
- 36 Unit Tests: успешно;
- Integration Tests должны быть подтверждены в GitHub Actions: локальный запуск заблокирован ограничением диска текущего облачного окружения.

## 11. CI — GitHub Actions

Файл:
`.github/workflows/ci.yml`.

Workflow называется `DeployForge CI`.

Запускается при:
- push в main;
- pull request в main.

Используется `ubuntu-latest`.

В CI поднимаются:
- PostgreSQL 17;
- RabbitMQ 4 Management.

Шаги:
1. Checkout repository.
2. Setup .NET 10.
3. Restore dependencies.
4. Build solution в Release.
5. Run unit tests.
6. Run integration tests.

Последний известный успешный GitHub Actions run:
`42695223478`.

В нём успешно завершились сборка, модульные и интеграционные тесты.

Известные предупреждения:
- предупреждение о будущем отказе от Node.js 20 в GitHub Actions;
- предупреждение о переходе `ubuntu-latest` на Ubuntu 26.

На момент последней проверки эти предупреждения не блокировали CI.

## 12. Поддержка приватных GitHub-репозиториев

Реализовано:

1. Domain-сущность и EF Core связь RepositoryCredential.
2. Шифрование секрета через ASP.NET Core Data Protection.
3. Общее постоянное хранилище ключей для API и Runner.
4. Repository и Application use cases для credentials.
5. API добавления, ротации, просмотра метаданных и удаления токена.
6. Отдельный доверенный clone-контейнер и передача кода через Docker volume.
7. Unit и Integration тесты.
8. EF Core migration `AddRepositoryCredentials`.

Для GitHub предпочтителен Fine-grained Personal Access Token с минимальными правами: доступ только к нужному репозиторию и Contents: Read-only.

Токен не должен:
- попадать в Git;
- попадать в логи;
- находиться в URL репозитория;
- передаваться пользовательским pipeline-командам.

Реальный токен в репозиторий и тестовые данные не добавлялся.

## 13. Известный технический долг

Не является частью текущей задачи, но требует внимания в будущем:

- надёжное хранение и ротация ключей Data Protection;
- обработка зависших PipelineRun при аварийном завершении Runner;
- согласованность сохранения PipelineRun и публикации сообщения RabbitMQ;
- Outbox Pattern для надёжной публикации событий;
- улучшение конфигурации секретов и подключения RabbitMQ;
- проверка совместимости версий NuGet-пакетов;
- обновление предупреждений GitHub Actions.

## 14. Правила разработки

Работа над DeployForge ведётся небольшими последовательными шагами.

Договорённости:

1. Перед изменением изучаем существующий код.
2. Не переписываем архитектуру без необходимости.
3. Каждый шаг сопровождается объяснением.
4. Для файлов всегда указываем полный путь от корня проекта.
5. Не предполагаем, что пользователь выполнил команду, пока не увидели результат.
6. Не просим присылать пароли, токены и другие секреты.
7. Не выполняем сборку, тесты, миграции и другие команды без объяснения и согласования этапа.
8. Не выполняем commit или push без явного согласия пользователя.
9. После крупного этапа обновляем этот документ.
10. Перед добавлением новой функциональности учитываем безопасность.

Среда разработки:
- macOS;
- Apple Silicon;
- Terminal;
- Vim.

## 15. Текущее состояние Git

Работа над credentials и приватным клонированием завершена в исходниках. Перед слиянием требуется зелёный GitHub Actions run.

## 16. Ближайшие шаги

Текущий этап: стабилизация и эксплуатационная безопасность.

Ближайшая последовательность:

1. Добавить аутентификацию и авторизацию API.
2. Внедрить Outbox Pattern для атомарной публикации запусков.
3. Обработать зависшие PipelineRun после аварий Runner.
4. Настроить production-хранилище и ротацию Data Protection keys.
5. Добавить end-to-end тест запуска приватного тестового репозитория без реальных пользовательских токенов.

---

**Документ является рабочим контекстом проекта.** При изменении архитектуры, API, базы данных, CI или статуса реализации его необходимо актуализировать.
