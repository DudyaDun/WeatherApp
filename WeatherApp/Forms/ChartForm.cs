using System.Drawing;
using WeatherApp.Models;

namespace WeatherApp.Forms;

/// <summary>
/// Отображает графики погодных данных (ScottPlot 4.1).
/// </summary>
public class ChartForm : Form
{
    private readonly List<WeatherRecord> _records;
    private readonly string _locationName;

    public ChartForm(List<WeatherRecord> records, string locationName)
    {
        _records      = records.OrderBy(r => r.Timestamp).ToList();
        _locationName = locationName;

        Text          = $"Графики — {locationName}";
        Size          = new System.Drawing.Size(1050, 670);
        MinimumSize   = new System.Drawing.Size(700, 450);
        StartPosition = FormStartPosition.CenterParent;
        Font          = new System.Drawing.Font("Segoe UI", 9f);

        BuildUi();
    }

    private void BuildUi()
    {
        if (_records.Count == 0)
        {
            Controls.Add(new System.Windows.Forms.Label
            {
                Text      = "Нет данных для отображения.",
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font      = new System.Drawing.Font("Segoe UI", 12f)
            });
            return;
        }

        var tabs = new TabControl { Dock = DockStyle.Fill };

        tabs.TabPages.Add(MakeTemperaturePage());
        tabs.TabPages.Add(MakeHumidityPage());
        tabs.TabPages.Add(MakeCloudPage());
        tabs.TabPages.Add(MakeCombinedPage());

        Controls.Add(tabs);
    }

    // ── Вспомогательные данные ────────────────────────────────────────────────

    private double[] OaDates() =>
        _records.Select(r => r.Timestamp.ToOADate()).ToArray();

    private double[] Temps()   => _records.Select(r => r.Temperature).ToArray();
    private double[] Humid()   => _records.Select(r => r.RelativeHumidity).ToArray();
    private double[] Clouds()  => _records.Select(r => r.CloudCoverTotal).ToArray();

    private static ScottPlot.FormsPlot MakePlot(TabPage page)
    {
        var fp = new ScottPlot.FormsPlot { Dock = DockStyle.Fill };
        page.Controls.Add(fp);
        return fp;
    }

    private static void Finalise(ScottPlot.FormsPlot fp, string title, string yLabel,
        double? yMin = null, double? yMax = null)
    {
        fp.Plot.Title(title);
        fp.Plot.YLabel(yLabel);
        fp.Plot.XAxis.DateTimeFormat(true);
        fp.Plot.Style(
            figureBackground: Color.FromArgb(250, 250, 250),
            dataBackground:   Color.White);
        if (yMin.HasValue || yMax.HasValue)
            fp.Plot.SetAxisLimitsY(yMin ?? double.NaN, yMax ?? double.NaN);
        fp.Refresh();
    }

    // ── Вкладка: Температура ──────────────────────────────────────────────────

    private TabPage MakeTemperaturePage()
    {
        var page = new TabPage("🌡 Температура");
        var fp   = MakePlot(page);

        var s = fp.Plot.AddScatter(OaDates(), Temps(),
            color: Color.FromArgb(210, 60, 60), markerSize: 0);
        s.LineWidth = 1.5f;

        fp.Plot.AddHorizontalLine(0,
            color: Color.FromArgb(100, 160, 255),
            width: 1,
            style: ScottPlot.LineStyle.Dash);

        Finalise(fp, $"Температура воздуха — {_locationName}", "Температура (°C)");
        return page;
    }

    // ── Вкладка: Влажность ────────────────────────────────────────────────────

    private TabPage MakeHumidityPage()
    {
        var page = new TabPage("💧 Влажность");
        var fp   = MakePlot(page);

        var s = fp.Plot.AddScatter(OaDates(), Humid(),
            color: Color.FromArgb(30, 120, 200), markerSize: 0);
        s.LineWidth = 1.5f;

        fp.Plot.AddHorizontalLine(60,
            color: Color.FromArgb(180, 180, 180), width: 1,
            style: ScottPlot.LineStyle.Dash);

        Finalise(fp, $"Относительная влажность — {_locationName}", "Влажность (%)",
            yMin: 0, yMax: 105);
        return page;
    }

    // ── Вкладка: Облачность ───────────────────────────────────────────────────

    private TabPage MakeCloudPage()
    {
        var page = new TabPage("☁ Облачность");
        var fp   = MakePlot(page);

        fp.Plot.AddFill(OaDates(), Clouds(),
            baseline: 0,
            color: Color.FromArgb(120, 140, 180, 220));

        var s = fp.Plot.AddScatter(OaDates(), Clouds(),
            color: Color.FromArgb(80, 130, 190), markerSize: 0);
        s.LineWidth = 1f;

        Finalise(fp, $"Облачность — {_locationName}", "Облачность (%)",
            yMin: 0, yMax: 105);
        return page;
    }

    // ── Вкладка: Все показатели ───────────────────────────────────────────────

    private TabPage MakeCombinedPage()
    {
        var page = new TabPage("📊 Все показатели");

        var tbl = new TableLayoutPanel
        {
            Dock        = DockStyle.Fill,
            ColumnCount = 1,
            RowCount    = 3
        };
        tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3f));
        tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3f));
        tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 33.4f));
        tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        double[] xs = OaDates();

        // — Температура
        var fp1 = new ScottPlot.FormsPlot { Dock = DockStyle.Fill };
        var s1  = fp1.Plot.AddScatter(xs, Temps(),
            color: Color.FromArgb(210, 60, 60), markerSize: 0);
        s1.LineWidth = 1.5f;
        Finalise(fp1, "Температура (°C)", "°C");
        tbl.Controls.Add(fp1, 0, 0);

        // — Влажность
        var fp2 = new ScottPlot.FormsPlot { Dock = DockStyle.Fill };
        var s2  = fp2.Plot.AddScatter(xs, Humid(),
            color: Color.FromArgb(30, 120, 200), markerSize: 0);
        s2.LineWidth = 1.5f;
        Finalise(fp2, "Влажность (%)", "%", yMin: 0, yMax: 105);
        tbl.Controls.Add(fp2, 0, 1);

        // — Облачность
        var fp3 = new ScottPlot.FormsPlot { Dock = DockStyle.Fill };
        fp3.Plot.AddFill(xs, Clouds(), baseline: 0,
            color: Color.FromArgb(120, 140, 180, 220));
        var s3 = fp3.Plot.AddScatter(xs, Clouds(),
            color: Color.FromArgb(80, 130, 190), markerSize: 0);
        s3.LineWidth = 1f;
        Finalise(fp3, "Облачность (%)", "%", yMin: 0, yMax: 105);
        tbl.Controls.Add(fp3, 0, 2);

        page.Controls.Add(tbl);
        return page;
    }
}
