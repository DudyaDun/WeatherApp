using WeatherApp.Models;

namespace WeatherApp.Forms;

/// <summary>
/// Dialog for creating or editing a <see cref="WeatherLocation"/>.
/// </summary>
public class LocationForm : Form
{
    // ── Controls ──────────────────────────────────────────────────────────────
    private readonly TextBox     _txtName       = new();
    private readonly NumericUpDown _nudLat      = new();
    private readonly NumericUpDown _nudLon      = new();
    private readonly NumericUpDown _nudAlt      = new();
    private readonly ComboBox    _cmbResolution = new();
    private readonly Button      _btnOk         = new();
    private readonly Button      _btnCancel     = new();

    // ── Result ────────────────────────────────────────────────────────────────
    public WeatherLocation Result { get; private set; }

    // ── Constructor ───────────────────────────────────────────────────────────
    public LocationForm(WeatherLocation? existing = null)
    {
        Result = existing != null
            ? new WeatherLocation
            {
                Id          = existing.Id,
                Name        = existing.Name,
                Latitude    = existing.Latitude,
                Longitude   = existing.Longitude,
                AltitudeASL = existing.AltitudeASL,
                Resolution  = existing.Resolution
            }
            : new WeatherLocation();

        BuildUi();
        PopulateFields();
    }

    // ── UI Build ──────────────────────────────────────────────────────────────
    private void BuildUi()
    {
        Text            = "Редактировать локацию";
        Size            = new Size(380, 310);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition   = FormStartPosition.CenterParent;
        MaximizeBox     = false;
        MinimizeBox     = false;

        var layout = new TableLayoutPanel
        {
            Dock        = DockStyle.Fill,
            ColumnCount = 2,
            RowCount    = 6,
            Padding     = new Padding(12),
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (int i = 0; i < 5; i++)
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        // Name
        layout.Controls.Add(MakeLabel("Название:"), 0, 0);
        _txtName.Dock = DockStyle.Fill;
        layout.Controls.Add(_txtName, 1, 0);

        // Latitude
        layout.Controls.Add(MakeLabel("Широта (°):"), 0, 1);
        ConfigureNud(_nudLat, -90, 90, 6);
        layout.Controls.Add(_nudLat, 1, 1);

        // Longitude
        layout.Controls.Add(MakeLabel("Долгота (°):"), 0, 2);
        ConfigureNud(_nudLon, -180, 180, 6);
        layout.Controls.Add(_nudLon, 1, 2);

        // Altitude
        layout.Controls.Add(MakeLabel("Высота (м над ур. м.):"), 0, 3);
        ConfigureNud(_nudAlt, -500, 9000, 2);
        layout.Controls.Add(_nudAlt, 1, 3);

        // Resolution
        layout.Controls.Add(MakeLabel("Разрешение:"), 0, 4);
        _cmbResolution.Dock = DockStyle.Fill;
        _cmbResolution.DropDownStyle = ComboBoxStyle.DropDownList;
        _cmbResolution.Items.AddRange(new[] { "hourly", "daily", "6-hourly", "15-minutely" });
        layout.Controls.Add(_cmbResolution, 1, 4);

        // Buttons
        var btnPanel = new FlowLayoutPanel
        {
            Dock          = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            Padding       = new Padding(0, 4, 0, 0)
        };

        _btnCancel.Text         = "Отмена";
        _btnCancel.Size         = new Size(90, 30);
        _btnCancel.DialogResult = DialogResult.Cancel;
        _btnCancel.Click       += (_, _) => Close();

        _btnOk.Text         = "ОК";
        _btnOk.Size         = new Size(90, 30);
        _btnOk.BackColor    = Color.FromArgb(0, 120, 215);
        _btnOk.ForeColor    = Color.White;
        _btnOk.FlatStyle    = FlatStyle.Flat;
        _btnOk.Click       += BtnOk_Click;

        btnPanel.Controls.Add(_btnCancel);
        btnPanel.Controls.Add(_btnOk);

        layout.Controls.Add(btnPanel, 0, 5);
        layout.SetColumnSpan(btnPanel, 2);

        Controls.Add(layout);
        AcceptButton = _btnOk;
        CancelButton = _btnCancel;
    }

    private void PopulateFields()
    {
        _txtName.Text           = Result.Name;
        _nudLat.Value           = (decimal)Math.Clamp(Result.Latitude, -90, 90);
        _nudLon.Value           = (decimal)Math.Clamp(Result.Longitude, -180, 180);
        _nudAlt.Value           = (decimal)Math.Clamp(Result.AltitudeASL, -500, 9000);
        _cmbResolution.SelectedItem = Result.Resolution;
        if (_cmbResolution.SelectedIndex < 0) _cmbResolution.SelectedIndex = 0;
    }

    private void BtnOk_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_txtName.Text))
        {
            MessageBox.Show("Укажите название локации.", "Ошибка",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtName.Focus();
            return;
        }

        Result.Name        = _txtName.Text.Trim();
        Result.Latitude    = (double)_nudLat.Value;
        Result.Longitude   = (double)_nudLon.Value;
        Result.AltitudeASL = (double)_nudAlt.Value;
        Result.Resolution  = _cmbResolution.SelectedItem?.ToString() ?? "hourly";

        DialogResult = DialogResult.OK;
        Close();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private static Label MakeLabel(string text) =>
        new() { Text = text, TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill };

    private static void ConfigureNud(NumericUpDown nud,
        decimal min, decimal max, int decimals)
    {
        nud.Minimum       = min;
        nud.Maximum       = max;
        nud.DecimalPlaces = decimals;
        nud.Increment     = 0.000001m;
        nud.Dock          = DockStyle.Fill;
    }
}
