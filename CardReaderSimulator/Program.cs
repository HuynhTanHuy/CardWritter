using CardReaderSimulator.Api;
using CardReaderSimulator.Contracts;
using CardReaderSimulator.Infrastructure;
using CardReaderSimulator.Options;
using CardReaderSimulator.Readers;
using CardReaderSimulator.Services;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<CardReaderOptions>(builder.Configuration.GetSection(CardReaderOptions.SectionName));

builder.Services.AddSingleton<MockCardReader>();
builder.Services.AddSingleton<RealCardReader>();

builder.Services.AddSingleton<ICardReader>(sp =>
{
    var opts = sp.GetRequiredService<IOptions<CardReaderOptions>>().Value;
    return string.Equals(opts.Mode, "Real", StringComparison.OrdinalIgnoreCase)
        ? sp.GetRequiredService<RealCardReader>()
        : sp.GetRequiredService<MockCardReader>();
});

builder.Services.AddSingleton<IManualScanTrigger>(sp =>
{
    var opts = sp.GetRequiredService<IOptions<CardReaderOptions>>().Value;
    return string.Equals(opts.Mode, "Mock", StringComparison.OrdinalIgnoreCase)
        ? sp.GetRequiredService<MockCardReader>()
        : NullManualScanTrigger.Instance;
});

builder.Services.AddHttpClient<IRfidCardsApiClient, RfidCardsApiClient>((sp, client) =>
{
    var opts = sp.GetRequiredService<IOptions<CardReaderOptions>>().Value;
    var baseUrl = string.IsNullOrWhiteSpace(opts.ApiBaseUrl)
        ? "http://localhost:5000"
        : opts.ApiBaseUrl.Trim().TrimEnd('/');
    client.BaseAddress = new Uri(baseUrl + "/", UriKind.Absolute);
});

builder.Services.AddHostedService<CardScanPostingHostedService>();

var app = builder.Build();

var optsMonitor = app.Services.GetRequiredService<IOptions<CardReaderOptions>>();

app.MapGet("/", () => Results.Ok(new { service = "CardReaderSimulator", hint = "POST /simulate/scan khi Mode=Mock và EnableManualSimulateEndpoint=true" }));

app.MapPost("/simulate/scan", (SimulateScanRequest? body, IManualScanTrigger trigger) =>
{
    var o = optsMonitor.Value;
    if (!o.EnableManualSimulateEndpoint)
        return Results.NotFound();
    if (!string.Equals(o.Mode, "Mock", StringComparison.OrdinalIgnoreCase))
        return Results.BadRequest(new { error = "Chỉ dùng khi CardReader:Mode = Mock." });

    trigger.EnqueueScan(body?.RfidCardNumber);
    return Results.Accepted("/simulate/scan", new { queued = true });
});

app.Run();

internal sealed record SimulateScanRequest(string? RfidCardNumber);
