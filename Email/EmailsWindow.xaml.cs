using Microsoft.Graph;
using Microsoft.Graph.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Email
{
    /// <summary>
    /// Lógica de interacción para EmailsWindow.xaml
    /// </summary>
    public partial class EmailsWindow : Window
    {
        private GraphServiceClient _graphClient;

        public EmailsWindow(GraphServiceClient graphClient)
        {
            InitializeComponent();
            _graphClient = graphClient;
            LoadEmails();
        }

        private async void LoadEmails()
        {
            try
            {
                // Obtener los mensajes de correo electrónico
                var messagePage = await _graphClient.Me.MailFolders["Inbox"].Messages
                    .GetAsync((config) =>
                    {
                        config.QueryParameters.Select = new[] { "subject", "sender", "receivedDateTime", "body" };
                        config.QueryParameters.Top = 20;
                    });

                List<string> emailSubjects = new List<string>();
                foreach (var message in messagePage.Value)
                {
                    emailSubjects.Add($"{message.Sender.EmailAddress.Name}: {message.Subject}");
                }

                // Verificar si la lista tiene elementos
                if (emailSubjects.Count > 0)
                {
                    MessageBox.Show("Correos electrónicos obtenidos exitosamente.");
                }
                else
                {
                    MessageBox.Show("No se encontraron correos electrónicos.");
                }

                // Actualizar la lista de correos electrónicos en la UI
                EmailsListBox.ItemsSource = emailSubjects;
            }
            catch (ServiceException ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }
    }
}
