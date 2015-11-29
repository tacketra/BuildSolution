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
    class Program
    {
        [Import(typeof(SVsServiceProvider))]
        IServiceProvider ServiceProvider { get; set; }

        ////public static string MsBuildPath = @"C:\Program Files\MSBuild\14.0\Bin\MSBuild";
        public static string MsBuildPath = @"C:\Windows\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe";
        ////public static string BuildArgument = "'{0}' /t:Build /p:Configuration=Debug";
        ////public static string BuildArgument = "'{0}' /t:Build";
        public static string BuildArgument = "{0} /t:Build /fileLogger /fileLoggerParameters:logfile=errorshello1.txt;errorsonly";

        public Projects AllProjects { get; set; }
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

                ////var newProcess = new Process();
                ////newProcess.StartInfo = new ProcessStartInfo(MsBuildPath);
                ////string arg = string.Format(BuildArgument, solutionPath);
                ////newProcess.StartInfo.Arguments = arg;
                //// newProcess.StartInfo = startInfo;

                ////var output = newProcess.StandardOutput;
                ////Console.WriteLine("starting to read from process");
                ////while (!output.EndOfStream)
                ////{
                ////    Console.WriteLine(output.ReadLine());
                ////}

                ////var genesightShellProcess = new GenesightShellProcess(newProcess);
                ////GenesightShellProcess.currentShell = genesightShellProcess;
                ////return genesightShellProcess;
            }
            catch (Exception e)
            {
                Console.WriteLine(string.Format("Error \n {0}", e.ToString()));
                return;
            }
        }

        public static void BuildTest(string rootPath)
        {
            List<ProjectFile> projectFiles = Directory.GetFiles(rootPath, "*.csproj", SearchOption.AllDirectories).Select(file => new ProjectFile(new FileInfo(file))).ToList();

            Console.WriteLine("printing projects below");
            foreach (var proj in projectFiles)
            {
                Console.WriteLine(proj.ProjectPath.ToString());
            }

            ProjectFile.PopulateReferenceProjects(projectFiles);
            projectFiles.RunFuncForEach(x => ProjectFile.PopulateNeedsToBeBuilt(x));

            var projsToBuild = projectFiles.Where(proj => proj.NeedsToBeBuilt.Value).ToList();
            
            // XmlDocument xmldoc = new XmlDocument();
            ////xmldoc.Load(rootPath);

            ////XmlNamespaceManager ns = new XmlNamespaceManager(xmldoc.NameTable);
            ////ns.AddNamespace("msbld", "http://schemas.microsoft.com/developer/msbuild/2003");
            ////XmlNode node = xmldoc.SelectSingleNode("//msbld:TheNodeIWant", ns);
        }

        ////public static void BuildAllSolutions(string rootPath)
        ////{
        ////    ////string[] files = Directory.GetFiles(rootPath, "*.sln", SearchOption.AllDirectories);
        ////    string[] files = Directory.GetFiles(rootPath, "*.csproj", SearchOption.AllDirectories);
        ////    foreach (var f in files)
        ////    {
        ////        string compilePath  = ProjectFile.GetCSProjOutputPath(f);

        ////        string pathWithoutProjFile = string.Empty, projFileName = string.Empty;
        ////        pathWithoutProjFile = FileHelper.removeFileNameFromPath(f, out projFileName);
        ////        string binPath = pathWithoutProjFile + compilePath;

        ////        string[] binFiles = Directory.GetFiles(binPath);
        ////        string buildFile = binFiles.Single(x => x.EndsWith(projFileName + ".dll") || x.EndsWith(projFileName + ".exe"));

        ////        string[] classFiles = Directory.GetFiles(pathWithoutProjFile, "*.cs", SearchOption.AllDirectories);
        ////        classFiles = classFiles.Where(x => !x.Contains("TemporaryGeneratedFile")).ToArray();
        ////        foreach (string fil in classFiles)
        ////        {
        ////            Console.WriteLine(fil);
        ////        }


        ////        Console.WriteLine(buildFile);
        ////        ////XmlNode node = doc.DocumentElement.SelectSingleNode("Project/PropertyGroup");
        ////        ////XmlNode node = doc.DocumentElement.SelectSingleNode("Project/PropertyGroup/OutputPath");
        ////    }
        ////}

        // delete me


        static void AssemTest()
        {
            Assembly testAssembly = Assembly.LoadFile(@"C:\Users\tacke\Documents\Visual Studio 2015\Projects\ConsoleApplication1\ConsoleApplication1\bin\Debug\ConsoleApplication1MyD.dll");
            
            //System.Reflection.
           // Console.WriteLine("test assembly");
        }

        static void Main(string[] args)
        {
            Projects allProjects = new Projects();

            SolutionFile solution = new SolutionFile();
            solution.BuildSolution();

           //  Console.Read();

            // string root = @"C:\Users\tacke\Documents\Visual Studio 2015\Projects";
            // BuildTest(root);
            //// BuildAllSolutions(root);

            /*
            string solutionToBuildPath = @"""C:\Users\tacke\Documents\visual studio 2015\Projects\helloTest\helloTest.sln""";
            BuildSolution(solutionToBuildPath);
            */

            Console.ReadLine();
        }
    }
}
