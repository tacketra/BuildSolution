using Microsoft.Build.Evaluation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace BuildSolution
{
    class ProjectFile
    {
        class ProjectItemTypes
        {
            public static readonly string BuildOutputPath = "BuiltProjectOutputGroupKeyOutput";
            public static readonly string CompilePath = "Compile";
            public static readonly string Reference = "Reference";
            public static readonly string PropOutputPath = "OutputPath";
            public static readonly string PropOutputFileName = "TargetFileName";
        }

        public FileInfo ProjectPath { get; set; }

        public FileInfo BuildProjectOutputPath { get; set; }

        public List<FileInfo> ProjectClassPaths { get; set; }

        public List<FileInfo> ReferencePaths { get; set; }

        public List<int> ReferenceProjects { get; set; } // index to the project in the array of projects

        public bool? NeedsToBeBuilt { get; set; }

        public bool ReadyToBuild { get; set; } = false; // default false since we initially have no idea if proj is ready to build without checking refs

        public bool? HasBuilt { get; set; }

        public ProjectFile()
        {

        }

        public ProjectFile(FileInfo file)
        {
            if (file.FullName.Contains("itcher"))
            {
                string hey = "hello";
            }

            Project project = new Project(file.FullName); // item = metadata , directMetadata count > 0 . metadata[x] where name = HintPath then evaluatedInlcude of that

            this.ProjectPath = file;

            ////this.BuildProjectOutputPath = new FileInfo(project.Items.Single(item => item.ItemType.Equals(ProjectItemTypes.BuildOutputPath)).EvaluatedInclude); // EvaluatedInclude
            var dirName = file.DirectoryName;
            var outName = project.Properties.Single(prop1 => prop1.Name.Equals(ProjectItemTypes.PropOutputPath)).EvaluatedValue;
            var fileName = project.Properties.Single(prop2 => prop2.Name.Equals(ProjectItemTypes.PropOutputFileName)).EvaluatedValue;
            var temp = dirName + @"\" + outName + fileName;
            this.BuildProjectOutputPath = new FileInfo(file.DirectoryName + @"\" + project.Properties.Single(prop1 => prop1.Name.Equals(ProjectItemTypes.PropOutputPath)).EvaluatedValue + project.Properties.Single(prop2 => prop2.Name.Equals(ProjectItemTypes.PropOutputFileName)).EvaluatedValue);

            this.ProjectClassPaths = project.Items.Where(item => item.ItemType.Equals(ProjectItemTypes.CompilePath)).Select(item => new FileInfo(file.DirectoryName + "\\" + item.EvaluatedInclude)).ToList();

            var refs = project.Items.Where(item => item.ItemType.Equals(ProjectItemTypes.Reference)).Where(refItem => refItem.Metadata.Any() && refItem.Metadata.Where(meta => meta.Name.Equals("HintPath")).Any()).Select(meta2 =>meta2.Metadata.Single(x => x.Name.Equals("HintPath"))); // Select(meta2 => meta2 // Select(fileInf => new FileInfo(fileInf.Metadata.Where(x => x.Name.Equals("HintPath")).Select(metaPath => metaPath.)
            this.ReferencePaths = refs.Select(item => new FileInfo(Path.Combine(file.DirectoryName, item.EvaluatedValue))).ToList();
        }

        ////public static void PopulateReferenceProjects(List<ProjectFile> projectFiles)
        ////{
        ////    foreach (var proj in projectFiles)
        ////    {
        ////        var refPaths = proj.ReferencePaths.Select(x => FileHelper.NormalizePath(x.FullName)).ToList();
        ////        proj.ReferenceProjects = projectFiles.Where(x => refPaths.Contains(FileHelper.NormalizePath(x.BuildProjectOutputPath.FullName))).ToList();
        ////    }
        ////}

        public static List<ProjectFile> GetCorrectBuildOrder(List<ProjectFile> projectFiles)
        {
            var retProjFiles = new List<ProjectFile>();
            while (projectFiles.Count != 0)
            {
                ProjectFile proj = projectFiles.Find(x => x.ReferenceProjects.Count == 0 || x.ReferenceProjects.TrueForAll(y => retProjFiles.Contains(Projects.ProjectList[y])));
                retProjFiles.Add(proj);
                projectFiles.Remove(proj);
            }

            projectFiles = retProjFiles;
            return retProjFiles;
        }

        public static void PopulateNeedsToBeBuilt(List<int> projectList)
        {
            projectList.RunFuncForEach(x => ProjectFile.PopulateNeedsToBeBuilt(Projects.ProjectList[x]));
        }

        public static void PopulateNeedsToBeBuilt(ProjectFile projectFile)
        {
            var curProjBuiltTime = projectFile.BuildProjectOutputPath.LastWriteTime;
            projectFile.NeedsToBeBuilt = File.Exists(projectFile.BuildProjectOutputPath.FullName) ? projectFile.ProjectClassPaths.Any(classFile => classFile.LastWriteTime > curProjBuiltTime) : true;

            //if (projectFile.ReferenceProjects.Count == 0)
            //{
            //    projectFile.ReadyToBuild = true; // no ref's to check
            //}
        }

        //public static void PopulateReadyToBuild(List<int> projectList)
        //{
        //    foreach (var projIndex in projectList)
        //    {
        //        var proj = Projects.ProjectList[projIndex];

        //    }

        //}

        public static void PopulateNeedsToBeBuiltTwo(ProjectFile projectFile)
        {
            
            var curProjBuiltTime = projectFile.BuildProjectOutputPath.LastWriteTime;
            var curProjNeedsToBeBuilt = File.Exists(projectFile.BuildProjectOutputPath.FullName) ? projectFile.ProjectClassPaths.Any(classFile => classFile.LastWriteTime > curProjBuiltTime) : true;
            projectFile.ReferenceProjects.Where(proj1 => Projects.ProjectList[proj1].NeedsToBeBuilt == null).RunFuncForEach(proj => ProjectFile.PopulateNeedsToBeBuilt(Projects.ProjectList[proj]));//.Where(x => x.NeedsToBeBuilt.Value).Any();
            var anyRefProjsNeedingBuild = projectFile.ReferenceProjects.Where(x => Projects.ProjectList[x].NeedsToBeBuilt.Value).Any();
            projectFile.NeedsToBeBuilt = (curProjNeedsToBeBuilt || anyRefProjsNeedingBuild);
            
        }
    }
}
