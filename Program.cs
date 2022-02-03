using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
app.Urls.Add("http://127.0.0.1:0");

app.UseFileServer(new FileServerOptions {
    RequestPath = "",
    FileProvider = new Microsoft.Extensions.FileProviders
                    .ManifestEmbeddedFileProvider(
        typeof(Program).Assembly, "ui"
    ) 
});

app.MapPost("/newguid", () => Guid.NewGuid().ToString());

var task = app.RunAsync();

if (!Debugger.IsAttached)
{
    var url = app.Services.GetRequiredService<IServer>()
        .Features.Get<IServerAddressesFeature>()!
            .Addresses.First();
    Process.Start(new ProcessStartInfo("cmd", $"/c start {url}")
    { CreateNoWindow = true });
}

task.Wait();
