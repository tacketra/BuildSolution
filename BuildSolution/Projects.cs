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
        public static List<ProjectFile> ProjectList { get; set; }

        public Projects()
        {
            ProjectList = new List<ProjectFile>();
            PopulateAllProjects();
        }

        void PopulateAllProjects()
        {
            DirectoryInfo dir = new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
            SearchFolder("*.csproj", dir);

            ////var files = dir.GetFiles("*.csproj")
            ////    .Where(x => (x.Attributes & FileAttributes.Hidden) == 0)
            ////    .RunFuncForEach(proj => this.ProjectList.Add(new ProjectFile(proj)));

            var test = "hello";
        }

        // this would be a simple linq query if it weren't for getFiles() trying to search through hidden folders
        void SearchFolder(string pattern, DirectoryInfo dir)
        {
            foreach (var file in dir.GetFiles(pattern).Where(x => (x.Attributes & FileAttributes.Hidden) == 0))
            {
                ProjectList.Add(new ProjectFile(file));
            }

            foreach (var subDir in dir.GetDirectories().Where(x => (x.Attributes & FileAttributes.Hidden) == 0))
            {
                try
                {
                    SearchFolder(pattern, subDir);
                }
                catch
                {
                    // TODO, hasn't been hit 
                }
            }
        }


    }
}
