﻿using System.Windows;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Me.Messages.Item.Reply;
using System.Web;
using Microsoft.Graph.Me.SendMail;

namespace Email
{
    public partial class ComposeEmailWindow : Window
    {
        private GraphServiceClient _graphClient;
        private string _originalMessageId;
        private enum emailType { New, Reply, Forward };
        private emailType _emailType;
        public ComposeEmailWindow(GraphServiceClient graphClient, Message originalMessage)
        {
            InitializeComponent();
            _graphClient = graphClient;
            if (originalMessage != null)
            {
                _emailType = emailType.Reply;
                _originalMessageId = originalMessage.Id;
                ToTextBox.Text = originalMessage.Sender.EmailAddress.Address;
                SubjectTextBox.Text = $"RE: {originalMessage.Subject}";
                string cleanedContent = CleanHtmlContent(originalMessage.Body.Content);
                BodyTextBox.Text = $"\n\n--- Original message ---\n{cleanedContent}";
            } else
            {
                _emailType = emailType.New;
            }
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if(string.IsNullOrEmpty(ToTextBox.Text) || string.IsNullOrEmpty(SubjectTextBox.Text) || string.IsNullOrEmpty(BodyTextBox.Text))
                {
                    MessageBox.Show("Please fill all the fields", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                if (_emailType == emailType.Reply)
                {
                    var requestBody = new ReplyPostRequestBody
                    {
                        Message = new Message
                        {
                            Subject = SubjectTextBox.Text,
                            Body = new ItemBody
                            {
                                ContentType = BodyType.Text,
                                Content = BodyTextBox.Text
                            },
                            ToRecipients = new List<Recipient>()
                            {
                                new Recipient
                                {
                                    EmailAddress = new EmailAddress
                                    {
                                        Address = ToTextBox.Text
                                    }
                                }
                            }
                        },
                    };


                    await _graphClient.Me.Messages[_originalMessageId]
                        .Reply
                        .PostAsync(requestBody);
                }
                else if (_emailType == emailType.New)
                {
                    var requestBody = new SendMailPostRequestBody
                    {
                        Message = new Message
                        {
                            Subject = SubjectTextBox.Text,
                            Body = new ItemBody
                            {
                                ContentType = BodyType.Text,
                                Content = BodyTextBox.Text
                            },
                            ToRecipients = new List<Recipient>()
                            {
                                new Recipient
                                {
                                    EmailAddress = new EmailAddress
                                    {
                                        Address = ToTextBox.Text
                                    }
                                }
                            }
                        },
                        SaveToSentItems = true
                    };

                    await _graphClient.Me
                        .SendMail
                        .PostAsync(requestBody);
                } 
                else
                {
                    throw new Exception("Something went wrong");
                }

                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }

        private string CleanHtmlContent(string htmlContent)
        {
            string decodedHtml = HttpUtility.HtmlDecode(htmlContent);

            string plainText = System.Text.RegularExpressions.Regex.Replace(decodedHtml, "<.*?>", string.Empty);

            plainText = System.Text.RegularExpressions.Regex.Replace(plainText, @"(\r\n|\n|\r){2,}", "\n\n");

            return plainText.Trim();
        }
    }
}
