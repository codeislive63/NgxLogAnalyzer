
# NgxLogAnalyzer — Консольный анализатор логов NGINX

## Описание проекта

**NgxLogAnalyzer** — консольная утилита для анализа логов NGINX. Она обрабатывает локальные и удалённые источники, агрегирует статистику и формирует отчёты в форматах:

- **JSON**
- **Markdown**
- **AsciiDoc**

---

## Возможности

### Поддерживаемые источники логов
- Локальные файлы (`.txt`, `.log`);
- Удалённые HTTP/HTTPS-ресурсы;
- Путь с glob-паттернами:
  - `*` — одиночная маска.
  - `**` — рекурсивный поиск по директориям.

---

## Собираемая статистика

- Количество запросов'
- Средний, максимальный и **95‑й перцентиль** размера ответа'
- Топ запрашиваемых ресурсов'
- Частота HTTP-кодов'
- Запросы по датам и **дням недели**'
- Список **уникальных протоколов** (HTTP/1.1, HTTP/2, grpc и др.).

---

## ⚙ Параметры CLI

| Параметр | Описание | Обязателен |
|---------|----------|------------|
| `-p, --path` | Путь или glob-паттерн | Да |
| `-f, --format` | Формат отчёта (`json`, `markdown`, `adoc`) | Да |
| `-o, --output` | Путь к файлу результата | Да |
| `--from` | Начальная дата фильтрации | Нет |
| `--to` | Конечная дата фильтрации | Нет |

---

## Примеры запуска

### JSON‑отчёт
```bash
dotnet run --project ./src/LogsApp/Logs.csproj -- -p "scripts/data/input/logs/**/*.txt" -f json -o "report.json"
```

### Markdown‑отчёт
```bash
dotnet run --project ./src/LogsApp/Logs.csproj -- -p "scripts/data/input/logs/**/*.txt" -f markdown -o "report.md"
```

### AsciiDoc‑отчёт
```bash
dotnet run --project ./src/LogsApp/Logs.csproj -- -p "scripts/data/input/logs/**/*.txt" -f adoc -o "report.adoc"
```

---

## Тестирование

В проекте присутствуют следующие группы тестов:

### Application
- `ArgumentsParserTests`
- `OutputPathValidatorTests`

### Domain
- `LogStatsAggregatorTests`
- `NginxLogLineParserTests`

### Formatters
- `ReportFormatterTests`

### Infrastructure
- `GlobResolverTests`
- `LogSourceReaderTests` (в т.ч. тесты HTTP и отмены `CancellationToken`)

### Запуск тестов

```bash
dotnet test src/LogsApp.Test/Logs.Test.csproj
```
