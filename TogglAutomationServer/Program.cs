using System.Net.Http.Headers;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.AspNetCore.WebSockets;
using MySqlConnector;
using TogglAutomationServer.Controllers;
using TogglAutomationServer.Models;
using TogglAutomationServer.Toggl;


UskokDB.ParameterHandler.UseJsonForUnknownClassesAndStructs = true;
UskokDB.ParameterHandler.JsonReader = (str, type) => str == null? null : System.Text.Json.JsonSerializer.Deserialize(str, type);
UskokDB.ParameterHandler.JsonWriter = (obj) => obj == null ? null : System.Text.Json.JsonSerializer.Serialize(obj);
var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DB");
if(connectionString == null)
{
    Console.Error.WriteLine("DB connection string not provided");
    return;
}

Log.ConnectionFactory = new Func<MySqlConnection>(() => new MySqlConnection(connectionString));

var togglSettings = builder.Configuration.GetSection("TogglSettings").Get<TogglSettings>();
if (togglSettings == null)
{

    Console.Error.WriteLine("Toggle settings was not found in appsettings.json");
    return;
}

var connection = Log.ConnectionFactory();
await Log.CreateIfNotExistAsync(connection);
await connection.DisposeAsync();

builder.Services.AddSingleton(togglSettings);
builder.Services.AddScoped<IgorFilterAttribute>();

builder.Services.AddHttpClient("Toggl", client =>
{
    client.BaseAddress = new Uri("https://api.track.toggl.com/");
    client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Basic",
            Convert.ToBase64String(Encoding.UTF8.GetBytes($"{togglSettings.Email}:{togglSettings.Password}")));
});
builder.Services.AddWebSockets(options => options.KeepAliveInterval = TimeSpan.FromSeconds(10));
builder.Services.AddRazorPages();

builder.Services.AddTransient((_) => Log.ConnectionFactory());

builder.Services.AddControllers();
builder.Services.AddLogging(options => options.AddSimpleConsole(c => c.TimestampFormat = "[yyyy-MM-dd HH:mm:ss]"));
builder.WebHost.ConfigureKestrel(options => {
#if !DEBUG
options.ListenAnyIP(togglSettings.ListenPort, listenOptions => {
        var cer = new X509Certificate2("/root/.acme.sh/luka.getremail.com_ecc/luka.getremail.com.cer");
        var key = File.ReadAllText("/root/.acme.sh/luka.getremail.com_ecc/luka.getremail.com.key");
        var ecdsa = ECDsa.Create();
        ecdsa.ImportFromPem(key);
        cer = cer.CopyWithPrivateKey(ecdsa);
        listenOptions.UseHttps(new HttpsConnectionAdapterOptions()
        {
            ServerCertificate = cer
        });
    });
#endif
});

var app = builder.Build();

app.UseWebSockets();
app.MapControllers();
app.MapRazorPages();
app.UseStaticFiles(new StaticFileOptions()
{
    OnPrepareResponse = context =>
    {
        context.Context.Response.Headers.TryAdd("Cache-Control", "no-cache, no-store");
        context.Context.Response.Headers.TryAdd("Expires", "-1");
    },
    
});

app.Run();