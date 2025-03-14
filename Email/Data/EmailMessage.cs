using System;
using System.Collections.Generic;

namespace Email.Data
{
    public class EmailMessage
    {
        public string Id { get; set; }
        public string Subject { get; set; }
        public string SenderEmail { get; set; }
        public DateTime ReceivedDateTime { get; set; }
        public string BodyContent { get; set; }
        public string BodyType { get; set; }
        public bool IsRead { get; set; }
        public string FolderId { get; set; }
        public DateTime LastSyncDateTime { get; set; }
        public List<EmailAttachment> Attachments { get; set; }
    }

    public class EmailAttachment
    {
        public string Id { get; set; }
        public string EmailMessageId { get; set; }
        public string Name { get; set; }
        public string ContentType { get; set; }
        public string ContentId { get; set; }
        public byte[] Content { get; set; }
    }
}