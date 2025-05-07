// https://luka.getremail.com:8213/version
// http://localhost:5000

using System.Diagnostics;
using System.Net.Http.Json;
using TogglAutomationAppStarter;

//#if DEBUG
//const string baseAddress = "http://localhost:5006";
//#else
const string baseAddress = "https://luka.getremail.com:8213";
//#endif

var httpClient = new HttpClient()
{
    BaseAddress = new Uri(baseAddress)
};
Console.WriteLine("Fetching the newest version...");
string version;
try
{
    var response = await httpClient.GetFromJsonAsync<VersionApi>("version") ?? throw new Exception();
    version = response.Version;
    Console.WriteLine($"Newest version: '{version}'");
}
catch(Exception ex)
{
    Console.WriteLine("Failed to fetch the new version please try again or ask Luka in the group..");
    Console.WriteLine(ex.Message);
    Console.WriteLine("Press any key to continue...");
    Console.ReadKey();
    return;
}
string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Toggl Automation");
if (!Directory.Exists(folderPath))
    Directory.CreateDirectory(folderPath);

var exeFilePath = Path.Combine(folderPath, "togglautomationapp.exe");
var currentVersion = VersionFile.GetVersion(folderPath);
if(currentVersion != version)
{
    Console.WriteLine("New version avaliable downloading...");
    try
    {
        var response = await httpClient.GetByteArrayAsync("version/download") ?? throw new Exception();
        Console.WriteLine(response.Length);
        await File.WriteAllBytesAsync(exeFilePath, response);
        Console.WriteLine("New version downloaded!");
        VersionFile.WriteVersion(folderPath, version);
    }
    catch(Exception ex)
    {
        Console.WriteLine("Failed to fetch the new version please try again or ask Luka in the group..");
        Console.WriteLine(ex.Message);
        Console.WriteLine("Press any key to continue...");
        Console.ReadKey();
        return;
    }
}

Console.WriteLine("Version is same openning app");
ProcessStartInfo startInfo = new ProcessStartInfo
{
    FileName = exeFilePath,
    UseShellExecute = true
};

try
{
    Process.Start(startInfo);
    Console.WriteLine("Executable started successfully.");
}
catch (Exception ex)
{
    Console.WriteLine($"An error occurred starting the app: {ex.Message}");
}

record VersionApi(string Version);