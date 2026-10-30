using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qrs.App.Vault.Client
{
    public class VaultHelper
    {
        // Key is a relative path inside vault  - displayed on UI; Value is a physical OS file path 
        public Dictionary<string, string> Files = new();

        public int AddFile(string inVaultPath, string osPath)
        {
            if (Files.ContainsKey(inVaultPath)) return 0;
            Files[inVaultPath] = osPath;
            return 1;
        }

        public List<string> AddPaths(List<string> paths)
        {
            var res = new List<string>();

            foreach (var path in paths)
            {
                if (Directory.Exists(path))
                {
                    // Recursively add files, preserving folder structure
                    // should work directories WITHOUT trailing slash, so trimming it
                    var baseDir = Path.GetDirectoryName(path.TrimEnd(Path.DirectorySeparatorChar)) ?? path;
                    foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
                    {
                        //string relative = Path.GetRelativePath(baseDir, file);

                        // OS-invariant slash
                        string relative = VaultContext.GetRelativePath(baseDir, file);
                        var cnt = AddFile(relative, file);
                        if (cnt > 0) res.Add(relative);
                    }
                }
                else if (File.Exists(path))
                {
                    string relative = Path.GetFileName(path);
                    var cnt = AddFile(relative, path);
                    if (cnt > 0) res.Add(relative);
                }
            }
            return res;
        }

    }
}
