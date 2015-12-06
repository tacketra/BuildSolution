using EnvDTE;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConsoleApplication1;
using DogLibrary;
using EnvDTE80;

namespace BuildSolution
{
    class SolutionFile
    {
        // List<ProjectFile> ProjectFiles { get; set; }
        List<int> ProjectFiles; // array of index's into the array of project files from the projects class (static)

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
            this.ProjectFiles = new List<int>();
            
            List<string> solutionProjects = dte.DTE.Solution.Projects.Cast<EnvDTE.Project>().Select(x => x.FullName).ToList();

            List<int> projIndexs = new List<int>();
            solutionProjects.RunFuncForEach(projString => {
                int index = Array.FindIndex(Projects.ProjectList, x => Helper.FileCompare(x.ProjectPath.FullName, projString));
                if (index != -1) projIndexs.Add(index);
            });

            // solutionProjects.Select(projString => Array.FindIndex(Projects.ProjectList, x => FileHelper.FileCompare(x.ProjectPath.FullName, projString)));
            this.PopulateSolutionProjects(projIndexs);
            // ProjectFile.PopulateReadyToBuild(this.ProjectFiles);
        }

        private void PopulateSolutionProjects(List<int> projList)
        {
            projList.RunFuncForEach(projIndex => this.ProjectFiles.Add(projIndex));

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
                        proj.ReferenceProjects.Add(refIndex); // just added, untested
                        addedProjects.Add(refIndex);
                    }
                   
                }

                ProjectFile.PopulateNeedsToBeBuilt(proj); // just added, this should be accurate

                if (addedProjects.Count != 0)
                {
                    this.PopulateSolutionProjects(addedProjects);
                }
                else
                {
                    proj.ReadyToBuild = true;
                }

            }
        }


        public void BuildSolution()
        {
            Nonsense();

            Console.WriteLine("solution projects, ref projects tabbed under them.");
            this.ProjectFiles.RunFuncForEach(projIndex => {
                var proj = Projects.ProjectList[projIndex];
                Console.WriteLine(proj.ProjectPath + "built?: " + !proj.NeedsToBeBuilt);
                proj.ReferenceProjects.RunFuncForEach(refIndex => Console.WriteLine("    ref: " + Projects.ProjectList[refIndex].ProjectPath));
                Console.WriteLine("----------------------------------------------- \n");
            });

            Console.WriteLine("building projects below \n");
            BuildSolution(this.ProjectFiles.Where(index => (bool)Projects.ProjectList[index].NeedsToBeBuilt || !Projects.ProjectList[index].ReadyToBuild).ToList());

            Console.WriteLine("projects all built! \n");

            Nonsense();
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
                    var dog = Microsoft.Build.Evaluation.ProjectCollection.GlobalProjectCollection;
                    new Microsoft.Build.Evaluation.Project(proj.ProjectPath.FullName).Build(); // I think default build should be good enough?
                    proj.NeedsToBeBuilt = false;
                    anyProjectsBuild = true;
                    Console.WriteLine("building: " +  proj.ProjectPath);
                }

                // if ((bool)!proj.NeedsToBeBuilt && proj.ReadyToBuild) { continue; }


                //while (!proj.ReadyToBuild)
                //{
                //    List<int> projectsThatNeedToBuild = new List<int>();
                //    foreach (int refIndex in proj.ReferenceProjects)
                //    {
                //        var refProj = Projects.ProjectList[refIndex];


                //    }
                //}
            }

            return anyProjectsBuild;
        }

        private void BuildSolutionOld(SolutionFile solution)
        {
            Console.WriteLine("solution projects, ref projects tabbed under them.");
            solution.ProjectFiles.RunFuncForEach(projIndex => {
                var proj = Projects.ProjectList[projIndex];
                Console.WriteLine(proj.ProjectPath + "built?: " + !proj.NeedsToBeBuilt);
                proj.ReferenceProjects.RunFuncForEach(refIndex => Console.WriteLine("    ref: " + proj.ProjectPath));
                Console.WriteLine("----------------------------------------------- \n");
            });


            // ProjectFile.PopulateReferenceProjects(solution.ProjectFiles); // I think this is now handled in instantion of this class
            ProjectFile.PopulateNeedsToBeBuilt(solution.ProjectFiles);

            Console.WriteLine("solution projects ");

            solution.ProjectFiles.RunFuncForEach(x => Console.WriteLine(Projects.ProjectList[x].ProjectPath));
            Console.WriteLine("\n projects that need to be built for solution");
            var projsToBuild = solution.ProjectFiles.Where(proj => Projects.ProjectList[proj].NeedsToBeBuilt.Value).ToList();
            projsToBuild.RunFuncForEach(x => Console.WriteLine(Projects.ProjectList[x].ProjectPath));


            Console.WriteLine("\n Correct build order");
            solution.ProjectFiles.RunFuncForEach(x => Console.WriteLine(Projects.ProjectList[x].ProjectPath));

            ProjectFile.GetCorrectBuildOrder(solution.ProjectFiles.Select(x => Projects.ProjectList[x]).ToList());

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
