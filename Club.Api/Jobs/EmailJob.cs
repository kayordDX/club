using System.Text.Json;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MimeKit;
using Club.Common.Config;
using Club.Data;
using Club.Models;
using TickerQ.Utilities.Base;

namespace Club.Jobs;

public class EmailJob(AppDbContext dbContext, IOptions<EmailConfig> emailConfig)
{
    private readonly EmailConfig _emailConfig = emailConfig.Value;

    [TickerFunction("EmailJob")]
    public async Task SendEmails(CancellationToken ct)
    {
        if (string.IsNullOrEmpty(_emailConfig.Email))
        {
            throw new Exception("Email is empty in config");
        }
        if (string.IsNullOrEmpty(_emailConfig.Host))
        {
            throw new Exception("Email host is empty in config");
        }
        if (string.IsNullOrEmpty(_emailConfig.Password))
        {
            throw new Exception("Email password is empty in config");
        }

        var pendingEmails = await dbContext.EmailLog
            .FromSql($"""
                select * from email_log
                where is_sent = false
                order by created_at_utc
                for update skip locked
            """)
            .ToListAsync(ct);

        if (pendingEmails.Count == 0) return;


        using var client = new SmtpClient();
        try
        {
            await client.ConnectAsync(_emailConfig.Host, _emailConfig.Port, SecureSocketOptions.StartTlsWhenAvailable, ct);
            await client.AuthenticateAsync(_emailConfig.Email, _emailConfig.Password, ct);

            foreach (var log in pendingEmails)
            {
                try
                {
                    var emailData = JsonSerializer.Deserialize<EmailPayload>(log.Payload);
                    if (emailData == null) continue;

                    var mail = new MimeMessage();
                    mail.From.Add(new MailboxAddress(_emailConfig.Name, _emailConfig.Email));
                    mail.Subject = log.Subject;

                    mail.To.AddRange(emailData.To.Select(x => new MailboxAddress(x.Name, x.Email)));

                    if (emailData.Cc?.Any() == true)
                        mail.Cc.AddRange(emailData.Cc.Select(x => new MailboxAddress(x.Name, x.Email)));

                    if (emailData.Bcc?.Any() == true)
                        mail.Bcc.AddRange(emailData.Bcc.Select(x => new MailboxAddress(x.Name, x.Email)));

                    if (!string.IsNullOrEmpty(emailData.ReplyTo))
                        mail.ReplyTo.Add(MailboxAddress.Parse(emailData.ReplyTo));

                    var builder = new BodyBuilder();
                    if (emailData.IsHtml) builder.HtmlBody = log.Message;
                    else builder.TextBody = log.Message;

                    mail.Body = builder.ToMessageBody();

                    await client.SendAsync(mail, ct);

                    log.IsSent = true;
                    log.ErrorMessage = null;
                }
                catch (Exception ex)
                {
                    log.RetryCount += 1;
                    log.ErrorMessage = $"{ex.Message} (Attempt {log.RetryCount})";
                    if (log.RetryCount > 3)
                    {
                        log.IsSent = true;
                    }
                }
                finally
                {
                    await dbContext.SaveChangesAsync(ct);
                }
            }
        }
        finally
        {
            await client.DisconnectAsync(true, ct);
        }
    }
}
