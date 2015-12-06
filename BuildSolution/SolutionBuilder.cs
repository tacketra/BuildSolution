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

        ////public static string MsBuildPath = @"C:\Program Files\MSBuild\14.0\Bin\MSBuild";
        public static string MsBuildPath = @"C:\Windows\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe";
        ////public static string BuildArgument = "'{0}' /t:Build /p:Configuration=Debug";
        ////public static string BuildArgument = "'{0}' /t:Build";
        public static string BuildArgument = "{0} /t:Build /fileLogger /fileLoggerParameters:logfile=errorshello1.txt;errorsonly";

        // public string SolutionToBuildPath = @"C:\Users\tacke\Documents\visual studio 2015\Projects\helloTest\helloTest.sln";

        static void BuildSolution(string solutionPath)
        {
            var currentPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            currentPath = @"C:\Users\tacke\Desktop\";
            var startInfo = new ProcessStartInfo()
            {
                // FileName = @"C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe",
                // Arguments = string.Format("-ExecutionPolicy RemoteSigned -NoExit -Command \"& {{ Import-Module .\\{0}; Show-GSDocumentation; }}\"", moduleName)
                FileName = MsBuildPath,
                Arguments = string.Format(BuildArgument, solutionPath)
            };

            try
            {
                string arg = string.Format(BuildArgument, solutionPath);
                string workingDirectory = @"C:\Program Files\MSBuild\14.0\Bin";
                workingDirectory = currentPath;
                ProcessStartInfo processStartInfo = new ProcessStartInfo(MsBuildPath);
                processStartInfo.Arguments = arg;
                processStartInfo.CreateNoWindow = true;
                processStartInfo.RedirectStandardError = true;
                processStartInfo.RedirectStandardOutput = true;
                processStartInfo.UseShellExecute = false;
                processStartInfo.WorkingDirectory = workingDirectory;
                System.Diagnostics.Process process = new System.Diagnostics.Process();
                process.StartInfo = processStartInfo;
                //// Process process = Process.Start(processStartInfo);
                process.Start();

                Console.WriteLine(process.StandardOutput.ReadToEnd());
                process.WaitForExit();
                if (process.ExitCode != 0)
                {
                    Console.WriteLine(process.StandardError.ReadToEnd());
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(string.Format("Error \n {0}", e.ToString()));
                return;
            }
        }


        static void AssemTest()
        {
            Assembly testAssembly = Assembly.LoadFile(@"C:\Users\tacke\Documents\Visual Studio 2015\Projects\ConsoleApplication1\ConsoleApplication1\bin\Debug\ConsoleApplication1MyD.dll");
            
            //System.Reflection.
           // Console.WriteLine("test assembly");
        }

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
            solution.BuildSolution();

            // Console.ReadLine();
        }

        static void Main(string[] args)
        {
            Projects allProjects = new Projects();
            Microsoft.Build.Evaluation.ProjectCollection.GlobalProjectCollection.UnloadAllProjects();
            SolutionFile solution = new SolutionFile();
            solution.BuildSolution();

            Console.ReadLine();
        }
    }
}
