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
        // List<ProjectFile> ProjectFiles { get; set; }
        int[] ProjectFiles; // array of index's into the array of project files from the projects class (static)

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

            this.ProjectFiles = new int[Projects.ProjectList.Length];
            for (int i = 0; i < this.ProjectFiles.Length; i++)
            {
                this.ProjectFiles[i] = -1;
            }

            
            List<string> solutionProjects = dte.DTE.Solution.Projects.Cast<EnvDTE.Project>().Select(x => x.FullName).ToList();

            List<int> projIndexs = new List<int>();
            solutionProjects.RunFuncForEach(projString => {
                int index = Array.FindIndex(Projects.ProjectList, x => FileHelper.FileCompare(x.ProjectPath.FullName, projString));
                if (index != -1) projIndexs.Add(index);
            });



            // solutionProjects.Select(projString => Array.FindIndex(Projects.ProjectList, x => FileHelper.FileCompare(x.ProjectPath.FullName, projString)));
            this.PopulateSolutionProjects(projIndexs);
        }

        public void PopulateSolutionProjects(List<int> projList)
        {
            projList.RunFuncForEach(projIndex => this.ProjectFiles[projIndex] = projIndex);

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
                    int refIndex = Array.FindIndex(Projects.ProjectList, x => FileHelper.FileCompare(x.BuildProjectOutputPath.FullName, refPath.FullName));
                    if (refIndex != -1 && !proj.ReferenceProjects.Contains(refIndex))
                    {
                        proj.ReferenceProjects.Add(refIndex); // just added, untested
                        addedProjects.Add(refIndex);
                    }
                   
                }

                if (addedProjects.Count != 0)
                {
                    this.PopulateSolutionProjects(addedProjects);
                }

            }

            var dog = "doggy";
        }

        ////public void PopulateSolutionProjects(List<ProjectFile> projList, int index)
        ////{
            
        ////    foreach (string proj in projList)
        ////    {
        ////        var projectFromList = Projects.ProjectList.Where(x => FileHelper.FileCompare(x.ProjectPath.FullName, proj)).Single();
        ////        if (!this.ProjectFiles.Contains(projectFromList))
        ////        {
        ////            this.ProjectFiles.Add(projectFromList);
        ////        }

        ////        // this.ProjectFiles.Add(new ProjectFile(new FileInfo(proj.FullName)));
        ////    }

        ////    var refProjects = new List<ProjectFile>();
        ////    foreach (var proj in this.ProjectFiles.Where(x => x.ReferenceProjects == null))
        ////    {
        ////        // not postive this is correct, so far the way to determine if projects references have been checked
        ////        if (proj.ReferenceProjects == null)
        ////        {
        ////            proj.ReferenceProjects = new List<ProjectFile>();
        ////        }

        ////        foreach (var refPath in proj.ReferencePaths)
        ////        {
        ////            var projectsFromRefs = Projects.ProjectList.Where(x => FileHelper.FileCompare(x.BuildProjectOutputPath.FullName, refPath.FullName)).ToList();
        ////            if (projectsFromRefs.Any())
        ////            {
        ////                var firstProj = projectsFromRefs.First();
        ////                if (!this.ProjectFiles.Contains(firstProj))
        ////                {
        ////                    proj.ReferenceProjects.Add(firstProj); // just added, untested
        ////                    refProjects.Add(firstProj);
        ////                }
        ////            }
        ////        }

        ////    }

        ////    this.PopulateSolutionProjects(refProjects);

        ////    var dog = "doggy";
        ////}


        public void BuildSolution()
        {
            Nonsense();
            SolutionFile.BuildSolution(this);
        }

        public static void BuildSolution(SolutionFile solution)
        {
            // ProjectFile.PopulateReferenceProjects(solution.ProjectFiles); // I think this is now handled in instantion of this class
            ProjectFile.PopulateNeedsToBeBuilt(solution.ProjectFiles);

            // remove below
            Console.WriteLine("solution projects ");

            solution.ProjectFiles.RunFuncForEach(x => Console.WriteLine(Projects.ProjectList[x].ProjectPath));
            Console.WriteLine("\n projects that need to be built for solution");
            var projsToBuild = solution.ProjectFiles.Where(proj => Projects.ProjectList[proj].NeedsToBeBuilt.Value).ToList();
            projsToBuild.RunFuncForEach(x => Console.WriteLine(Projects.ProjectList[x].ProjectPath));


            Console.WriteLine("\n Correct build order");
            solution.ProjectFiles.RunFuncForEach(x => Console.WriteLine(Projects.ProjectList[x].ProjectPath));
            // remove above

            ProjectFile.GetCorrectBuildOrder(solution.ProjectFiles.Select(x => Projects.ProjectList[x]).ToList() );

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
