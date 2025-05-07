using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace TogglAutomationAppStarter
{
    internal static class VersionFile
    {
        public static string? GetVersion()
        {
            string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Toggl Automation", "version.txt");
            if (!File.Exists(filePath)) return null;
            return File.ReadAllText(filePath);
        }
    }
}
