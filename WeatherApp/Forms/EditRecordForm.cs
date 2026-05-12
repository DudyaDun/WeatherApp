using WeatherApp.Models;

namespace WeatherApp.Forms;

/// <summary>
/// Dialog for creating or editing a single <see cref="WeatherRecord"/>.
/// </summary>
public class EditRecordForm : Form
{
    // ── Controls ──────────────────────────────────────────────────────────────
    private readonly DateTimePicker  _dtpDate    = new();
    private readonly DateTimePicker  _dtpTime    = new();
    private readonly NumericUpDown   _nudTemp    = new();
    private readonly NumericUpDown   _nudHumid   = new();
    private readonly NumericUpDown   _nudCloud   = new();
    private readonly Label           _lblPreview = new();
    private readonly Button          _btnOk      = new();
    private readonly Button          _btnCancel  = new();

    // ── Result ────────────────────────────────────────────────────────────────
    public WeatherRecord Result { get; private set; }

    // ── Constructor ───────────────────────────────────────────────────────────
    public EditRecordForm(Guid locationId, WeatherRecord? existing = null)
    {
        bool isNew = existing == null;
        Result = existing != null
            ? new WeatherRecord
            {
                Id               = existing.Id,
                LocationId       = existing.LocationId,
                Timestamp        = existing.Timestamp,
                Temperature      = existing.Temperature,
                RelativeHumidity = existing.RelativeHumidity,
                CloudCoverTotal  = existing.CloudCoverTotal
            }
            : new WeatherRecord { LocationId = locationId, Timestamp = DateTime.Now };

        BuildUi(isNew);
        PopulateFields();
        UpdatePreview();
    }

    // ── UI Build ──────────────────────────────────────────────────────────────
    private void BuildUi(bool isNew)
    {
        Text            = isNew ? "Добавить запись" : "Редактировать запись";
        Size            = new Size(400, 380);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition   = FormStartPosition.CenterParent;
        MaximizeBox     = false;
        MinimizeBox     = false;

        var outer = new TableLayoutPanel
        {
            Dock        = DockStyle.Fill,
            ColumnCount = 1,
            RowCount    = 3,
            Padding     = new Padding(12)
        };
        outer.RowStyles.Add(new RowStyle(SizeType.Absolute, 220));
        outer.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));
        outer.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        // ── Fields grid ───────────────────────────────────────────────────────
        var fields = new TableLayoutPanel
        {
            Dock        = DockStyle.Fill,
            ColumnCount = 2,
            RowCount    = 5,
        };
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (int i = 0; i < 5; i++)
            fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));

        // Date
        fields.Controls.Add(Label("Дата:"), 0, 0);
        _dtpDate.Format       = DateTimePickerFormat.Short;
        _dtpDate.Dock         = DockStyle.Fill;
        _dtpDate.ValueChanged += (_, _) => UpdatePreview();
        fields.Controls.Add(_dtpDate, 1, 0);

        // Time
        fields.Controls.Add(Label("Время:"), 0, 1);
        _dtpTime.Format       = DateTimePickerFormat.Time;
        _dtpTime.ShowUpDown   = true;
        _dtpTime.Dock         = DockStyle.Fill;
        _dtpTime.ValueChanged += (_, _) => UpdatePreview();
        fields.Controls.Add(_dtpTime, 1, 1);

        // Temperature
        fields.Controls.Add(Label("Температура (°C):"), 0, 2);
        ConfigNud(_nudTemp, -60, 60, 1, 0.1m);
        _nudTemp.ValueChanged += (_, _) => UpdatePreview();
        fields.Controls.Add(_nudTemp, 1, 2);

        // Humidity
        fields.Controls.Add(Label("Влажность (%):"), 0, 3);
        ConfigNud(_nudHumid, 0, 100, 1, 0.1m);
        _nudHumid.ValueChanged += (_, _) => UpdatePreview();
        fields.Controls.Add(_nudHumid, 1, 3);

        // Cloud cover
        fields.Controls.Add(Label("Облачность (%):"), 0, 4);
        ConfigNud(_nudCloud, 0, 100, 1, 0.1m);
        _nudCloud.ValueChanged += (_, _) => UpdatePreview();
        fields.Controls.Add(_nudCloud, 1, 4);

        outer.Controls.Add(fields, 0, 0);

        // ── Preview box ───────────────────────────────────────────────────────
        var previewBox = new GroupBox
        {
            Text    = "Предварительный просмотр",
            Dock    = DockStyle.Fill,
            Padding = new Padding(6)
        };
        _lblPreview.Dock      = DockStyle.Fill;
        _lblPreview.TextAlign = ContentAlignment.MiddleCenter;
        _lblPreview.Font      = new Font("Segoe UI", 9f);
        previewBox.Controls.Add(_lblPreview);
        outer.Controls.Add(previewBox, 0, 1);

        // ── Buttons ───────────────────────────────────────────────────────────
        var btnPanel = new FlowLayoutPanel
        {
            Dock          = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            Padding       = new Padding(0, 4, 0, 0)
        };

        _btnCancel.Text         = "Отмена";
        _btnCancel.Size         = new Size(90, 30);
        _btnCancel.DialogResult = DialogResult.Cancel;

        _btnOk.Text      = "ОК";
        _btnOk.Size      = new Size(90, 30);
        _btnOk.BackColor = Color.FromArgb(0, 120, 215);
        _btnOk.ForeColor = Color.White;
        _btnOk.FlatStyle = FlatStyle.Flat;
        _btnOk.Click    += BtnOk_Click;

        btnPanel.Controls.Add(_btnCancel);
        btnPanel.Controls.Add(_btnOk);
        outer.Controls.Add(btnPanel, 0, 2);

        Controls.Add(outer);
        AcceptButton = _btnOk;
        CancelButton = _btnCancel;
    }

    private void PopulateFields()
    {
        _dtpDate.Value  = Result.Timestamp.Date;
        _dtpTime.Value  = Result.Timestamp;
        _nudTemp.Value  = (decimal)Math.Clamp(Result.Temperature, -60, 60);
        _nudHumid.Value = (decimal)Math.Clamp(Result.RelativeHumidity, 0, 100);
        _nudCloud.Value = (decimal)Math.Clamp(Result.CloudCoverTotal, 0, 100);
    }

    private void UpdatePreview()
    {
        double t = (double)_nudTemp.Value;
        double h = (double)_nudHumid.Value;
        double c = (double)_nudCloud.Value;

        string sky = c switch
        {
            < 10 => "☀ Ясно",
            < 30 => "🌤 Малооблачно",
            < 70 => "⛅ Переменная облачность",
            < 90 => "🌥 Облачно",
            _    => "☁ Пасмурно"
        };

        string comfort = t switch
        {
            < 10 => "🧊 Холодно",
            < 16 => "🌬 Прохладно",
            < 24 => "😊 Комфортно",
            < 30 => "🌡 Тепло",
            _    => "🔥 Жарко"
        };

        _lblPreview.Text =
            $"{sky}   |   {comfort}\n" +
            $"Температура: {t:F1}°C   Влажность: {h:F0}%   Облачность: {c:F0}%";
    }

    private void BtnOk_Click(object? sender, EventArgs e)
    {
        var date = _dtpDate.Value.Date;
        var time = _dtpTime.Value.TimeOfDay;
        Result.Timestamp        = date + time;
        Result.Temperature      = (double)_nudTemp.Value;
        Result.RelativeHumidity = (double)_nudHumid.Value;
        Result.CloudCoverTotal  = (double)_nudCloud.Value;

        DialogResult = DialogResult.OK;
        Close();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private static Label Label(string text) =>
        new() { Text = text, TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill };

    private static void ConfigNud(NumericUpDown n,
        decimal min, decimal max, int dec, decimal inc)
    {
        n.Minimum       = min;
        n.Maximum       = max;
        n.DecimalPlaces = dec;
        n.Increment     = inc;
        n.Dock          = DockStyle.Fill;
    }
}
