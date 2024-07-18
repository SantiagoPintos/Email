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
    public partial class EmailsWindow : Window
    {
        private GraphServiceClient _graphClient;
        private List<Message> _emails;
        private List<MailFolder> _categories;

        public EmailsWindow(GraphServiceClient graphClient)
        {
            InitializeComponent();
            _graphClient = graphClient;
            InitializeWebView();
            LoadOutlookEmails();
            LoadOutlookCategories();
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

        private async void LoadOutlookEmails()
        {
            try
            {
                // Get outlook emails
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

        private async void LoadOutlookCategories()
        {
            try
            {
                var categories = await _graphClient.Me
                    .MailFolders
                    .GetAsync();
                _categories = categories.Value.ToList();
                FoldersListBox.ItemsSource = _categories;
                if(_categories.Count <= 0)
                {
                    MessageBox.Show("We couldn't find any categories.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
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

                MarkAsReadButton.IsEnabled = !selectedEmail.IsRead.GetValueOrDefault();
                MarkAsUnreadButton.IsEnabled = selectedEmail.IsRead.GetValueOrDefault();
                ReplyButton.IsEnabled = true;
                DeleteButton.IsEnabled = true;

                EmailDetailsGrid.Visibility = Visibility.Visible;
            }
            else
            {
                EmailDetailsGrid.Visibility = Visibility.Collapsed;
            }

        }

        //Email folders
        private async void FoldersListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(FoldersListBox.SelectedItem is MailFolder selectedCategory)
            {
                try
                {
                    var messages = await _graphClient.Me.MailFolders[selectedCategory.Id]
                        .Messages
                        .GetAsync((config) =>
                        {
                            config.QueryParameters.Select = new[] { "subject", "sender", "receivedDateTime", "body", "attachments" };
                            config.QueryParameters.Expand = new[] { "attachments " };
                            config.QueryParameters.Orderby = new[] { "receivedDateTime desc" };
                            config.QueryParameters.Top = 50;
                        });

                    _emails = messages.Value.ToList();
                    EmailsListBox.ItemsSource = _emails;
                }
                catch (ServiceException ex)
                {
                    MessageBox.Show($"Error: {ex.Message}");
                }
            }

        }

        private async void MarkAsUnreadButton_Click(object sender, RoutedEventArgs e)
        {
            if (EmailsListBox.SelectedItem is Message selectedEmail)
            {
                await MarkOutlookEmailAsUnread(selectedEmail);
                selectedEmail.IsRead = false;
                EmailsListBox.Items.Refresh();
                MarkAsUnreadButton.IsEnabled = false;
            }
        }

        private async void MarkAsReadButton_Click(object sender, RoutedEventArgs e)
        {
            if (EmailsListBox.SelectedItem is Message selectedEmail)
            {
                await MarkEmailAsRead(selectedEmail);
                selectedEmail.IsRead = true;
                EmailsListBox.Items.Refresh();
                MarkAsReadButton.IsEnabled = false; 
            }
        }

        private void ReplyButton_Click(object sender, RoutedEventArgs e)
        {
            if (EmailsListBox.SelectedItem is Message selectedEmail)
            {
                var composeWindow = new ComposeEmailWindow(_graphClient, selectedEmail);
                composeWindow.Owner = this;
                composeWindow.ShowDialog();
            }
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (EmailsListBox.SelectedItem is Message selectedEmail)
            {
                try
                {
                    await _graphClient.Me.Messages[selectedEmail.Id]
                        .DeleteAsync();

                    _emails.Remove(selectedEmail);
                    EmailsListBox.ItemsSource = null;
                    EmailsListBox.ItemsSource = _emails;
                    EmailDetailsGrid.Visibility = Visibility.Collapsed;
                    MessageBox.Show("Deleted!");
                }
                catch (ServiceException ex)
                {
                    MessageBox.Show($"Error: {ex.Message}");
                }
            }
        }

        private async void ComposeButton_Click(object sender, RoutedEventArgs e)
        {
            var composeWindow = new ComposeEmailWindow(_graphClient, null);
            composeWindow.Owner = this;
            composeWindow.ShowDialog();
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

        private async Task MarkOutlookEmailAsUnread(Message email)
        {
            try
            {
                if (email.IsRead == false) return;
                email.IsRead = false;

                var result = await _graphClient.Me.Messages[email.Id]
                    .PatchAsync(email);
            }
            catch (ServiceException ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }
    }
}
