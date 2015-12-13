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

        public static string CurPath =  @"C:\\Windows\\Microsoft.NET\\Framework\\v4.0.30319\\MSBuild.exe";//@"C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe";//@"C:\\Windows\\Microsoft.NET\\Framework\\v4.0.30319\\MSBuild.exe";
        public static string MsBuildPath = @"C:\\Windows\\Microsoft.NET\\Framework\\v4.0.30319\\MSBuild.exe";
        public static string CscBuildPath = @"C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe";
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

        static void Main(string[] args)
        {
            Projects allProjects = new Projects();

            /*below Unload is needed because instantating Micrsooft's Project object forces a reference to be kept in the global project collection.
            Huge waste of memory (rarely will all projects on computer be used) but forces lookup time = O(total number of projects on comp).
            More time and memory efficient to instantiate project objects iff they are going to be built*/
            Microsoft.Build.Evaluation.ProjectCollection.GlobalProjectCollection.UnloadAllProjects();

            SolutionFile solution = new SolutionFile();
            solution.BuildSolution(true); // bool is runInParallel?

            Console.ReadLine();
        }
    }
}
