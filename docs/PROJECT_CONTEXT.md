# DeployForge — Project Context

> Рабочая техническая документация проекта.
> Последнее обновление: 11 ноября 2026 года.

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

Новая сущность для учётных данных доступа к Git-репозиторию.

Текущие свойства:
- Id;
- ProjectId;
- Provider;
- EncryptedSecret;
- CreatedAt.

Есть метод `UpdateEncryptedSecret()`.

Предполагаемая связь: один Project — не более одной RepositoryCredential.

**Важно:** сущность и связь пока находятся в незавершённой разработке. Шифрование, API, миграция и использование токена Runner ещё не реализованы.

## 6. База данных и миграции

Используется PostgreSQL 17.

EF Core DbContext:
`src/DeployForge.Infrastructure/Persistence/AppDbContext.cs`.

Обнаруженные миграции:

1. `20261107211542_InitialCreate`
2. `20261108164938_AddPipelines`
3. `20261108175230_AddPipelineRuns`

Миграции находятся в:
`src/DeployForge.Infrastructure/Persistence/Migrations/`.

Отношения:
- Project → Pipelines: один ко многим;
- Pipeline → PipelineSteps: один ко многим;
- Pipeline → PipelineRuns: один ко многим;
- Project → RepositoryCredential: планируемое отношение один к одному, конфигурация уже добавлена в незакоммиченных изменениях.

Миграция для RepositoryCredential пока не создана.

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
- GET-маршруты для получения списка запусков и отдельного запуска.

Маршруты управления RepositoryCredential ещё не реализованы.

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

**Ограничение безопасности:** сейчас клонирование репозитория и выполнение пользовательских команд происходят в одном контейнере. Это означает, что пользовательские команды потенциально могут получить доступ к данным, доступным этому контейнеру.

GitHub-токен нельзя передавать в этот контейнер через переменную окружения, аргумент команды или смонтированный файл.

Для поддержки приватных репозиториев необходимо отделить доверенную операцию клонирования от выполнения пользовательских команд.

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
- синхронизирует миграции через `SemaphoreSlim`, чтобы избежать конфликтов при параллельном запуске тестов.

Последний подтверждённый результат тестирования:
- 25 Unit Tests;
- 16 Integration Tests;
- всего 41 тест — успешно.

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

## 12. Текущая задача — поддержка приватных GitHub-репозиториев

Цель: разрешить DeployForge клонировать приватные GitHub-репозитории.

Предварительный план:

1. Завершить Domain-сущность RepositoryCredential.
2. Настроить связь с Project в EF Core.
3. Добавить шифрование секрета через ASP.NET Core Data Protection.
4. Настроить общее постоянное хранилище ключей для API и Runner.
5. Реализовать сервис шифрования в Infrastructure.
6. Добавить репозиторий для учётных данных.
7. Реализовать API добавления и обновления токена.
8. Не возвращать исходный или зашифрованный токен в публичных API-ответах.
9. Реализовать безопасное клонирование приватного репозитория в отдельном доверенном окружении.
10. Передавать исходный код в окружение выполнения pipeline без передачи туда GitHub-токена.
11. Добавить тесты.
12. Создать EF Core migration.
13. Проверить сборку и полный набор тестов.
14. Проверить CI.

Для GitHub предпочтителен Fine-grained Personal Access Token с минимальными правами: доступ только к нужному репозиторию и Contents: Read-only.

Токен не должен:
- попадать в Git;
- попадать в логи;
- находиться в URL репозитория;
- передаваться пользовательским pipeline-командам.

Токен пока не создавался и не добавлялся в проект.

## 13. Известный технический долг

Не является частью текущей задачи, но требует внимания в будущем:

- разделение доверенного клонирования и исполнения пользовательских команд;
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

На момент последней проверки:

Незакоммиченные изменения:

- `src/DeployForge.Domain/Entities/Project.cs` — изменён;
- `src/DeployForge.Infrastructure/Persistence/AppDbContext.cs` — изменён;
- `src/DeployForge.Domain/Entities/RepositoryCredential.cs` — новый файл.

Эти изменения относятся к текущей работе над RepositoryCredential.

Их нельзя терять или откатывать без согласования.

На этом этапе не выполнять commit и push.

## 16. Ближайшие шаги

Текущий этап: подготовка поддержки приватных GitHub-репозиториев.

Ближайшая последовательность:

1. Проверить конфигурацию Docker Compose и настройки API/Runner.
2. Выбрать безопасное общее хранилище ключей Data Protection.
3. Настроить шифрование и расшифрование в Infrastructure.
4. Добавить тесты сервиса шифрования.
5. Настроить хранение RepositoryCredential в PostgreSQL.
6. Реализовать API управления credentials.
7. Спроектировать и реализовать безопасное клонирование.
8. Добавить интеграционные тесты.
9. Проверить сборку, тесты и CI.
10. После подтверждения пользователя выполнить commit и push.

---

**Документ является рабочим контекстом проекта.** При изменении архитектуры, API, базы данных, CI или статуса реализации его необходимо актуализировать.
