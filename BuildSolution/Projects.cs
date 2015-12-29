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
        /// <summary>
        /// list of all projects on the computer, will be filled when a Projects object is instantiated. 
        /// </summary>
        public static ProjectFile[] ProjectList { get; set; }

        public Projects() : this(true) {}

        public Projects(bool runInParallel)
        {
            PopulateAllProjects(runInParallel);
        }

        /// <summary>
        /// Populates static member ProjectList with all c# projects on the computer. Projects have references to dll's from other
        /// projects and from a dll it is impossible to tell what project they came from. So we need all projects on the computer 
        /// to search through and match them to the appropiate project.
        /// </summary>
        void PopulateAllProjects(bool runInParallel)
        {
            DirectoryInfo dir = new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
            List<ProjectFile> tempProjectList = new List<ProjectFile>();

            if (!runInParallel)
            {
                Console.WriteLine("searchFolder Normal time: " + Helper.TimeFunction(() => SearchFolder("*.csproj", dir, tempProjectList)));
            }
            else
            {
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

                var watch = System.Diagnostics.Stopwatch.StartNew();

                var threadList = new List<System.Threading.Thread>();
                for (int i = 0; i < processorCount; i++)
                {
                    int index = i;
                    System.Threading.Thread t = new System.Threading.Thread(new System.Threading.ThreadStart(() => SearchFolderUsingInfos(".csproj", projArray[index], dirInfos[index].ToArray())));
                    t.Start();
                    threadList.Add(t);
                }

                foreach (var t in threadList)
                {
                    t.Join();
                }

                projArray.RunFuncForEach(x => tempProjectList.AddRange(x));

                watch.Stop();

                Console.WriteLine("searchFolder Parallel time: " + watch.ElapsedMilliseconds / 1000d);
            }

            Projects.ProjectList = tempProjectList.ToArray();
        }

        /// <summary>
        /// Similar to SearchFolder function but this gets all files regardless of whether file or directory so that
        /// it is easier to run in parallel. You can split the initial files equally into threads.
        /// Given an extension to filter files by and a starting fileSystemInfos in a dir, look through all dirs
        /// for all files matching the given extension. Then create a project object for that file and append to projList.
        /// This would be a simple linq query if it weren't for getSystemInfoFiles() trying to search through hidden folders. 
        /// Even getSystemInfos().Where(x => x.doSomething) will cause an illegal access exception if the file is hidden, hence this function.
        /// </summary>
        int SearchFolderUsingInfos(string ext, List<ProjectFile> projList, FileSystemInfo[] files)
        {
            foreach (var f in files)
            {
                if ((f.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden )
                {
                    continue; // go to the next file, the current file or directory is hidden. Trying to access it will cause an exception.
                }

                if (f is FileInfo && f.Extension.Equals(ext))
                {
                    projList.Add(new ProjectFile((FileInfo)f));
                }
                else
                {
                    if (f is DirectoryInfo)
                    {
                        try
                        {
                            this.SearchFolderUsingInfos(ext, projList, ((DirectoryInfo)f).GetFileSystemInfos());
                        }
                        catch
                        {
                            // Has not been hit yet
                        }
                    }
                }
            }

            return 0;
        }



        /// <summary>
        /// Given a pattern to filter files by and a starting directory (dir), look through dir and all subdirectories
        /// for all files matching the given pattern. Then create a project object for that file and append to projList.
        /// This would be a simple linq query if it weren't for getFiles() trying to search through hidden folders. 
        /// Even getFiles().Where(x => x.doSomething) will cause an illegal access exception if the file is hideen, hence this function.
        /// </summary>
        void SearchFolder(string pattern, DirectoryInfo dir, List<ProjectFile> projList)
        {
            foreach (var file in dir.GetFiles(pattern).Where(x => (x.Attributes & FileAttributes.Hidden) == 0))
            {
                
                projList.Add(new ProjectFile(file));
            }

            foreach (var subDir in dir.GetDirectories().Where(x => (x.Attributes & FileAttributes.Hidden) == 0))
            {
                try
                {
                    SearchFolder(pattern, subDir, projList);
                }
                catch
                {
                    // hasn't been hit yet 
                }
            }
        }


    }
}
