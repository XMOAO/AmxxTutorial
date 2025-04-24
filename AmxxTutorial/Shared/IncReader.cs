using System;
using System.Collections.Generic;
using System.IO;

namespace AmxxTutorial.Shared
{
    public static class IncReader
    {
        private static readonly Dictionary<string, List<IncFile>> IncFilesByVersion = new Dictionary<string, List<IncFile>>();

        public static void LoadIncFiles()
        {
            IncFilesByVersion.Clear();

            string includesPath = Path.Combine(AppContext.BaseDirectory, "Resources", "Includes"); ;

            if (!Directory.Exists(includesPath))
            {
                Directory.CreateDirectory(includesPath);
                return;
            }

            foreach (var versionFolder in Directory.GetDirectories(includesPath))
            {
                var versionName = new DirectoryInfo(versionFolder).Name;
                var files = new List<IncFile>();

                foreach (var file in Directory.GetFiles(versionFolder, "*.inc"))
                {
                    var content = File.ReadAllText(file);
                    var fileName = Path.GetFileName(file);
                    files.Add(new IncFile { FileName = fileName, Content = content });
                }

                IncFilesByVersion[versionName] = files;
            }
        }

        public static List<string> GetVersions()
        {
            return new List<string>(IncFilesByVersion.Keys);
        }

        public static List<IncFile> GetIncFilesByVersion(string version)
        {
            return IncFilesByVersion.TryGetValue(version, out var list) ? list : new List<IncFile>();
        }
    }
    public class IncFile
    {
        public string? FileName { get; set; }
        public string? Content { get; set; }
    }
}
