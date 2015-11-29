using EnvDTE;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConsoleApplication1;
using DogLibrary;

namespace BuildSolution
{
    class SolutionFile
    {
        List<ProjectFile> ProjectFiles { get; set; }

        Solution solutionInfo { get; set; }

        public void Nonsense()
        {
            Console.WriteLine("Nonsense function called");
            Console.WriteLine(ConsoleApp1.GetHello());
            Console.WriteLine(TheDog.DogNumber());

        }

        public SolutionFile()
        {
            EnvDTE80.DTE2 dte = (EnvDTE80.DTE2)System.Runtime.InteropServices.Marshal.GetActiveObject("VisualStudio.DTE");
            this.solutionInfo = dte.DTE.Solution;

            this.ProjectFiles = new List<ProjectFile>();
            //dte.DTE.Solution

            List<string> solutionProjects = dte.DTE.Solution.Projects.Cast<EnvDTE.Project>().Select(x => x.FullName).ToList();
            this.PopulateSolutionProjects(solutionProjects);

           
            
            // OLD BELOW

            foreach (Project proj in dte.DTE.Solution.Projects)
            {
                string projFull = proj.FullName;
                var projectFromList = Projects.ProjectList.Where(x => FileHelper.FileCompare(x.ProjectPath.FullName, proj.FullName)).Single();
                this.ProjectFiles.Add(projectFromList);

                // this.ProjectFiles.Add(new ProjectFile(new FileInfo(proj.FullName)));
            }

            var refProjects = new List<ProjectFile>();
            foreach (var proj in this.ProjectFiles)
            {
                foreach (var refPath in proj.ReferencePaths)
                {
                    var projectsFromRefs = Projects.ProjectList.Where(x => FileHelper.FileCompare(x.BuildProjectOutputPath.FullName, refPath.FullName)).ToList();
                    if (projectsFromRefs.Any())
                    {
                        refProjects.Add(projectsFromRefs.First()); 
                    }

                }
            }

            this.ProjectFiles.AddRange(refProjects); // might need to check for dupes here, probably by just projectPath
            var dog = "doggy";
            // this.ProjectFiles.RunFuncForEach(x => x.ReferencePaths.RunFuncForEach(y => this.ProjectFiles.Add(new ProjectFile(new FileInfo(y.FullName)))));// this.ProjectFiles.Add(new ProjectFile(new FileInfo(x.ref))))
        }

        public void PopulateSolutionProjects(List<string> projList)
        {
            foreach (string proj in projList)
            {
                var projectFromList = Projects.ProjectList.Where(x => FileHelper.FileCompare(x.ProjectPath.FullName, proj)).Single();
                this.ProjectFiles.Add(projectFromList);

                // this.ProjectFiles.Add(new ProjectFile(new FileInfo(proj.FullName)));
            }

            var refProjects = new List<ProjectFile>();
            foreach (var proj in this.ProjectFiles)
            {
                foreach (var refPath in proj.ReferencePaths)
                {
                    var projectsFromRefs = Projects.ProjectList.Where(x => FileHelper.FileCompare(x.BuildProjectOutputPath.FullName, refPath.FullName)).ToList();
                    if (projectsFromRefs.Any())
                    {
                        refProjects.Add(projectsFromRefs.First());
                    }

                }
            }

            this.ProjectFiles.AddRange(refProjects); // might need to check for dupes here, probably by just projectPath
            var dog = "doggy";
            // this.ProjectFiles.RunFuncForEach(x => x.ReferencePaths.RunFuncForEach(y => this.ProjectFiles.Add(new ProjectFile(new FileInfo(y.FullName)))));// this.ProjectFiles.Add(new ProjectFile(new FileInfo(x.ref))))
        }

        ////public SolutionFile()
        ////{
        ////    EnvDTE80.DTE2 dte = (EnvDTE80.DTE2)System.Runtime.InteropServices.Marshal.GetActiveObject("VisualStudio.DTE");
        ////    this.solutionInfo = dte.DTE.Solution;

        ////    this.ProjectFiles = new List<ProjectFile>();
        ////    //dte.DTE.Solution
        ////    foreach (Project proj in dte.DTE.Solution.Projects) 
        ////    {
        ////        this.ProjectFiles.Add(new ProjectFile(new FileInfo(proj.FullName)));
        ////    }

        ////    this.ProjectFiles.RunFuncForEach(x => x.ReferencePaths.RunFuncForEach(y => this.ProjectFiles.Add(new ProjectFile(new FileInfo(y.FullName)))));// this.ProjectFiles.Add(new ProjectFile(new FileInfo(x.ref))))
        ////}

        public void BuildSolution()
        {
            Nonsense();
            SolutionFile.BuildSolution(this);
        }

        public static void BuildSolution(SolutionFile solution)
        {
            ProjectFile.PopulateReferenceProjects(solution.ProjectFiles);
            ProjectFile.PopulateNeedsToBeBuilt(solution.ProjectFiles);

            // remove below
            Console.WriteLine("solution projects ");

            solution.ProjectFiles.RunFuncForEach(x => Console.WriteLine(x.ProjectPath));
            Console.WriteLine("\n projects that need to be built for solution");
            var projsToBuild = solution.ProjectFiles.Where(proj => proj.NeedsToBeBuilt.Value).ToList();
            projsToBuild.RunFuncForEach(x => Console.WriteLine(x.ProjectPath));


            Console.WriteLine("\n Correct build order");
            solution.ProjectFiles.RunFuncForEach(x => Console.WriteLine(x.ProjectPath));
            // remove above

            ProjectFile.GetCorrectBuildOrder(solution.ProjectFiles);

        }

        public static string GetCurrentSolution()
        {
            EnvDTE80.DTE2 dte;
            dte = (EnvDTE80.DTE2)System.Runtime.InteropServices.Marshal.
            GetActiveObject("VisualStudio.DTE");
            Console.WriteLine(dte.DTE.Solution.FullName);
            foreach (Project proj in dte.DTE.Solution.Projects)
            {
                Console.WriteLine(proj.FullName);
            }

            return dte.DTE.Solution.FullName;
        }
    }
}
