using System.Net;
using System.Net.Sockets;
using System.Text;
using FSH.Framework.Mailing;
using FSH.Framework.Mailing.Services;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Framework.Tests.Mailing;

// MailOptions:Smtp:Security decides how SmtpMailService opens the connection. The connect call lives
// inside the service with a MailKit client it constructs itself, so the only honest observation point
// is a real socket: a loopback listener that speaks plain SMTP and, like Mailpit, never offers STARTTLS.
public sealed class SmtpConnectionSecurityTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);

    private static ServiceProvider BuildProvider(int port, string? security)
    {
        var settings = new Dictionary<string, string?>
        {
            ["MailOptions:UseSendGrid"] = "false",
            ["MailOptions:From"] = "noreply@x.com",
            ["MailOptions:Smtp:Host"] = "127.0.0.1",
            ["MailOptions:Smtp:Port"] = port.ToString(System.Globalization.CultureInfo.InvariantCulture),
        };
        if (security is not null)
        {
            settings["MailOptions:Smtp:Security"] = security;
        }

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(settings).Build());
        services.AddLogging();
        services.AddHeroMailing();
        return services.BuildServiceProvider();
    }

    private static MailRequest Request() =>
        new(to: ["dest@x.com"], subject: "Confirm your e-mail", body: "<p>hi</p>", from: "sender@x.com");

    #region Binding

    [Fact]
    public void Security_Should_DefaultToStartTls_When_NotConfigured()
    {
        // Arrange
        using var provider = BuildProvider(port: 587, security: null);

        // Act
        SecureSocketOptions security = provider.GetRequiredService<IOptions<MailOptions>>().Value.Smtp!.Security;

        // Assert — every deployment configured before the option existed keeps connecting with STARTTLS.
        security.ShouldBe(SecureSocketOptions.StartTls);
    }

    [Theory]
    [InlineData("None", SecureSocketOptions.None)]
    [InlineData("SslOnConnect", SecureSocketOptions.SslOnConnect)]
    [InlineData("StartTlsWhenAvailable", SecureSocketOptions.StartTlsWhenAvailable)]
    public void Security_Should_BindByName_When_Configured(string configured, SecureSocketOptions expected)
    {
        // Arrange
        using var provider = BuildProvider(port: 587, security: configured);

        // Act
        SecureSocketOptions security = provider.GetRequiredService<IOptions<MailOptions>>().Value.Smtp!.Security;

        // Assert
        security.ShouldBe(expected);
    }

    [Fact]
    public void Security_Should_FailToBind_When_TheNameIsUnknown()
    {
        // Arrange — a typo must stop the options from binding rather than silently fall back to a
        // mode the operator did not ask for.
        using var provider = BuildProvider(port: 587, security: "Plain");
        IOptions<MailOptions> options = provider.GetRequiredService<IOptions<MailOptions>>();

        // Act + Assert
        Should.Throw<InvalidOperationException>(() => options.Value);
    }

    #endregion

    #region Connection

    [Fact]
    public async Task SendAsync_Should_DeliverOverPlainSmtp_When_SecurityIsNone()
    {
        // Arrange
        using var cts = new CancellationTokenSource(Timeout);
        using var server = PlainSmtpServer.Start(cts.Token);
        using var provider = BuildProvider(server.Port, security: "None");
        IMailService mail = provider.GetRequiredService<IMailService>();

        // Act
        await mail.SendAsync(Request(), cts.Token);
        IReadOnlyList<string> commands = await server.CompletedSessionAsync(cts.Token);

        // Assert — the envelope and the message reached the server, which is what a local catcher needs.
        commands.ShouldContain(c => c.StartsWith("MAIL FROM:<sender@x.com>", StringComparison.Ordinal));
        commands.ShouldContain(c => c.StartsWith("RCPT TO:<dest@x.com>", StringComparison.Ordinal));
        server.Data.ShouldContain("Subject: Confirm your e-mail");
        server.ClientHungUp.ShouldBeFalse();
        commands[^1].ShouldBe("QUIT");
    }

    [Fact]
    public async Task SendAsync_Should_Fail_When_SecurityDefaultsToStartTlsAndTheServerOffersNone()
    {
        // Arrange
        using var cts = new CancellationTokenSource(Timeout);
        using var server = PlainSmtpServer.Start(cts.Token);
        using var provider = BuildProvider(server.Port, security: null);
        IMailService mail = provider.GetRequiredService<IMailService>();

        // Act
        InvalidOperationException ex = await Should.ThrowAsync<InvalidOperationException>(
            () => mail.SendAsync(Request(), cts.Token));
        IReadOnlyList<string> commands = await server.CompletedSessionAsync(cts.Token);

        // Assert — the pre-option behaviour, kept as the default: a plain catcher is refused before any
        // envelope is sent, so nothing is delivered.
        ex.InnerException.ShouldBeOfType<NotSupportedException>();
        commands.ShouldNotContain(c => c.StartsWith("MAIL FROM", StringComparison.Ordinal));
    }

    #endregion

    // One-connection SMTP server: greets, answers 250 to everything (so EHLO advertises no extension,
    // STARTTLS included), takes one DATA block and ends on QUIT or when the client hangs up.
    private sealed class PlainSmtpServer : IDisposable
    {
        private readonly TcpListener _listener;
        private readonly List<string> _commands = [];
        private readonly StringBuilder _data = new();
        private Task _session = Task.CompletedTask;

        private PlainSmtpServer(TcpListener listener) => _listener = listener;

        public int Port => ((IPEndPoint)_listener.LocalEndpoint).Port;

        public string Data => _data.ToString();

        public bool ClientHungUp { get; private set; }

        public static PlainSmtpServer Start(CancellationToken ct)
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var server = new PlainSmtpServer(listener);
            server._session = server.RunAsync(ct);
            return server;
        }

        public async Task<IReadOnlyList<string>> CompletedSessionAsync(CancellationToken ct)
        {
            await _session.WaitAsync(ct);
            return _commands;
        }

        private async Task RunAsync(CancellationToken ct)
        {
            using TcpClient client = await _listener.AcceptTcpClientAsync(ct);
            await using NetworkStream stream = client.GetStream();
            using var reader = new StreamReader(stream, Encoding.ASCII);
            await using var writer = new StreamWriter(stream, Encoding.ASCII) { NewLine = "\r\n", AutoFlush = true };

            try
            {
                await writer.WriteLineAsync("220 localhost ESMTP test");
                while (await reader.ReadLineAsync(ct) is { } line)
                {
                    _commands.Add(line);
                    string verb = line.Split(' ', 2)[0].ToUpperInvariant();
                    if (verb == "QUIT")
                    {
                        await writer.WriteLineAsync("221 bye");
                        return;
                    }

                    if (verb == "DATA")
                    {
                        await writer.WriteLineAsync("354 end with <CRLF>.<CRLF>");
                        await ReadDataAsync(reader, ct);
                    }

                    await writer.WriteLineAsync("250 ok");
                }
            }
            // A client dropping the socket mid-dialogue is the refused-connection path under test, not a
            // server fault: the commands recorded up to that point are the evidence.
            catch (IOException)
            {
                ClientHungUp = true;
            }
        }

        private async Task ReadDataAsync(StreamReader reader, CancellationToken ct)
        {
            while (await reader.ReadLineAsync(ct) is { } line && line != ".")
            {
                _data.AppendLine(line);
            }
        }

        // Both tests await the session, so its outcome is observed there; disposing only frees the port.
        public void Dispose() => _listener.Dispose();
    }
}
