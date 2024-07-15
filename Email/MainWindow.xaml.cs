using Microsoft.Graph;
using Microsoft.Identity.Client;
using Azure.Identity;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Email
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private GraphServiceClient _clientApp;
        private static string _clientId;
        private static string[] _scopes = { "User.Read", "Mail.ReadWrite" };

        public MainWindow()
        {
            InitializeComponent();
            _clientId = App.Configuration["client_id"];
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if(string.IsNullOrEmpty(_clientId)) throw new Exception("Something went wrong: ClientID");

                // Configura las credenciales del navegador interactivo
                var interactiveBrowserCredential = new InteractiveBrowserCredential(new InteractiveBrowserCredentialOptions
                {
                    ClientId = _clientId,
                    RedirectUri = new Uri("http://localhost")
                });

                // Inicializa el cliente de Microsoft Graph con las credenciales
                _clientApp = new GraphServiceClient(interactiveBrowserCredential, _scopes);

                // Navega a la ventana de correos electrónicos
                EmailsWindow emailsWindow = new EmailsWindow(_clientApp);
                emailsWindow.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }
    }
}