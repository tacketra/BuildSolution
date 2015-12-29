using Microsoft.Build.Evaluation;
using Microsoft.CSharp;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

using System.Reflection;
using System.IO;
using System.Diagnostics;
using System.ComponentModel;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using System.ComponentModel.Composition;

namespace BuildSolution
{
    public class SolutionBuilder
    {
        [Import(typeof(SVsServiceProvider))]
        IServiceProvider ServiceProvider { get; set; }

        public static string CurPath =   @"C:\\Windows\\Microsoft.NET\\Framework\\v4.0.30319\\MSBuild.exe";//@"C:\Program Files (x86)\MSBuild\14.0\bin\csc.exe";//@"C:\\Windows\\Microsoft.NET\\Framework\\v4.0.30319\\MSBuild.exe";//@"C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe";//@"C:\\Windows\\Microsoft.NET\\Framework\\v4.0.30319\\MSBuild.exe";
        public static string MsBuildPath = @"C:\\Windows\\Microsoft.NET\\Framework\\v4.0.30319\\MSBuild.exe";
        public static string CscBuildPath = @"C:\\Program Files (x86)\\MSBuild\\14.0\\bin\\csc.exe";//@"C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe";
        public static string BuildArgument = "{0} /t:Build /fileLogger /fileLoggerParameters:logfile=errorshello1.txt;errorsonly";

        static void ProjCollect()
        {
            var projCollection = Microsoft.Build.Evaluation.ProjectCollection.GlobalProjectCollection;
            Console.WriteLine("collection");
        }

        public static void Run()
        {
            Projects allProjects = new Projects();
            Microsoft.Build.Evaluation.ProjectCollection.GlobalProjectCollection.UnloadAllProjects();
            SolutionFile solution = new SolutionFile();
            solution.BuildSolution(true);
        }

        public static void DelMe(string pattern, DirectoryInfo dir)
        {
            foreach (var file in dir.GetFiles(pattern).Where(x => (x.Attributes & FileAttributes.Hidden) == 0))
            {
                 using (FileStream fs = new FileStream(file.FullName, FileMode.Append, FileAccess.Write))
                 using (StreamWriter sw = new StreamWriter(fs))
                 {
                    sw.WriteLine("//adding line");
                 }
                // string echoStr = "echo //adding line >> \"" + file.FullName + "\"";
                // Console.WriteLine(echoStr);
            }

            foreach (var subDir in dir.GetDirectories().Where(x => (x.Attributes & FileAttributes.Hidden) == 0))
            {
                if (!subDir.FullName.Contains("BuildSolution") && !subDir.FullName.Contains("JustBuild") && !subDir.FullName.Contains("msbuild-master"))
                {
                    try
                    {
                        DelMe(pattern, subDir);
                    }
                    catch
                    {
                        // hasn't been hit yet 
                    }
                }
            }
        }

        static void Main(string[] args)
        {
            DirectoryInfo dir = new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
            DelMe("*.cs", dir);
            Console.WriteLine("done writing to .cs files");

            Projects allProjects = new Projects();

            /*below Unload is needed because instantating Micrsooft's Project object forces a reference to be kept in the global project collection.
            Huge waste of memory (rarely will all projects on computer be used) but forces lookup time = O(total number of projects on comp).
            More time and memory efficient to instantiate project objects iff they are going to be built*/
            Microsoft.Build.Evaluation.ProjectCollection.GlobalProjectCollection.UnloadAllProjects();

            SolutionFile solution = new SolutionFile();
            solution.BuildSolution(true); // bool is runInParallel?
            //solution.BuildSolution(false); // bool is runInParallel?

            Console.ReadLine();
        }
    }
}
