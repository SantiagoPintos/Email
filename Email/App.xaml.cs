using System.Configuration;
using System.Data;
using System.Windows;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace Email
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static IConfiguration Configuration { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddUserSecrets<MainWindow>();
           
            Configuration = builder.Build();

            base.OnStartup(e);
        }
    }

}
