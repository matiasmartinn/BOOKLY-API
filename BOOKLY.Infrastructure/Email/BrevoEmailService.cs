using BOOKLY.Application.Common;
using BOOKLY.Infrastructure.Email;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;

public sealed class BrevoEmailService : EmailServiceBase
{
    private readonly HttpClient _httpClient;

    public BrevoEmailService(
        HttpClient httpClient,
        IOptions<EmailOptions> emailOptions,
        IOptions<FrontendOptions> frontendOptions)
        : base(emailOptions, frontendOptions)
    {
        _httpClient = httpClient;
    }

    protected override async Task SendAsync(
        string recipientEmail,
        string recipientName,
        string subject,
        string htmlBody,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(EmailOptions.ApiKey))
            throw new InvalidOperationException(
                "Email:ApiKey no está configurada.");

        var payload = new
        {
            sender = new { name = EmailOptions.SenderName, email = EmailOptions.SenderAddress },
            to = new[] { new { email = recipientEmail, name = recipientName } },
            subject,
            htmlContent = htmlBody
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.brevo.com/v3/smtp/email");
        request.Headers.Add("api-key", EmailOptions.ApiKey);
        request.Content = JsonContent.Create(payload);

        var response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
    }
}