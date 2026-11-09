# Консольный парсер электронных ведомостей

Утилита автоматизирует процесс обработки электронных ведомостей учебной части. Программа извлекает неструктурированные данные (ФИО студентов, оценки, даты) из сырых текстовых файлов и экспортирует их в единый структурированный формат.

## 🛠 Технологии и требования
- **Язык:** C# / .NET 8.0 (или .NET 9.0)
- **Зависимости:**
  - `Newtonsoft.Json` или `System.Text.Json` (для работы с JSON)
  - `CsvHelper` (опционально, если будете использовать готовый инструмент для CSV)

## 🚀 Быстрый старт

### 1. Клонирование репозитория
```bash
git clone https://github.com
cd Console-based-parser-for-electronic-spreadsheets-statements
```

### 2. Сборка проекта
```bash
dotnet build
```

### 3. Запуск приложения
Запустите парсер, передав путь к файлу ведомости в качестве аргумента:
```bash
dotnet run -- path/to/statement.txt
```

## 📋 Использование и форматы данных

### Пример входных данных (сырой файл)
```text
Группа: ИВТ-21  Дата: 15.01.2026
Дисциплина: Программирование
-------------------------------------
1. Иванов Иван Иванович - Отлично
2. Петров Петр Петрович - Хорошо
-------------------------------------
Подпись декана: _________
```

### Пример структуры данных (Student.cs)
```csharp
public class Student
{
    public string Fullname { get; set; }
    public string Grade { get; set; }
    public string Date { get; set; }
}
```

## 👥 Авторы
- [JuggernautMGE](https://github.com/JuggernautMGE) - Lead Developer, QA/DevOps Engineer
- [dmmkk88](https://github.com/dmmkk88) - Product Owner, QA/DevOps Engineer

