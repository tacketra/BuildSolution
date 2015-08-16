using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildSolution
{
    class FileHelper
    {
        public static bool FileCompare(string path1, string path2)
        {
            return NormalizePath(path1).Equals(NormalizePath(path2));
        }

        public static string NormalizePath(string path)
        {
            return Path.GetFullPath(new Uri(path).LocalPath)
                       .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                       .ToUpperInvariant();
        }

        public static string removeFileNameFromPath(string f)
        {
            int lastSlash = f.LastIndexOf("\\");
            return f.Substring(0, lastSlash + 1);
        }

        // returns with out filename (lopping off ext)
        public static string removeFileNameFromPath(string f, out string filename)
        {
            int lastSlash = f.LastIndexOf("\\");
            int lastPeriod = f.LastIndexOf(".");
            filename = f.Substring(lastSlash + 1, lastPeriod - lastSlash - 1);
            return f.Substring(0, lastSlash + 1);
        }


    }
}
