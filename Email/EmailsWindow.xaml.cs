using Microsoft.Graph;
using Microsoft.Graph.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
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
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace Email
{
    /// <summary>
    /// Lógica de interacción para EmailsWindow.xaml
    /// </summary>
    public partial class EmailsWindow : Window
    {
        private GraphServiceClient _graphClient;
        private List<Message> _emails;

        public EmailsWindow(GraphServiceClient graphClient)
        {
            InitializeComponent();
            _graphClient = graphClient;
            InitializeWebView();
            LoadEmails();
        }

        private async void InitializeWebView()
        {
            await BodyWebView.EnsureCoreWebView2Async(null);
            BodyWebView.CoreWebView2.Settings.AreDefaultScriptDialogsEnabled = false;
            BodyWebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            BodyWebView.CoreWebView2.Settings.AreHostObjectsAllowed = true;
            BodyWebView.CoreWebView2.Settings.IsScriptEnabled = false;
            //TODO: img src
            BodyWebView.CoreWebView2.Settings.AreDevToolsEnabled = true;
        }

        private async void LoadEmails()
        {
            try
            {
                // Obtener los mensajes de correo electrónico
                var messagePage = await _graphClient.Me.MailFolders["Inbox"].Messages
                    .GetAsync((config) =>
                    {
                        config.QueryParameters.Select = new[] { "subject", "sender", "receivedDateTime", "body", "attachments" };
                        config.QueryParameters.Expand = new[] { "attachments " };
                        config.QueryParameters.Orderby = new[] { "receivedDateTime desc" };
                        config.QueryParameters.Top = 50;
                    });

                _emails = messagePage.Value.ToList();
                EmailsListBox.ItemsSource = _emails;

                // Verificar si la lista tiene elementos
                if (_emails.Count <= 0)
                {
                    MessageBox.Show("No se encontraron correos electrónicos.");
                }
            }
            catch (ServiceException ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }

        private async void EmailsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (EmailsListBox.SelectedItem is Message selectedEmail)
            {
                SubjectTextBlock.Text = selectedEmail.Subject;
                SenderTextBlock.Text = selectedEmail.Sender.EmailAddress.Address;
                DateTextBlock.Text = selectedEmail.ReceivedDateTime?.ToString("g");

                //Replace CID with base64 content
                if(selectedEmail.Attachments != null)
                {
                    foreach(var attachment in selectedEmail.Attachments)
                    {
                        if (attachment is FileAttachment fileAttachment)
                        {
                            if (!string.IsNullOrEmpty(fileAttachment.ContentId))
                            {
                                string base64content = Convert.ToBase64String(fileAttachment.ContentBytes);
                                string mimeType = fileAttachment.ContentType;
                                string dataUri = $"data:{mimeType};base64,{base64content}";
                                
                                selectedEmail.Body.Content = selectedEmail.Body.Content.Replace($"cid:{fileAttachment.ContentId}", dataUri);
                            }
                        }
                    }
                }


                BodyWebView.NavigateToString(selectedEmail.Body.Content);
                await MarkEmailAsRead(selectedEmail);

                EmailDetailsGrid.Visibility = Visibility.Visible;

                
            }
            else
            {
                EmailDetailsGrid.Visibility = Visibility.Collapsed;
            }

        }

        private async Task MarkEmailAsRead(Message email)
        {
            try
            {
                if(email.IsRead==true) return;
                email.IsRead = true;

                var result  = await _graphClient.Me.Messages[email.Id]
                    .PatchAsync(email);
            }
            catch (ServiceException ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }
    }
}
