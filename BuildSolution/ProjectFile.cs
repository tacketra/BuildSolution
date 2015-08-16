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

        public List<ProjectFile> ReferenceProjects { get; set; }

        public bool? NeedsToBeBuilt { get; set; }

        public ProjectFile()
        {

        }

        public ProjectFile(FileInfo file)
        {
            Project project = new Project(file.FullName); // item = metadata , directMetadata count > 0 . metadata[x] where name = HintPath then evaluatedInlcude of that
            this.ProjectPath = new FileInfo(project.FullPath);

            ////this.BuildProjectOutputPath = new FileInfo(project.Items.Single(item => item.ItemType.Equals(ProjectItemTypes.BuildOutputPath)).EvaluatedInclude); // EvaluatedInclude
            var dirName = file.DirectoryName;
            var outName = project.Properties.Single(prop1 => prop1.Name.Equals(ProjectItemTypes.PropOutputPath)).EvaluatedValue;
            var fileName = project.Properties.Single(prop2 => prop2.Name.Equals(ProjectItemTypes.PropOutputFileName)).EvaluatedValue;
            var temp = dirName + "\\" + outName + fileName;
            this.BuildProjectOutputPath = new FileInfo(file.DirectoryName + "\\" + project.Properties.Single(prop1 => prop1.Name.Equals(ProjectItemTypes.PropOutputPath)).EvaluatedValue + project.Properties.Single(prop2 => prop2.Name.Equals(ProjectItemTypes.PropOutputFileName)).EvaluatedValue);

            this.ProjectClassPaths = project.Items.Where(item => item.ItemType.Equals(ProjectItemTypes.CompilePath)).Select(item => new FileInfo(file.DirectoryName + "\\" + item.EvaluatedInclude)).ToList();

            var refs = project.Items.Where(item => item.ItemType.Equals(ProjectItemTypes.Reference)).Where(refItem => refItem.Metadata.Any() && refItem.Metadata.Where(meta => meta.Name.Equals("HintPath")).Any()).Select(meta2 =>meta2.Metadata.Single(x => x.Name.Equals("HintPath"))); // Select(meta2 => meta2 // Select(fileInf => new FileInfo(fileInf.Metadata.Where(x => x.Name.Equals("HintPath")).Select(metaPath => metaPath.)
            this.ReferencePaths = refs.Select(item => new FileInfo(Path.Combine(file.DirectoryName, item.EvaluatedValue))).ToList();

            var hello = "hello";
        }

        public static void PopulateReferenceProjects(List<ProjectFile> projectFiles)
        {
            foreach (var proj in projectFiles)
            {
                var refPaths = proj.ReferencePaths.Select(x => FileHelper.NormalizePath(x.FullName)).ToList();
                proj.ReferenceProjects = projectFiles.Where(x => refPaths.Contains(FileHelper.NormalizePath(x.BuildProjectOutputPath.FullName))).ToList();
            }
        }

        public static void PopulateNeedsToBeBuilt(ProjectFile projectFile)
        {
            var curProjBuiltTime = projectFile.BuildProjectOutputPath.LastWriteTime;
            var curProjNeedsToBeBuilt = File.Exists(projectFile.BuildProjectOutputPath.FullName) ? projectFile.ProjectClassPaths.Any(classFile => classFile.LastWriteTime > curProjBuiltTime) : true;
            projectFile.ReferenceProjects.Where(proj1 => proj1.NeedsToBeBuilt == null).RunFuncForEach(proj => ProjectFile.PopulateNeedsToBeBuilt(proj));//.Where(x => x.NeedsToBeBuilt.Value).Any();
            var anyRefProjsNeedingBuild = projectFile.ReferenceProjects.Where(x => x.NeedsToBeBuilt.Value).Any();
            projectFile.NeedsToBeBuilt = (curProjNeedsToBeBuilt || anyRefProjsNeedingBuild);
        }

        // deleteme
        public static string GetCSProjOutputPath(string file)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(file);
            //// var propGroupNodes = doc.DocumentElement.SelectNodes("/PropertyGroup");
            var nodes = doc.DocumentElement.ChildNodes;

            foreach (XmlNode node in nodes)
            {
                if (node.OuterXml.Contains("Debug|"))// && !node.InnerXml.Contains("Release") && node.Attributes.Count > 0)
                {
                    var innerChildNodes = node.ChildNodes;
                    foreach (XmlNode innerNode in innerChildNodes)
                    {
                        if (innerNode.Name.Equals("OutputPath"))
                        {
                            return innerNode.InnerText;
                        }

                    }
                }
            }

            return string.Empty;
        }
    }
}
