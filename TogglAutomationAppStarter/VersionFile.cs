using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TogglAutomationAppStarter
{
    internal static class VersionFile
    {
        const string fileName = "version.txt";
        public static string? GetVersion(string folderPath)
        {
            var filePath = Path.Combine(folderPath, fileName);
            if (!File.Exists(filePath)) return null;

            return File.ReadAllText(filePath);
        }

        public static void WriteVersion(string folderPath, string version)
        {
            File.WriteAllText(Path.Combine(folderPath, fileName), version);
        }
    }
}
