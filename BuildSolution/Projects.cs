using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildSolution
{
    class Projects
    {
        public static ProjectFile[] ProjectList { get; set; }

        public Projects()
        {
            var projCollection = Microsoft.Build.Evaluation.ProjectCollection.GlobalProjectCollection;
            PopulateAllProjects();
        }

        void PopulateAllProjects()
        {
            DirectoryInfo dir = new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
            List<ProjectFile> tempProjectList = new List<ProjectFile>();
            SearchFolder("*.csproj", dir, tempProjectList);

            Projects.ProjectList = new ProjectFile[tempProjectList.Count];
            Projects.ProjectList = tempProjectList.ToArray();

            ////var files = dir.GetFiles("*.csproj")
            ////    .Where(x => (x.Attributes & FileAttributes.Hidden) == 0)
            ////    .RunFuncForEach(proj => this.ProjectList.Add(new ProjectFile(proj)));
        }

        // this would be a simple linq query if it weren't for getFiles() trying to search through hidden folders
        void SearchFolder(string pattern, DirectoryInfo dir, List<ProjectFile> tempProjectList)
        {
            foreach (var file in dir.GetFiles(pattern).Where(x => (x.Attributes & FileAttributes.Hidden) == 0))
            {
                tempProjectList.Add(new ProjectFile(file));
            }

            foreach (var subDir in dir.GetDirectories().Where(x => (x.Attributes & FileAttributes.Hidden) == 0))
            {
                try
                {
                    SearchFolder(pattern, subDir, tempProjectList);
                }
                catch
                {
                    // TODO, hasn't been hit 
                }
            }
        }


    }
}
