public class VedomostConfig
{
// ключевые слова, по которым ищем колонку в шапке
public string TabKeyword { get; set; } = "таб";
public string NameKeyword { get; set; } = "фио";
public string PeriodKeyword { get; set; } = "период";
public string AccruedKeyword { get; set; } = "начислен";
public string WithheldKeyword { get; set; } = "удержан";
public string ToPayKeyword { get; set; } = "к выплате";

// строки с такими словами в первой колонке — пропускаем ("Итого", "Всего"...)
public string[] SkipKeywords { get; set; } = { "итог", "всего" };

// сколько строк сверху сканируем в поисках шапки
public int HeaderSearchDepth { get; set; } = 10;
}