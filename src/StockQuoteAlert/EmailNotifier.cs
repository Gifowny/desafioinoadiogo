using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace StockQuoteAlert;

public class EmailNotifier(SmtpSettings smtp, EmailSettings email) : INotifier
{
    public async Task SendAsync(string subject, string body, CancellationToken cancellationToken = default)
    {
        // monta a mensagem
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Stock Quote Alert", smtp.Username));
        message.To.Add(MailboxAddress.Parse(email.To));
        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = body };

        // conecta, autentica, envia e desconecta
        using var client = new SmtpClient();
        await client.ConnectAsync(smtp.Host, smtp.Port, SecureSocketOptions.Auto, cancellationToken);
        await client.AuthenticateAsync(smtp.Username, smtp.Password, cancellationToken);
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }
}