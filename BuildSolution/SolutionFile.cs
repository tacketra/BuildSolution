using EnvDTE;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildSolution
{
    public class SolutionFile
    {
        List<ProjectFile> ProjectFiles { get; set; }

        Solution solutionInfo { get; set; }

        public SolutionFile()
        {
            EnvDTE80.DTE2 dte = (EnvDTE80.DTE2)System.Runtime.InteropServices.Marshal.GetActiveObject("VisualStudio.DTE");
            this.solutionInfo = dte.DTE.Solution;

            this.ProjectFiles = new List<ProjectFile>();
            foreach (Project proj in dte.DTE.Solution.Projects)
            {
                this.ProjectFiles.Add(new ProjectFile(new FileInfo(proj.FullName)));
            }


        }

        public void BuildSolution()
        {
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
