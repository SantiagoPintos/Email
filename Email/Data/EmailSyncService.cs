using Microsoft.EntityFrameworkCore;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Email.Data
{
    public class EmailSyncService
    {
        private readonly EmailDbContext _dbContext;
        private readonly GraphServiceClient _graphClient;

        public EmailSyncService(EmailDbContext dbContext, GraphServiceClient graphClient)
        {
            _dbContext = dbContext;
            _graphClient = graphClient;
        }

        public async Task<List<EmailMessage>> GetLocalEmails(string folderId = "Inbox")
        {
            return await _dbContext.Emails
                .Include(e => e.Attachments)
                .Where(e => e.FolderId == folderId)
                .OrderByDescending(e => e.ReceivedDateTime)
                .Take(50)
                .ToListAsync();
        }

        public async Task SyncEmailsFromServer(string folderId = "Inbox")
        {
            var messagePage = await _graphClient.Me.MailFolders[folderId].Messages
                .GetAsync((config) =>
                {
                    config.QueryParameters.Select = new[] { "subject", "sender", "receivedDateTime", "body", "attachments", "isRead" };
                    config.QueryParameters.Expand = new[] { "attachments" };
                    config.QueryParameters.Orderby = new[] { "receivedDateTime desc" };
                    config.QueryParameters.Top = 50;
                });

            foreach (var message in messagePage.Value)
            {
                var existingEmail = await _dbContext.Emails.FindAsync(message.Id);
                if (existingEmail == null)
                {
                    var emailMessage = new EmailMessage
                    {
                        Id = message.Id,
                        Subject = message.Subject,
                        SenderEmail = message.Sender?.EmailAddress?.Address,
                        ReceivedDateTime = message.ReceivedDateTime?.DateTime ?? DateTime.UtcNow,
                        BodyContent = message.Body?.Content,
                        BodyType = message.Body?.ContentType?.ToString(),
                        IsRead = message.IsRead ?? false,
                        FolderId = folderId,
                        LastSyncDateTime = DateTime.UtcNow,
                        Attachments = new List<EmailAttachment>()
                    };

                    if (message.Attachments != null)
                    {
                        foreach (var attachment in message.Attachments)
                        {
                            if (attachment is FileAttachment fileAttachment)
                            {
                                emailMessage.Attachments.Add(new EmailAttachment
                                {
                                    Id = fileAttachment.Id,
                                    EmailMessageId = message.Id,
                                    Name = fileAttachment.Name,
                                    ContentType = fileAttachment.ContentType,
                                    ContentId = fileAttachment.ContentId,
                                    Content = fileAttachment.ContentBytes
                                });
                            }
                        }
                    }

                    _dbContext.Emails.Add(emailMessage);
                }
                else
                {
                    existingEmail.IsRead = message.IsRead ?? existingEmail.IsRead;
                    existingEmail.LastSyncDateTime = DateTime.UtcNow;
                }
            }

            await _dbContext.SaveChangesAsync();
        }

        public async Task UpdateEmailReadStatus(string emailId, bool isRead)
        {
            var email = await _dbContext.Emails.FindAsync(emailId);
            if (email != null)
            {
                email.IsRead = isRead;
                email.LastSyncDateTime = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();
            }
        }
    }
}