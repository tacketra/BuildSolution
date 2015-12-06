using EnvDTE;
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
            PopulateAllProjects();
        }

        void PopulateAllProjects()
        {
            DirectoryInfo dir = new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
            int processorCount = Environment.ProcessorCount;
            var dirFiInfos = dir.GetFileSystemInfos().ToList();
            var dirInfos = new List<FileSystemInfo>[processorCount];
            List<ProjectFile>[] projArray = new List<ProjectFile>[processorCount];

            int interval = 0;
            int j = -1;
            for (int i = 0; i < dirFiInfos.Count; i++)
            {
                if (i >= interval)
                {
                    j++;
                    projArray[j] = new List<ProjectFile>();
               
                    dirInfos[j] = new List<FileSystemInfo>();
                    interval += (dirFiInfos.Count / 4) + 1;
                }

                dirInfos[j].Add(dirFiInfos[i]);
            }
            

            List<ProjectFile> tempProjectList = new List<ProjectFile>();

            Microsoft.Build.Evaluation.ProjectCollection.GlobalProjectCollection.UnloadAllProjects();

            Task[] taskArray = new Task[processorCount];

            var watch = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 0; i < processorCount; i++)
            {
                int index = i;
                var myTask = new Task(() => { SearchFolderUsingInfos(".csproj", projArray[index], dirInfos[index].ToArray()); });
                myTask.Start();
                taskArray[index] = myTask;
            }

            Task.WaitAll(taskArray);
            projArray.RunFuncForEach(x => tempProjectList.AddRange(x));

            watch.Stop();
            Projects.ProjectList = tempProjectList.ToArray();

            Console.WriteLine("searchFolder Parallel time: " + watch.ElapsedMilliseconds / 1000d);

            List<ProjectFile> tempProjectList2 = new List<ProjectFile>();

            Console.WriteLine("searchFolder Normal time: " + Helper.TimeFunction(() => SearchFolder("*.csproj", dir, tempProjectList2)));
        }

        // pass in list of fileInfo's then determine which are files and which are direcotires, easier with the parallel implementation
        int SearchFolderUsingInfos(string pattern, List<ProjectFile> projList, FileSystemInfo[] files)
        {
            foreach (var f in files)
            {
                if ((f.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden )
                {
                    continue;
                }

                if (f is FileInfo && f.Extension.Equals(pattern))
                {
                    projList.Add(new ProjectFile((FileInfo)f));
                }
                else
                {
                    if (f is DirectoryInfo)
                    {
                        try
                        {
                            this.SearchFolderUsingInfos(pattern, projList, ((DirectoryInfo)f).GetFileSystemInfos());
                        }
                        catch{}
                    }
                }
            }

            return 0;
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
