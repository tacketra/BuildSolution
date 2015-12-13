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

        public struct MSBuildLock {
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
            for (int i = 0; i <this.BaseProjectFiles.Count; i++) { this.ProjectQueues[i] = new List<int>(); }

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

                    return false;});

                counter += list.Count;
            }

            int processorCount = Environment.ProcessorCount;
            Console.WriteLine("buildSolution Parallel time: " + Helper.TimeFunction(() =>
                BuildSolution(counter, processorCount)));

            Console.WriteLine("projects all built! \n");

            // TestDllRefFunctions();
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
                    Console.WriteLine(proj.ProjectPath + ": " + Helper.TimeFunction(() =>
                    { new Microsoft.Build.Evaluation.Project(proj.ProjectPath.FullName).Build(); // I think default build should be good enough?
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
            Task[] taskArray = new Task[threads];
            for (int i = 0; i < threads; i++)
            {
                int index = i;
                var myTask = new Task(() => { this.BuildSolution(projCount); });

                taskArray[index] = myTask;
                myTask.Start();
            }

            Task.WaitAll(taskArray);
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
                        projIndex = list.RemoveAndGet(0);
                    }

                    var proj = Projects.ProjectList[projIndex];

                    if (proj.HasBuilt.HasValue) { break; }

                    if (proj.ReadyToBuild && (bool)proj.NeedsToBeBuilt)
                    {
                        Console.WriteLine(proj.ProjectPath + ": " + Helper.TimeFunction(() =>
                        {
  
                            if (Monitor.TryEnter(msLock))
                            {
                                Console.WriteLine("starting to MS_build " + proj.ProjectPath.Name);
                                var dog = Microsoft.Build.Evaluation.ProjectCollection.GlobalProjectCollection.LoadedProjects;
                                new Microsoft.Build.Evaluation.Project(proj.ProjectPath.FullName).Build(); // I think default build should be good enough?

                                proj.NeedsToBeBuilt = false;
                                proj.HasBuilt = true;
                                COUNTER++;

                            }
                            else
                            {
                                proj.NeedsToBeBuilt = null;
                                ExecuteCommand(string.Format("{0} {1}", SolutionBuilder.CurPath, "\"" + proj.ProjectPath.FullName + "\""), projIndex);
                            }
                        }));

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
                            Console.WriteLine(proj.ProjectPath + ": " + Helper.TimeFunction(() =>
                            {

                                if (Monitor.TryEnter(msLock))
                                {
                                    Console.WriteLine("starting to MS_build " + proj.ProjectPath.Name);
                                    var dog = Microsoft.Build.Evaluation.ProjectCollection.GlobalProjectCollection.LoadedProjects;
                                    new Microsoft.Build.Evaluation.Project(proj.ProjectPath.FullName).Build(); // I think default build should be good enough?
                                    proj.NeedsToBeBuilt = false;
                                    proj.HasBuilt = true;
                                    COUNTER++;
                                }
                                else
                                {
                                    proj.NeedsToBeBuilt = null;
                                    ExecuteCommand(string.Format("{0} {1}", SolutionBuilder.CurPath, "\"" + proj.ProjectPath.FullName + "\""), projIndex);
                                }
                            }
                            ));

                            break;
                        }
                        else
                        {
                            lock (proj)
                            {
                                proj.NeedsToBeBuilt = false;
                                proj.HasBuilt = false;
                                lock (this.listLock) { COUNTER++; }
                            }

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

        // called if you use an execute command that does not waitfor it to complete, this should be called on completion
        private void BuildProcessExited(object sender, System.EventArgs e, int projIndex)
        {
            var proj = Projects.ProjectList[projIndex];

                proj.NeedsToBeBuilt = false;
                proj.HasBuilt = true;
                COUNTER++; 
            
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
            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = "/c  \"\"C:\\Program Files (x86)\\Microsoft Visual Studio 14.0\\Common7\\Tools\\VsDevCmd.bat\"" + " && csc " + " " + proj.ReferenceCompileArg + " " + proj.TargetCompileArg + " /out:\"" + proj.BuildProjectOutputPath + "\" *.cs\"";//string.Join(" ", proj.ProjectClassPaths.Select(x => "\"" + x.FullName + "\""));//" *.cs";//+ " /maxcpucount:4 /p:BuildInParallel=true";
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
            COUNTER++;

        }


        public void ExecuteCommandOld(ProjectFile proj)
        {
            Console.WriteLine("starting to build " + proj.ProjectPath.Name);

            System.Diagnostics.Process process = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            startInfo.WorkingDirectory = proj.ProjectPath.Directory.FullName;
            startInfo.FileName = "CMD.exe";
            startInfo.Arguments = "/c " + SolutionBuilder.CurPath + " " + proj.ReferenceCompileArg + " " + proj.TargetCompileArg + " " + "/out:" + proj.BuildProjectOutputPath + " " + "*.cs";//string.Join(" ", proj.ProjectClassPaths.Select(x => "\"" + x.FullName + "\""));//" *.cs";//+ " /maxcpucount:4 /p:BuildInParallel=true";
            process.StartInfo = startInfo;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            Console.WriteLine(output);

            proj.NeedsToBeBuilt = false;
            proj.HasBuilt = true;
            COUNTER++;
        }

        public void ExecuteCommand(string command, int projIndex)
        {
            var proj = Projects.ProjectList[projIndex];
            Console.WriteLine("starting to build " + proj.ProjectPath.Name);

            System.Diagnostics.Process process = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            startInfo.FileName = "CMD.exe";
            startInfo.Arguments = "/c " + command + " /maxcpucount:4 /p:BuildInParallel=true";
            process.StartInfo = startInfo;
            process.Start();
            process.WaitForExit();
            proj.NeedsToBeBuilt = false;
            proj.HasBuilt = true;
            COUNTER++;

        }

        public void ExecuteCommandDelay(string command, int projIndex)
        {
            var proj = Projects.ProjectList[projIndex];
            Console.WriteLine("starting to build " + proj.ProjectPath.Name);

            System.Diagnostics.Process process = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            startInfo.FileName = "CMD.exe";
            startInfo.Arguments = "/c " + command; //+ " /maxcpucount:4 /p:BuildInParallel=true";
            process.StartInfo = startInfo;
            process.EnableRaisingEvents = true;
            process.Exited += (sender, e) => BuildProcessExited(sender, e, projIndex);
            process.Start();
        }

    }
}
