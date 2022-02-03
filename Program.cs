using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
app.Urls.Add("http://127.0.0.1:0");

app.UseFileServer(new FileServerOptions
{
    RequestPath = "",
    FileProvider = new Microsoft.Extensions.FileProviders
                    .ManifestEmbeddedFileProvider(
        typeof(Program).Assembly, "ui"
    )
});

app.MapPost("/newguid", () => Guid.NewGuid().ToString());

int sseCnnCount = 0;
bool sseTriggered = false;
int bps = 4;

app.MapGet("/sse", async (context) =>
{
    sseTriggered = true;
    var resp = context.Response;
    Interlocked.Increment(ref sseCnnCount);
    resp.Headers.Add("Content-Type", "text/event-stream");
    try
    {
        //set reconnetion timeout
        await resp.WriteAsync($"retry: {1000 / bps}\n\n");
        for (int i = 0; i < 60 * bps; i++)
        {
            await resp.WriteAsync($"data: {i}\n\n");
            await resp.Body.FlushAsync();
            if (context.RequestAborted.IsCancellationRequested)
                break;
            await Task.Delay(1000 / bps);
        }
    }
    finally
    {
        Interlocked.Decrement(ref sseCnnCount);
    }
});

var task = app.RunAsync();

if (!Debugger.IsAttached)
{
    var url = app.Services.GetRequiredService<IServer>()
        .Features.Get<IServerAddressesFeature>()!
            .Addresses.First();
    Process.Start(new ProcessStartInfo("cmd", $"/c start {url}")
    { CreateNoWindow = true });
}

// watch dog
var appLife = app.Services.GetRequiredService<IHostApplicationLifetime>();
Task.Factory.StartNew(async () => {
    int zeroCount = 0;
    while (!sseTriggered) {
        await Task.Delay(1000);
    }
    while (zeroCount <= 2 * bps) {
        if (sseCnnCount != 0) zeroCount = 0;
        else zeroCount++;
        await Task.Delay(1000 / bps);
    }
    appLife.StopApplication();
});

task.Wait();
