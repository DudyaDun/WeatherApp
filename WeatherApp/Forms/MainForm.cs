using System.Globalization;
using WeatherApp.Data;
using WeatherApp.Models;

namespace WeatherApp.Forms;

/// <summary>
/// Main application window: tabular data display, filtering, CRUD.
/// </summary>
public class MainForm : Form
{
    // ── Services ──────────────────────────────────────────────────────────────
    private readonly WeatherDataManager _mgr = new();

    // ── State ─────────────────────────────────────────────────────────────────
    private WeatherLocation? _currentLocation;
    private List<WeatherRecord> _displayedRecords = new();

    // ── Controls ──────────────────────────────────────────────────────────────
    private MenuStrip       _menuStrip     = null!;
    private ToolStrip       _toolStrip     = null!;
    private Panel           _locationPanel = null!;
    private ComboBox        _cmbLocation   = null!;
    private Panel           _filterPanel   = null!;
    private DateTimePicker  _dtpFrom       = null!;
    private DateTimePicker  _dtpTo         = null!;
    private DataGridView    _grid          = null!;
    private StatusStrip     _status        = null!;
    private ToolStripStatusLabel _lblStatus = null!;
    private ToolStripStatusLabel _lblCount  = null!;

    // ── Constructor ───────────────────────────────────────────────────────────
    public MainForm()
    {
        BuildUi();
        _mgr.Load();
        RefreshLocationCombo();

        if (_mgr.Locations.Count == 0)
            ShowWelcomeHint();
    }

    // ── UI Construction ───────────────────────────────────────────────────────
    private void BuildUi()
    {
        Text            = "Метеоданные";
        Size            = new Size(1100, 700);
        MinimumSize     = new Size(760, 500);
        StartPosition   = FormStartPosition.CenterScreen;
        Font            = new Font("Segoe UI", 9f);

        BuildMenu();
        BuildToolStrip();
        BuildStatus();   // Bottom — до Fill

        // Единый контейнер Fill — внутри TableLayoutPanel с явными высотами строк.
        // Это надёжнее, чем несколько DockStyle.Top непосредственно на форме.
        var mainTable = new TableLayoutPanel
        {
            Dock        = DockStyle.Fill,
            ColumnCount = 1,
            RowCount    = 3,
            Padding     = Padding.Empty,
            Margin      = Padding.Empty,
            CellBorderStyle = TableLayoutPanelCellBorderStyle.None
        };
        mainTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        mainTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));   // локация
        mainTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));   // фильтр
        mainTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100));   // таблица
        Controls.Add(mainTable);

        BuildLocationPanel(mainTable);
        BuildFilterPanel(mainTable);
        BuildGrid(mainTable);
    }

    private void BuildMenu()
    {
        _menuStrip = new MenuStrip();

        // Файл
        var miFile   = new ToolStripMenuItem("Файл");
        var miImport = new ToolStripMenuItem("Импорт из Excel…",  null, OnImportExcel)  { ShortcutKeys = Keys.Control | Keys.O };
        var miExport = new ToolStripMenuItem("Экспорт в CSV…",   null, OnExportCsv)    { ShortcutKeys = Keys.Control | Keys.E };
        var miSep    = new ToolStripSeparator();
        var miExit   = new ToolStripMenuItem("Выход",            null, (_, _) => Close()) { ShortcutKeys = Keys.Alt | Keys.F4 };
        miFile.DropDownItems.AddRange(new ToolStripItem[] { miImport, miExport, miSep, miExit });

        // Данные
        var miData   = new ToolStripMenuItem("Данные");
        var miAdd    = new ToolStripMenuItem("Добавить запись…",    null, OnAddRecord)    { ShortcutKeys = Keys.Insert };
        var miEdit   = new ToolStripMenuItem("Редактировать…",      null, OnEditRecord)   { ShortcutKeys = Keys.F2 };
        var miDel    = new ToolStripMenuItem("Удалить выбранные",   null, OnDeleteRecords) { ShortcutKeys = Keys.Delete };
        var miSep2   = new ToolStripSeparator();
        var miAddLoc = new ToolStripMenuItem("Новая локация…",      null, OnAddLocation);
        var miDelLoc = new ToolStripMenuItem("Удалить локацию",     null, OnDeleteLocation);
        miData.DropDownItems.AddRange(new ToolStripItem[]
            { miAdd, miEdit, miDel, miSep2, miAddLoc, miDelLoc });

        // Вид
        var miView   = new ToolStripMenuItem("Вид");
        var miReset  = new ToolStripMenuItem("Сбросить фильтр",     null, OnResetFilter) { ShortcutKeys = Keys.Control | Keys.R };
        var miCharts = new ToolStripMenuItem("Графики…",             null, OnShowCharts)  { ShortcutKeys = Keys.Control | Keys.G };
        miView.DropDownItems.Add(miReset);
        miView.DropDownItems.Add(miCharts);

        _menuStrip.Items.AddRange(new ToolStripItem[] { miFile, miData, miView });
        MainMenuStrip = _menuStrip;
        Controls.Add(_menuStrip);
    }

    private void BuildToolStrip()
    {
        _toolStrip = new ToolStrip { GripStyle = ToolStripGripStyle.Hidden, Padding = new Padding(4, 2, 0, 2) };

        ToolStripButton Btn(string text, string tip, EventHandler handler, Color? bg = null)
        {
            var b = new ToolStripButton(text) { ToolTipText = tip, DisplayStyle = ToolStripItemDisplayStyle.Text };
            if (bg.HasValue) b.BackColor = bg.Value;
            b.Click += handler;
            return b;
        }

        _toolStrip.Items.Add(Btn("📂 Импорт",  "Импортировать данные из Excel",     OnImportExcel));
        _toolStrip.Items.Add(Btn("💾 Экспорт", "Экспортировать в CSV",              OnExportCsv));
        _toolStrip.Items.Add(new ToolStripSeparator());
        _toolStrip.Items.Add(Btn("➕ Добавить",  "Добавить новую запись (Insert)",    OnAddRecord));
        _toolStrip.Items.Add(Btn("✏ Изменить",  "Редактировать выбранную запись (F2)", OnEditRecord));
        _toolStrip.Items.Add(Btn("🗑 Удалить",  "Удалить выбранные записи (Del)",    OnDeleteRecords, Color.FromArgb(255, 230, 230)));
        _toolStrip.Items.Add(new ToolStripSeparator());
        _toolStrip.Items.Add(Btn("🔄 Обновить", "Обновить данные",                   (_, _) => RefreshGrid()));
        _toolStrip.Items.Add(new ToolStripSeparator());
        _toolStrip.Items.Add(Btn("📈 Графики",  "Просмотр графиков (Ctrl+G)",           OnShowCharts, Color.FromArgb(230, 245, 230)));

        Controls.Add(_toolStrip);
    }

    private void BuildLocationPanel(TableLayoutPanel table)
    {
        _locationPanel = new Panel
        {
            Dock      = DockStyle.Fill,
            BackColor = Color.FromArgb(235, 244, 255),
            Padding   = new Padding(8, 4, 8, 4)
        };

        var inner = new TableLayoutPanel
        {
            Dock        = DockStyle.Fill,
            ColumnCount = 3,
            RowCount    = 1
        };
        inner.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
        inner.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        inner.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));

        inner.Controls.Add(new Label
        {
            Text      = "Локация:",
            TextAlign = ContentAlignment.MiddleRight,
            Dock      = DockStyle.Fill
        }, 0, 0);

        _cmbLocation               = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        _cmbLocation.SelectedIndexChanged += (_, _) => OnLocationChanged();
        inner.Controls.Add(_cmbLocation, 1, 0);

        var btnEdit = new Button
        {
            Text      = "⚙ Изменить локацию",
            Dock      = DockStyle.Fill,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(0, 120, 215),
            ForeColor = Color.White,
            Cursor    = Cursors.Hand
        };
        btnEdit.FlatAppearance.BorderSize = 0;
        btnEdit.Click += OnEditLocation;
        inner.Controls.Add(btnEdit, 2, 0);

        _locationPanel.Controls.Add(inner);
        table.Controls.Add(_locationPanel, 0, 0);
    }

    private void BuildFilterPanel(TableLayoutPanel table)
    {
        _filterPanel = new Panel
        {
            Dock      = DockStyle.Fill,
            BackColor = Color.FromArgb(245, 245, 245),
            Padding   = new Padding(8, 4, 8, 4)
        };

        var inner = new FlowLayoutPanel
        {
            Dock          = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight
        };

        inner.Controls.Add(new Label { Text = "С:", TextAlign = ContentAlignment.MiddleLeft, AutoSize = false, Size = new Size(24, 26) });

        _dtpFrom        = new DateTimePicker { Format = DateTimePickerFormat.Short, Size = new Size(110, 26) };
        _dtpFrom.Value  = new DateTime(2000, 1, 1);
        inner.Controls.Add(_dtpFrom);

        inner.Controls.Add(new Label { Text = "  По:", TextAlign = ContentAlignment.MiddleLeft, AutoSize = false, Size = new Size(30, 26) });

        _dtpTo       = new DateTimePicker { Format = DateTimePickerFormat.Short, Size = new Size(110, 26) };
        _dtpTo.Value = new DateTime(2100, 12, 31);
        inner.Controls.Add(_dtpTo);

        var btnFilter = new Button
        {
            Text      = "Применить",
            Size      = new Size(90, 26),
            FlatStyle = FlatStyle.Flat
        };
        btnFilter.Click += (_, _) => RefreshGrid();
        inner.Controls.Add(btnFilter);

        var btnReset = new Button
        {
            Text      = "Сбросить",
            Size      = new Size(90, 26),
            FlatStyle = FlatStyle.Flat
        };
        btnReset.Click += OnResetFilter;
        inner.Controls.Add(btnReset);

        _filterPanel.Controls.Add(inner);
        table.Controls.Add(_filterPanel, 0, 1);
    }

    private void BuildGrid(TableLayoutPanel table)
    {
        _grid = new DataGridView
        {
            Dock                  = DockStyle.Fill,
            AllowUserToAddRows    = false,
            AllowUserToDeleteRows = false,
            ReadOnly              = true,
            SelectionMode         = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect           = true,
            AutoSizeColumnsMode   = DataGridViewAutoSizeColumnsMode.Fill,
            RowHeadersVisible     = false,
            AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(245, 248, 255)
            },
            ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                Alignment = DataGridViewContentAlignment.MiddleCenter
            },
            EnableHeadersVisualStyles = false,
            BorderStyle               = BorderStyle.None,
            AllowUserToResizeRows     = false
        };

        // Double-click to edit
        _grid.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) OnEditRecord(this, EventArgs.Empty); };

        // Numeric + date sort
        _grid.SortCompare += OnGridSortCompare;

        // Context menu
        var ctx = new ContextMenuStrip();
        ctx.Items.Add("Добавить запись").Click    += OnAddRecord;
        ctx.Items.Add("Редактировать").Click      += OnEditRecord;
        ctx.Items.Add(new ToolStripSeparator());
        ctx.Items.Add("Удалить выбранные").Click  += OnDeleteRecords;
        _grid.ContextMenuStrip = ctx;

        table.Controls.Add(_grid, 0, 2);
    }

    private void BuildStatus()
    {
        _status         = new StatusStrip();
        _lblStatus      = new ToolStripStatusLabel("Готово") { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
        _lblCount       = new ToolStripStatusLabel("0 записей");
        _status.Items.AddRange(new ToolStripItem[] { _lblStatus, _lblCount });
        Controls.Add(_status);
    }

    // ── Data Loading ──────────────────────────────────────────────────────────

    private void RefreshLocationCombo()
    {
        var prev = _currentLocation?.Id;
        _cmbLocation.Items.Clear();

        foreach (var loc in _mgr.Locations)
            _cmbLocation.Items.Add(loc);

        if (_mgr.Locations.Count > 0)
        {
            int idx = prev.HasValue
                ? _mgr.Locations.ToList().FindIndex(l => l.Id == prev.Value)
                : 0;
            _cmbLocation.SelectedIndex = Math.Max(0, idx);
        }
        else
        {
            _currentLocation = null;
            UpdateLocationInfo();
            _grid.Rows.Clear();
            _grid.Columns.Clear();
            SetStatus("Нет данных. Импортируйте Excel-файл или добавьте локацию.", 0);
        }
    }

    private void AdjustFilterToData()
    {
        if (_currentLocation == null) return;
        var all = _mgr.GetRecords(_currentLocation.Id);
        if (all.Count == 0) return;
        _dtpFrom.Value = all.Min(r => r.Timestamp).Date;
        _dtpTo.Value   = all.Max(r => r.Timestamp).Date.AddDays(1);
    }

    private void OnLocationChanged()
    {
        _currentLocation = _cmbLocation.SelectedItem as WeatherLocation;
        UpdateLocationInfo();
        AdjustFilterToData();
        RefreshGrid();
    }

    private void UpdateLocationInfo()
    {
        Text = _currentLocation != null
            ? $"Метеоданные — {_currentLocation.Name}"
            : "Метеоданные";
    }

    private void RefreshGrid()
    {
        if (_currentLocation == null) return;

        _displayedRecords = _mgr.GetRecords(
            locationId: _currentLocation.Id,
            from:       _dtpFrom.Value.Date,
            to:         _dtpTo.Value.Date.AddDays(1).AddSeconds(-1));

        _grid.SuspendLayout();
        _grid.Columns.Clear();
        _grid.Rows.Clear();

        // Define columns
        AddCol("Дата",            120, "dd.MM.yyyy");
        AddCol("Время",           70,  "HH:mm");
        AddCol("Темп. (°C)",      100, "F1");
        AddCol("Влажность (%)",   110, "F1");
        AddCol("Облачность (%)",  110, "F1");
        AddCol("Небо",            140, null);
        AddCol("Комфорт",         110, null);
        AddCol("ID",              0,   null);   // hidden

        _grid.Columns["ID"]!.Visible = false;

        foreach (var r in _displayedRecords)
        {
            _grid.Rows.Add(
                r.Timestamp.ToString("dd.MM.yyyy"),
                r.Timestamp.ToString("HH:mm"),
                r.Temperature.ToString("F1"),
                r.RelativeHumidity.ToString("F1"),
                r.CloudCoverTotal.ToString("F1"),
                r.SkyCondition,
                r.ComfortIndex,
                r.Id.ToString());
        }

        // Colour code temperature column
        foreach (DataGridViewRow row in _grid.Rows)
        {
            if (double.TryParse(row.Cells["Темп. (°C)"]?.Value?.ToString(), out double t))
            {
                row.Cells["Темп. (°C)"].Style.BackColor = t switch
                {
                    < 0  => Color.FromArgb(200, 225, 255),
                    < 10 => Color.FromArgb(210, 240, 255),
                    < 20 => Color.FromArgb(220, 255, 220),
                    < 28 => Color.FromArgb(255, 255, 200),
                    _    => Color.FromArgb(255, 220, 200)
                };
            }
        }

        _grid.ResumeLayout();
        SetStatus($"Локация: {_currentLocation.Name}", _displayedRecords.Count);
    }

    private void AddCol(string name, int width, string? fmt)
    {
        var col = new DataGridViewTextBoxColumn
        {
            Name             = name,
            HeaderText       = name,
            FillWeight       = width == 0 ? 1 : width,
            MinimumWidth     = width == 0 ? 2 : 60,
            DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
        };
        if (width == 0) col.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
        _grid.Columns.Add(col);
    }

    // ── CRUD Handlers ─────────────────────────────────────────────────────────

    private void OnAddRecord(object? sender, EventArgs e)
    {
        if (_currentLocation == null)
        {
            MessageBox.Show("Сначала выберите или создайте локацию.",
                "Добавление", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dlg = new EditRecordForm(_currentLocation.Id);
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            _mgr.AddRecord(dlg.Result);
            RefreshGrid();
            SetStatus($"Запись добавлена: {dlg.Result.Timestamp:dd.MM.yyyy HH:mm}",
                _displayedRecords.Count);
        }
    }

    private void OnEditRecord(object? sender, EventArgs e)
    {
        if (_grid.SelectedRows.Count == 0) return;

        string? idStr = _grid.SelectedRows[0].Cells["ID"].Value?.ToString();
        if (!Guid.TryParse(idStr, out Guid id)) return;

        var rec = _mgr.Records.FirstOrDefault(r => r.Id == id);
        if (rec == null) return;

        using var dlg = new EditRecordForm(rec.LocationId, rec);
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            _mgr.UpdateRecord(dlg.Result);
            RefreshGrid();
            SetStatus($"Запись обновлена: {dlg.Result.Timestamp:dd.MM.yyyy HH:mm}",
                _displayedRecords.Count);
        }
    }

    private void OnDeleteRecords(object? sender, EventArgs e)
    {
        if (_grid.SelectedRows.Count == 0) return;

        int n = _grid.SelectedRows.Count;
        string msg = n == 1
            ? "Удалить выбранную запись?"
            : $"Удалить {n} выбранных записей?";

        if (MessageBox.Show(msg, "Подтверждение удаления",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            return;

        var ids = new List<Guid>();
        foreach (DataGridViewRow row in _grid.SelectedRows)
        {
            if (Guid.TryParse(row.Cells["ID"].Value?.ToString(), out Guid id))
                ids.Add(id);
        }

        _mgr.DeleteRecords(ids);
        RefreshGrid();
        SetStatus($"Удалено {ids.Count} записей", _displayedRecords.Count);
    }

    // ── Location Handlers ─────────────────────────────────────────────────────

    private void OnAddLocation(object? sender, EventArgs e)
    {
        using var dlg = new LocationForm();
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            _mgr.AddLocation(dlg.Result);
            RefreshLocationCombo();
            _cmbLocation.SelectedItem = _mgr.Locations
                .FirstOrDefault(l => l.Id == dlg.Result.Id);
        }
    }

    private void OnEditLocation(object? sender, EventArgs e)
    {
        if (_currentLocation == null) return;
        using var dlg = new LocationForm(_currentLocation);
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            _mgr.UpdateLocation(dlg.Result);
            _currentLocation = dlg.Result;
            RefreshLocationCombo();
            UpdateLocationInfo();
        }
    }

    private void OnDeleteLocation(object? sender, EventArgs e)
    {
        if (_currentLocation == null) return;
        int recs = _mgr.Records.Count(r => r.LocationId == _currentLocation.Id);
        if (MessageBox.Show(
                $"Удалить локацию «{_currentLocation.Name}»\nвместе с {recs} записями?",
                "Удалить локацию",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            return;

        _mgr.DeleteLocation(_currentLocation.Id);
        RefreshLocationCombo();
    }

    // ── Import / Export ───────────────────────────────────────────────────────

    private void OnImportExcel(object? sender, EventArgs e)
    {
        using var dlg = new OpenFileDialog
        {
            Title  = "Импорт данных из Excel",
            Filter = "Excel файлы (*.xlsx;*.xls)|*.xlsx;*.xls|Все файлы|*.*"
        };

        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            Cursor = Cursors.WaitCursor;
            int added = _mgr.ImportFromExcel(dlg.FileName);
            RefreshLocationCombo();           // обновляет комбо и вызывает OnLocationChanged
            AdjustFilterToData();             // фильтр = реальный диапазон данных
            RefreshGrid();
            SetStatus($"Импортировано {added} новых записей из {Path.GetFileName(dlg.FileName)}",
                _displayedRecords.Count);
            MessageBox.Show(
                $"Импорт завершён.\nДобавлено записей: {added}\n" +
                $"Период: {_dtpFrom.Value:dd.MM.yyyy} – {_dtpTo.Value:dd.MM.yyyy}",
                "Импорт", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка импорта:\n{ex.Message}",
                "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            Cursor = Cursors.Default;
        }
    }

    private void OnExportCsv(object? sender, EventArgs e)
    {
        if (_displayedRecords.Count == 0)
        {
            MessageBox.Show("Нет данных для экспорта.", "Экспорт",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dlg = new SaveFileDialog
        {
            Title      = "Экспорт в CSV",
            Filter     = "CSV файлы (*.csv)|*.csv",
            FileName   = $"weather_{_currentLocation?.Name}_{DateTime.Today:yyyyMMdd}.csv"
        };

        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            _mgr.ExportToCsv(dlg.FileName, _displayedRecords);
            SetStatus($"Экспортировано {_displayedRecords.Count} записей в {Path.GetFileName(dlg.FileName)}",
                _displayedRecords.Count);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка экспорта:\n{ex.Message}",
                "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnResetFilter(object? sender, EventArgs e)
    {
        AdjustFilterToData();   // сброс = диапазон реальных данных
        RefreshGrid();
    }

    // ── Chart Handler ─────────────────────────────────────────────────────────

    private void OnShowCharts(object? sender, EventArgs e)
    {
        if (_currentLocation == null || _displayedRecords.Count == 0)
        {
            MessageBox.Show("Нет данных для отображения графиков.Сначала загрузите данные и выберите локацию.",
                "Графики", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        using var frm = new ChartForm(_displayedRecords, _currentLocation.Name);
        frm.ShowDialog(this);
    }

    // ── Sort ──────────────────────────────────────────────────────────────────

    private static void OnGridSortCompare(object? sender, DataGridViewSortCompareEventArgs e)
    {
        // Numeric columns — sort as double, not as string
        if (e.Column.Name is "Темп. (°C)" or "Влажность (%)" or "Облачность (%)")
        {
            bool ok1 = double.TryParse(e.CellValue1?.ToString(),
                NumberStyles.Any, CultureInfo.InvariantCulture, out double d1);
            bool ok2 = double.TryParse(e.CellValue2?.ToString(),
                NumberStyles.Any, CultureInfo.InvariantCulture, out double d2);
            if (ok1 && ok2) { e.SortResult = d1.CompareTo(d2); e.Handled = true; }
            return;
        }

        // Date column — sort by parsed DateTime
        if (e.Column.Name == "Дата")
        {
            bool ok1 = DateTime.TryParseExact(e.CellValue1?.ToString(), "dd.MM.yyyy",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dt1);
            bool ok2 = DateTime.TryParseExact(e.CellValue2?.ToString(), "dd.MM.yyyy",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dt2);
            if (ok1 && ok2) { e.SortResult = dt1.CompareTo(dt2); e.Handled = true; }
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void SetStatus(string msg, int count)
    {
        _lblStatus.Text = msg;
        _lblCount.Text  = $"{count} записей";
    }

    private void ShowWelcomeHint()
    {
        MessageBox.Show(
            "База данных пуста.\n\n" +
            "Нажмите «Файл → Импорт из Excel», чтобы загрузить\n" +
            "данные из файла экспорта Open Meteo,\n" +
            "или «Данные → Новая локация» для ручного ввода.",
            "Добро пожаловать",
            MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}
