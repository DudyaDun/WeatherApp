using System.Data;
using System.Globalization;
using ExcelDataReader;
using Newtonsoft.Json;
using WeatherApp.Models;

namespace WeatherApp.Data;

/// <summary>
/// Handles persistence (JSON file) and import (Excel) for weather data.
/// </summary>
public class WeatherDataManager
{
    // ── Storage path ──────────────────────────────────────────────────────────
    private static readonly string DataFilePath =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "WeatherApp",
            "weather_data.json");

    private WeatherDataStore _store = new();

    // ── Public Properties ─────────────────────────────────────────────────────

    public IReadOnlyList<WeatherLocation> Locations => _store.Locations;
    public IReadOnlyList<WeatherRecord>   Records   => _store.Records;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    public void Load()
    {
        if (!File.Exists(DataFilePath)) { _store = new WeatherDataStore(); return; }
        string json = File.ReadAllText(DataFilePath);
        _store = JsonConvert.DeserializeObject<WeatherDataStore>(json) ?? new WeatherDataStore();
    }

    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(DataFilePath)!);
        File.WriteAllText(DataFilePath, JsonConvert.SerializeObject(_store, Formatting.Indented));
    }

    // ── Location CRUD ─────────────────────────────────────────────────────────

    public void AddLocation(WeatherLocation loc)    { _store.Locations.Add(loc); Save(); }

    public void UpdateLocation(WeatherLocation updated)
    {
        int i = _store.Locations.FindIndex(l => l.Id == updated.Id);
        if (i >= 0) { _store.Locations[i] = updated; Save(); }
    }

    public void DeleteLocation(Guid id)
    {
        _store.Locations.RemoveAll(l => l.Id == id);
        _store.Records.RemoveAll(r => r.LocationId == id);
        Save();
    }

    // ── Record CRUD ───────────────────────────────────────────────────────────

    public void AddRecord(WeatherRecord rec)        { _store.Records.Add(rec); Save(); }

    public void UpdateRecord(WeatherRecord updated)
    {
        int i = _store.Records.FindIndex(r => r.Id == updated.Id);
        if (i >= 0) { _store.Records[i] = updated; Save(); }
    }

    public void DeleteRecord(Guid id)               { _store.Records.RemoveAll(r => r.Id == id); Save(); }

    public void DeleteRecords(IEnumerable<Guid> ids)
    {
        var set = ids.ToHashSet();
        _store.Records.RemoveAll(r => set.Contains(r.Id));
        Save();
    }

    // ── Query ─────────────────────────────────────────────────────────────────

    public List<WeatherRecord> GetRecords(
        Guid? locationId = null, DateTime? from = null, DateTime? to = null)
    {
        IEnumerable<WeatherRecord> q = _store.Records;
        if (locationId.HasValue) q = q.Where(r => r.LocationId == locationId.Value);
        if (from.HasValue)       q = q.Where(r => r.Timestamp >= from.Value);
        if (to.HasValue)         q = q.Where(r => r.Timestamp <= to.Value);
        return q.OrderBy(r => r.Timestamp).ToList();
    }

    // ── Excel Import ──────────────────────────────────────────────────────────

    /// <summary>
    /// Imports weather data from an Open Meteo-style Excel export.
    /// Reads cell values by native CLR type — not by ToString() — so it works
    /// regardless of the Windows locale (Russian comma vs. English dot).
    /// </summary>
    public int ImportFromExcel(string filePath)
    {
        using FileStream stream = File.Open(filePath, FileMode.Open, FileAccess.Read);
        using IExcelDataReader reader = ExcelReaderFactory.CreateReader(stream);

        DataSet ds = reader.AsDataSet(new ExcelDataSetConfiguration
        {
            ConfigureDataTable = _ => new ExcelDataTableConfiguration { UseHeaderRow = false }
        });

        if (ds.Tables.Count == 0)
            throw new InvalidDataException("Excel-файл не содержит листов.");

        DataTable sheet = ds.Tables[0];

        // ── Metadata rows ─────────────────────────────────────────────────────
        //   Row 0: location | Базель | …
        //   Row 1: lat      | 47.75  | …
        //   Row 2: lon      | 7.5    | …
        //   Row 3: asl      | 363.65 | …
        string locationName = CellStr(sheet, 0, 1);
        double lat = CellDbl(sheet, 1, 1);
        double lon = CellDbl(sheet, 2, 1);
        double asl = CellDbl(sheet, 3, 1);

        if (string.IsNullOrWhiteSpace(locationName))
            throw new InvalidDataException(
                "Не удалось прочитать название локации (строка 1, столбец 2).\n" +
                "Убедитесь, что файл экспортирован из Open Meteo.");

        // ── Detect variable columns from row 4 ───────────────────────────────
        //   Row 4: variable | Temperature | Relative Humidity | Cloud Cover Total
        int tempCol = -1, humCol = -1, cloudCol = -1;
        for (int c = 1; c < sheet.Columns.Count; c++)
        {
            string v = CellStr(sheet, 4, c).ToLowerInvariant();
            if      (v.Contains("temperature")) tempCol  = c;
            else if (v.Contains("humidity"))    humCol   = c;
            else if (v.Contains("cloud"))       cloudCol = c;
        }

        if (tempCol < 0 || humCol < 0 || cloudCol < 0)
            throw new InvalidDataException(
                "Не найдены обязательные колонки.\n" +
                $"Temperature: col {tempCol}, Humidity: col {humCol}, Cloud: col {cloudCol}\n\n" +
                "Проверьте, что строка 5 содержит: Temperature / Relative Humidity / Cloud Cover Total.");

        // ── Find first data row (scan rows 5–14 for a numeric timestamp) ─────
        int dataStartRow = -1;
        for (int r = 5; r < Math.Min(15, sheet.Rows.Count); r++)
        {
            if (IsNumericOrDate(sheet.Rows[r][0])) { dataStartRow = r; break; }
        }

        if (dataStartRow < 0)
            throw new InvalidDataException(
                "Не найдены строки с данными. " +
                "Ожидался числовой timestamp в первом столбце строк 6–15.");

        // ── Find or create location ───────────────────────────────────────────
        WeatherLocation? loc = _store.Locations
            .FirstOrDefault(l => l.Name.Equals(locationName, StringComparison.OrdinalIgnoreCase));

        if (loc == null)
        {
            loc = new WeatherLocation
            {
                Name        = locationName,
                Latitude    = lat,
                Longitude   = lon,
                AltitudeASL = asl,
                Resolution  = "hourly"
            };
            _store.Locations.Add(loc);
        }

        // ── Import data rows ──────────────────────────────────────────────────
        var excelEpoch = new DateTime(1899, 12, 30);

        var existing = _store.Records
            .Where(r => r.LocationId == loc.Id)
            .Select(r => r.Timestamp)
            .ToHashSet();

        int added = 0;

        for (int row = dataStartRow; row < sheet.Rows.Count; row++)
        {
            object? tsCell = sheet.Rows[row][0];
            if (tsCell == null || tsCell == DBNull.Value) continue;

            DateTime ts;
            if (tsCell is DateTime dtDirect)
            {
                ts = dtDirect;
            }
            else if (TryGetDouble(tsCell, out double oaDate) && oaDate > 1000)
            {
                // OLE Automation serial — days since 1899-12-30
                ts = excelEpoch.AddDays(oaDate);
            }
            else
            {
                string s = tsCell.ToString()!.Trim();
                if (!DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out ts) &&
                    !DateTime.TryParse(s, CultureInfo.CurrentCulture,   DateTimeStyles.None, out ts))
                    continue;
            }

            // Normalise to the minute — drops sub-minute floating-point drift
            ts = new DateTime(ts.Year, ts.Month, ts.Day, ts.Hour, ts.Minute, 0);

            if (existing.Contains(ts)) continue;

            _store.Records.Add(new WeatherRecord
            {
                LocationId       = loc.Id,
                Timestamp        = ts,
                Temperature      = CellDbl(sheet, row, tempCol),
                RelativeHumidity = CellDbl(sheet, row, humCol),
                CloudCoverTotal  = CellDbl(sheet, row, cloudCol)
            });
            existing.Add(ts);
            added++;
        }

        if (added > 0) Save();
        return added;
    }

    // ── CSV Export ────────────────────────────────────────────────────────────

    public void ExportToCsv(string filePath, IEnumerable<WeatherRecord> records)
    {
        var lines = new List<string>
            { "Дата и время;Температура (°C);Влажность (%);Облачность (%);Состояние неба" };

        foreach (var r in records)
            lines.Add(string.Join(";",
                r.Timestamp.ToString("dd.MM.yyyy HH:mm"),
                r.Temperature.ToString("F1"),
                r.RelativeHumidity.ToString("F1"),
                r.CloudCoverTotal.ToString("F1"),
                r.SkyCondition));

        File.WriteAllLines(filePath, lines, System.Text.Encoding.UTF8);
    }

    // ── Cell helpers (locale-safe) ────────────────────────────────────────────

    /// <summary>
    /// Returns the string value of a cell.
    /// Numeric types are formatted with InvariantCulture (dot-decimal)
    /// regardless of the Windows locale.
    /// </summary>
    private static string CellStr(DataTable t, int row, int col)
    {
        if (row >= t.Rows.Count || col >= t.Columns.Count) return string.Empty;
        object? v = t.Rows[row][col];
        if (v == null || v == DBNull.Value) return string.Empty;
        if (v is double  d)  return d.ToString(CultureInfo.InvariantCulture);
        if (v is float   f)  return f.ToString(CultureInfo.InvariantCulture);
        if (v is decimal dm) return dm.ToString(CultureInfo.InvariantCulture);
        return v.ToString()?.Trim() ?? string.Empty;
    }

    /// <summary>
    /// Returns the numeric value of a cell.
    /// Works whether ExcelDataReader stored it as double, int, string, etc.
    /// </summary>
    private static double CellDbl(DataTable t, int row, int col)
    {
        if (row >= t.Rows.Count || col >= t.Columns.Count) return 0;
        return TryGetDouble(t.Rows[row][col], out double d) ? Math.Round(d, 4) : 0;
    }

    /// <summary>
    /// Extracts a double from any object type ExcelDataReader may produce.
    /// Tries native type first, then string parsing with InvariantCulture
    /// and CurrentCulture so both dot-decimal and comma-decimal work.
    /// </summary>
    private static bool TryGetDouble(object? v, out double result)
    {
        result = 0;
        if (v == null || v == DBNull.Value) return false;
        if (v is double  d)  { result = d;          return true; }
        if (v is float   f)  { result = f;          return true; }
        if (v is int     i)  { result = i;          return true; }
        if (v is long    l)  { result = l;          return true; }
        if (v is decimal dm) { result = (double)dm; return true; }
        if (v is short   s)  { result = s;          return true; }
        string str = v.ToString()!.Trim();
        if (double.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out result)) return true;
        if (double.TryParse(str, NumberStyles.Any, CultureInfo.CurrentCulture,   out result)) return true;
        return false;
    }

    /// <summary>Returns true if the cell value is a number or DateTime.</summary>
    private static bool IsNumericOrDate(object? v)
    {
        if (v == null || v == DBNull.Value) return false;
        if (v is double or float or int or long or decimal or DateTime) return true;
        string s = v.ToString()!.Trim();
        return double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out _) ||
               double.TryParse(s, NumberStyles.Any, CultureInfo.CurrentCulture,   out _);
    }
}
