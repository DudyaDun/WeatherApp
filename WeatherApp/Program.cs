using System.Text;
using WeatherApp.Forms;

namespace WeatherApp;

static class Program
{
    [STAThread]
    static void Main()
    {
        // Register encoding provider for ExcelDataReader
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new MainForm());
    }
}
