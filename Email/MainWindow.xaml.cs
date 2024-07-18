﻿using Microsoft.Graph;
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
        private static string[] _scopes = { "User.Read", "Mail.ReadWrite", "Mail.Send" };

        public MainWindow()
        {
            InitializeComponent();
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            _clientId = App.Configuration["client_id"];
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            try
            {
                if(string.IsNullOrEmpty(_clientId)) throw new Exception("Something went wrong: ClientID");

                await Task.Run(async () =>
                {
                    // Configure interactive browser credentials
                    var interactiveBrowserCredential = new InteractiveBrowserCredential(new InteractiveBrowserCredentialOptions
                    {
                        ClientId = _clientId,
                        RedirectUri = new Uri("http://localhost")
                    });

                    // Initialize the Microsoft Graph client with the credentials
                    _clientApp = new GraphServiceClient(interactiveBrowserCredential, _scopes);

                    //Await a silent token request to ensure login
                    await interactiveBrowserCredential.AuthenticateAsync(cts.Token);

                });

                Dispatcher.Invoke(() =>
                {
                    // Navigate to the emails window
                    EmailsWindow emailsWindow = new EmailsWindow(_clientApp);
                    emailsWindow.Show();
                    this.Close();
                });
            }
            catch (OperationCanceledException)
            {
               Dispatcher.Invoke(() => MessageBox.Show("Login canceled", "Error", MessageBoxButton.OK));
            }
            catch (MsalClientException ex) when (ex.ErrorCode == "authentication_canceled")
            {
                Dispatcher.Invoke(() => MessageBox.Show("Authentication was canceled", "Error", MessageBoxButton.OK));
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK));
            }
        }
    }
}