using EnvDTE;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BuildSolRunner;
using DogLibrary;
using EnvDTE80;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Build.Logging;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;

namespace BuildSolution
{
    class SolutionFile
    {
        List<int> ProjectFiles; // array of index's into the array of project files from the projects class (static)
        List<int> BaseProjectFiles;
        List<int>[] ProjectQueues; // each base project file has a queue of its reference projects to help with build order

        Solution solutionInfo { get; set; }
        Object listLock = new object();
        Object msLock = new object();
        public int COUNTER = 0;

        public struct MSBuildLock
        {
            public bool locked { get; set; }
            public MSBuildLock(bool lockedVal) { locked = lockedVal; }
        };

        public MSBuildLock msBuildLock = new MSBuildLock(false);

        /// <summary>
        /// functions inside this are from referenced dll's. changing output of these dll functions and running Solution Builder
        /// is an easy way to 100% verify that building the projects that were out of date worked.
        /// </summary>
        public void TestDllRefFunctions()
        {
            Console.WriteLine("Nonsense function called");
            Console.WriteLine(SolRunnerPrinter.SolRunnerCallConsoleApp1());
            Console.WriteLine(TheDog.DogNumber());
        }

        /// <summary>
        /// Get all projects in current solution, including projects that created the dll's referenced in current solution.
        /// </summary>
        public SolutionFile()
        {
            EnvDTE80.DTE2 dte = (EnvDTE80.DTE2)System.Runtime.InteropServices.Marshal.GetActiveObject("VisualStudio.DTE");
            this.solutionInfo = dte.DTE.Solution;
            this.ProjectFiles = new List<int>();
            this.BaseProjectFiles = new List<int>();

            List<string> solutionProjects = dte.DTE.Solution.Projects.Cast<EnvDTE.Project>().Select(x => x.FullName).ToList();

            solutionProjects.RunFuncForEach(projString => {
                int index = Array.FindIndex(Projects.ProjectList, x => Helper.FileCompare(x.ProjectPath.FullName, projString));
                if (index != -1) this.BaseProjectFiles.Add(index);
            });

            this.ProjectQueues = new List<int>[this.BaseProjectFiles.Count];
            for (int i = 0; i < this.BaseProjectFiles.Count; i++) { this.ProjectQueues[i] = new List<int>(); }

            this.PopulateSolutionProjects(this.BaseProjectFiles, null);
        }

        private void PopulateSolutionProjects(List<int> projList, int? queIndex)
        {
            projList.RunFuncForEach(projIndex => this.ProjectFiles.Add(projIndex));
            int count = 0;

            // foreach (int projIndex in this.ProjectFiles.Where(x => x != -1))
            foreach (int projIndex in projList)
            {
                var proj = Projects.ProjectList[projIndex];
                if (proj.ReferenceProjects != null)
                {
                    continue;
                }

                // not postive this is correct, so far the way to determine if projects references have been checked
                if (proj.ReferenceProjects == null)
                {
                    proj.ReferenceProjects = new List<int>();
                }

                List<int> addedProjects = new List<int>();
                foreach (var refPath in proj.ReferencePaths)
                {
                    int refIndex = Array.FindIndex(Projects.ProjectList, x => Helper.FileCompare(x.BuildProjectOutputPath.FullName, refPath.FullName));
                    if (refIndex != -1 && !proj.ReferenceProjects.Contains(refIndex))
                    {
                        proj.ReferenceProjects.Add(refIndex);
                        addedProjects.Add(refIndex);
                    }

                }

                ProjectFile.PopulateNeedsToBeBuilt(proj);

                int index = queIndex ?? count;
                if (addedProjects.Count != 0)
                {
                    this.PopulateSolutionProjects(addedProjects, index);

                    this.ProjectQueues[index].Add(projIndex);
                }
                else
                {
                    proj.ReadyToBuild = true; // no other projects need to be built for this project to build correctly, no refd projects

                    this.ProjectQueues[index].Insert(0, projIndex);
                }

                count++;
            }
        }

        public void BuildSolution(bool runInParallel)
        {
            // TestDllRefFunctions();

            Console.WriteLine("solution projects, ref projects tabbed under them.");
            this.ProjectFiles.RunFuncForEach(projIndex => {
                var proj = Projects.ProjectList[projIndex];
                Console.WriteLine(proj.ProjectPath + "built?: " + !proj.NeedsToBeBuilt);
                proj.ReferenceProjects.RunFuncForEach(refIndex => Console.WriteLine("    ref: " + Projects.ProjectList[refIndex].ProjectPath));
                Console.WriteLine("----------------------------------------------- \n");
            });

            Console.WriteLine("building projects below \n");

            if (!runInParallel)
            {
                //if a project does not need to be built and it is ReadyToBuild , it is a stand alone project and is up to date. No need to include it.
                Console.WriteLine("buildSolution Normal time: " + Helper.TimeFunction(() =>
                BuildSolution(this.ProjectFiles.Where(index =>
                    (bool)Projects.ProjectList[index].NeedsToBeBuilt || !Projects.ProjectList[index].ReadyToBuild).ToList())));
                // TestDllRefFunctions();

                return;
            }

            int counter = 0;
            foreach (var list in this.ProjectQueues)
            {
                list.RemoveAll(index => {
                    var proj = Projects.ProjectList[index];
                    if ((bool)!proj.NeedsToBeBuilt && proj.ReadyToBuild)
                    {
                        proj.HasBuilt = false;
                        return true;
                    }

                    return false;
                });

                counter += list.Count;
            }

            Console.WriteLine("getBuildParams time: " + Helper.TimeFunction(() => {
                foreach (var list in this.ProjectQueues)
                {
                    foreach (var projIndex in list)
                    {
                        var proj = Projects.ProjectList[projIndex];
                        GetBuildParams(proj);
                    }
                } }
                ));

            int processorCount = Environment.ProcessorCount;
            Console.WriteLine("buildSolution Parallel time: " + Helper.TimeFunction(() =>
                BuildSolution(counter, processorCount)));

            Console.WriteLine("projects all built! \n");

            // TestDllRefFunctions();
        }

        public static void GetBuildParams(ProjectFile proj)
        {
            ConsoleLogger logger = new ConsoleLogger(LoggerVerbosity.Normal);
            BuildManager manager = BuildManager.DefaultBuildManager;

            ProjectInstance projectInstance = new ProjectInstance(proj.ProjectPath.FullName);

            var result = manager.Build(
                new BuildParameters() {
                    DetailedSummary = true,
                    Loggers = new List<ILogger>() { logger }
                },
                new BuildRequestData(projectInstance, new string[]
                {
                    "ResolveProjectReferences",
                    "ResolveAssemblyReferences"
                })
            );

            proj.ReferenceCompileArg = string.Empty;
            proj.ReferencePaths = new List<FileInfo>();
            var projRefs = PrintResultItems(proj, result, "ResolveProjectReferences");
            var projAssemRefs = PrintResultItems(proj, result, "ResolveAssemblyReferences");
        }

        public static List<string> PrintResultItems(ProjectFile proj,BuildResult result, string targetName)
        {
            List<string> ret = new List<string>();
            var buildResult = result.ResultsByTarget[targetName];
            var buildResultItems = buildResult.Items;

            if (buildResultItems.Length == 0)
            {
                return ret;
            }




            //var buildResList = buildResultItems.Select(x => new FileInfo(x.ItemSpec)).ToList();//Where( fInfo => fInfo.FullName

            //List<string> buildResTempList = new List<string>();
            //int listSize = buildResList.Count;
            //for (int i = 0; i < listSize; i ++)
            //{
            //    var item = buildResList.RemoveAndGet(0);
            //    buildResList.RemoveAll(x => x.Name.Equals(item.Name));
            //    buildResList.Add(item);
            //}

            //foreach (var item in buildResList)
            //{
            //    proj.ReferenceCompileArg += " /r:" + "\"" + item.FullName + "\"";
            //    proj.ReferencePaths.Add(new FileInfo(item.FullName));
            //}
            
            //string heyup = "lakjdsfjdlsk";




            //foreach (var item in buildResTempList)
            //{
            //    ret.Add(string.Format("{0}", item.));
            //    proj.ReferenceCompileArg += " /r:" + "\"" + item.ItemSpec + "\"";
            //    proj.ReferencePaths.Add(new FileInfo(item.ItemSpec));
            //    //Console.WriteLine("{0} reference: {1}", targetName, item.ItemSpec);
            //}

            foreach (var item in buildResultItems)
            {
                ret.Add(string.Format("{0}", item.ItemSpec));
                proj.ReferenceCompileArg += " /r:" + "\"" + item.ItemSpec + "\"";
                proj.ReferencePaths.Add(new FileInfo(item.ItemSpec));
                //Console.WriteLine("{0} reference: {1}", targetName, item.ItemSpec);
            }

            return ret;
        }

        // returns true if any project passed in built, false otherwise
        private bool BuildSolution(List<int> projects)
        {
            bool anyProjectsBuild = false; // if a single projects from the list of projects built return true
            foreach (int projIndex in projects)
            {
                var proj = Projects.ProjectList[projIndex];
                if (this.BuildSolution(proj.ReferenceProjects) || (bool)proj.NeedsToBeBuilt)
                {
                    Console.WriteLine(proj.ProjectPath + ": " + Helper.TimeFunction(() => {
                        new Microsoft.Build.Evaluation.Project(proj.ProjectPath.FullName).Build(); // I think default build should be good enough?
                    }));

                    proj.NeedsToBeBuilt = false;
                    anyProjectsBuild = true;
                }
            }

            return anyProjectsBuild;
        }

        /// <summary>
        /// parallel build, impossilbe with MSBuild so need to use CSC which I cnanot get the ref's passsed in correctly just yet
        /// </summary>
        /// <param name="projCount"></param>
        /// <param name="threads"></param>
        private void BuildSolution(int projCount, int threads)
        {
            var threadList = new List<System.Threading.Thread>();
            for (int i = 0; i < threads; i++)
            {
                System.Threading.Thread t = new System.Threading.Thread(new System.Threading.ThreadStart(() => BuildSolution(projCount)));
                t.Start();
                threadList.Add(t);
            }

            foreach (var t in threadList)
            {
                t.Join();
            }

            //Task[] taskArray = new Task[threads];
            //for (int i = 0; i < threads; i++)
            //{
            //    int index = i;
            //    var myTask = new Task(() => { this.BuildSolution(projCount); });

            //    taskArray[index] = myTask;
            //    myTask.Start();
            //}

            //Task.WaitAll(taskArray);
        }

        public void BuildSolution(int projCount)
        {
            while (this.COUNTER < projCount)
            {
                int i = 0;
                while (i < ProjectQueues.Length)
                {
                    int projQueIndex = i;
                    var list = ProjectQueues[projQueIndex];
                    int projIndex = 0;
                    lock (list)
                    {
                        if (list.Count == 0) { break; }
                        projIndex = list.FindIndex(x => { var projer = Projects.ProjectList[x]; return projer.NeedsToBeBuilt.HasValue && projer.NeedsToBeBuilt.Value; });
                        if (projIndex != -1)
                        {
                            projIndex = list.RemoveAndGet(projIndex);
                        }
                        else
                        {
                            projIndex = list.RemoveAndGet(0);
                        }
                    }

                    var proj = Projects.ProjectList[projIndex];

                    if (proj.HasBuilt.HasValue) { break; }

                    if (proj.ReadyToBuild && (bool)proj.NeedsToBeBuilt)
                    {
                        //this.ExecuteCommand(proj);
                        this.RunCorrectExecuteCommandT(proj, projIndex);

                        break;
                    }

                    bool aRefProjectHasBuilt = false;
                    bool curProjReadyToBuild = true;
                    foreach (var refIndex in proj.ReferenceProjects)
                    {
                        var refProj = Projects.ProjectList[refIndex];
                        if (!refProj.HasBuilt.HasValue)
                        {
                            curProjReadyToBuild = false;
                            break;
                        }

                        if (refProj.HasBuilt.Value)
                        {
                            aRefProjectHasBuilt = true;
                        }
                    }

                    if (curProjReadyToBuild)
                    {
                        if (aRefProjectHasBuilt || proj.NeedsToBeBuilt.Value)
                        {
                            this.RunCorrectExecuteCommandT(proj, projIndex);

                            break;
                        }
                        else
                        {
                            proj.NeedsToBeBuilt = false;
                            proj.HasBuilt = false;
                            lock (this.listLock) { COUNTER++; }

                            break;
                        }
                    }

                    lock (list)
                    {
                        list.Add(projIndex);
                    }

                    i++;

                }
            }
        }

        public static Solution GetCurrentSolution()
        {
            EnvDTE80.DTE2 dte;
            dte = (EnvDTE80.DTE2)System.Runtime.InteropServices.Marshal.
            GetActiveObject("VisualStudio.DTE");
            Console.WriteLine(dte.DTE.Solution.FullName);
            foreach (Project proj in dte.DTE.Solution.Projects)
            {
                Console.WriteLine(proj.FullName);
            }

            return dte.DTE.Solution;
        }

        private void RunCorrectExecuteCommand(ProjectFile proj, int projIndex)
        {
            //ExecuteCommandCSharp(proj);
        }

        private void RunCorrectExecuteCommandT(ProjectFile proj, int projIndex)
        {
            //            Console.WriteLine("starting to MS_build " + proj.ProjectPath.Name);
            //new Microsoft.Build.Evaluation.Project(proj.ProjectPath.FullName).Build();
            //proj.NeedsToBeBuilt = false;
            //proj.HasBuilt = true;
            //lock (this.listLock) { COUNTER++; }
            //return;

            Console.WriteLine(proj.ProjectPath + ": " + Helper.TimeFunction(() => {
                ExecuteCommand(proj);
                /*
                if (Monitor.TryEnter(msLock))
                {
                    Console.WriteLine("starting to MS_build " + proj.ProjectPath.Name);
                    var projToBuild = new Microsoft.Build.Evaluation.Project(proj.ProjectPath.FullName);
                    projToBuild.Build();
                    proj.NeedsToBeBuilt = false;
                    proj.HasBuilt = true;
                    lock (this.listLock) { COUNTER++; }
                }
                else
                {
                    proj.NeedsToBeBuilt = null;
                    //ExecuteCommandOld(proj);
                    ExecuteCommand(SolutionBuilder.CurPath, projIndex);
                }
                */
            }
            ));
        }

        // called if you use an execute command that does not waitfor it to complete, this should be called on completion
        private void BuildProcessExited(object sender, System.EventArgs e, int projIndex)
        {
            var proj = Projects.ProjectList[projIndex];

            proj.NeedsToBeBuilt = false;
            proj.HasBuilt = true;
            lock (this.listLock) { COUNTER++; }

        }

        /// <summary>
        /// Currently not working perfectly. CSC compiler has to be used without msbuild since msbuild does not allow multithreaded building. 
        /// </summary>
        /// <param name="proj"></param>
        public void ExecuteCommand(ProjectFile proj)
        {
            Console.WriteLine("starting to build " + proj.ProjectPath.Name);

            System.Diagnostics.Process process = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            startInfo.WorkingDirectory = proj.ProjectPath.Directory.FullName;
            startInfo.FileName = SolutionBuilder.CscBuildPath;
            startInfo.Arguments = proj.ReferenceCompileArg + " " + proj.TargetCompileArg + " /out:\"" + proj.BuildProjectOutputPath + "\" *.cs\"";//string.Join(" ", proj.ProjectClassPaths.Select(x => "\"" + x.FullName + "\""));//" *.cs";//+ " /maxcpucount:4 /p:BuildInParallel=true";
            //startInfo.Arguments = "/c  \"\"C:\\Program Files (x86)\\Microsoft Visual Studio 14.0\\Common7\\Tools\\VsDevCmd.bat\"" + " && csc " + " " + proj.ReferenceCompileArg + " " + proj.TargetCompileArg + " /out:\"" + proj.BuildProjectOutputPath + "\" *.cs\"";//string.Join(" ", proj.ProjectClassPaths.Select(x => "\"" + x.FullName + "\""));//" *.cs";//+ " /maxcpucount:4 /p:BuildInParallel=true";
            Console.WriteLine(startInfo.Arguments);

            process.StartInfo = startInfo;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            Console.WriteLine(output);

            proj.NeedsToBeBuilt = false;
            proj.HasBuilt = true;
            lock (this.listLock) { COUNTER++; }

        }


        public void ExecuteCommandOld(ProjectFile proj)
        {
            Console.WriteLine("starting to build " + proj.ProjectPath.Name);

            System.Diagnostics.Process process = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            startInfo.WorkingDirectory = proj.ProjectPath.Directory.FullName;
            startInfo.FileName = SolutionBuilder.CurPath;//"CMD.exe";
            startInfo.Arguments = proj.ReferenceCompileArg + " " + proj.TargetCompileArg + " /out:" + proj.BuildProjectOutputPath + " /ruleset: \"C:\\Program Files (x86)\\Microsoft Visual Studio 14.0\\Team Tools\\Static Analysis Tools\\Rule Sets\\MinimumRecommendedRules.ruleset\" /subsystemversion:6.00 /resource:" + proj.ResourceCompileArg + " *.cs \"C:\\Users\\tacke\\AppData\\Local\\Temp\\.NETFramework,Version=v4.5.2.AssemblyAttributes.cs\"";//string.Join(" ", proj.ProjectClassPaths.Select(x => "\"" + x.FullName + "\""));//" *.cs";//+ " /maxcpucount:4 /p:BuildInParallel=true";
            process.StartInfo = startInfo;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            Console.WriteLine(output);

            proj.NeedsToBeBuilt = false;
            proj.HasBuilt = true;
            lock (this.listLock) { COUNTER++; }
        }

        public void ExecuteCommandCSharp(ProjectFile proj)
        {
            Console.WriteLine("starting to build " + proj.ProjectPath.Name);

            System.Diagnostics.Process process = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            startInfo.WorkingDirectory = proj.ProjectPath.DirectoryName;
            startInfo.FileName = @"C:\Users\tacke\Documents\visual studio 2015\Projects\JustBuild\JustBuild\bin\Debug\JustBuild.exe";
            startInfo.Arguments = "\"" + proj.ProjectPath.FullName + "\"";
            startInfo.UseShellExecute = false;
            process.StartInfo = startInfo;


            process.Start();
            process.WaitForExit();

            proj.NeedsToBeBuilt = false;
            proj.HasBuilt = true;
            lock (this.listLock) { COUNTER++; }

        }

        public void ExecuteCommand(string command, int projIndex)
        {
            var proj = Projects.ProjectList[projIndex];
            Console.WriteLine("starting to build " + proj.ProjectPath.Name);

            System.Diagnostics.Process process = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            startInfo.WorkingDirectory = proj.ProjectPath.DirectoryName;
            startInfo.FileName = "CMD.exe";
            startInfo.Arguments = "/c " + command + " " + proj.ProjectPath.Name + " /maxcpucount:4 /p:BuildInParallel=true";
            process.StartInfo = startInfo;
            //process.StartInfo.UseShellExecute = false;
            //process.StartInfo.RedirectStandardOutput = true;
            process.Start();
            //string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            //Console.WriteLine("output" + output);
            proj.NeedsToBeBuilt = false;
            proj.HasBuilt = true;
            lock (this.listLock) { COUNTER++; }

        }

        public void ExecuteCommandDelay(string command, int projIndex)
        {
            var proj = Projects.ProjectList[projIndex];
            Console.WriteLine("starting to build " + proj.ProjectPath.Name);

            System.Diagnostics.Process process = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            startInfo.WorkingDirectory = proj.ProjectPath.DirectoryName;
            startInfo.FileName = "CMD.exe";
            startInfo.Arguments = "/c " + command + " /maxcpucount:4 /p:BuildInParallel=true";
            process.StartInfo = startInfo;
            process.EnableRaisingEvents = true;
            process.Exited += (sender, e) => BuildProcessExited(sender, e, projIndex);
            process.Start();
        }
    }
}
