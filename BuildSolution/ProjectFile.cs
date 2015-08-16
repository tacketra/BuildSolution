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
        }

        public FileInfo ProjectPath { get; set; }

        public FileInfo BuildProjectOutputPath { get; set; }

        public List<FileInfo> ProjectClassPaths { get; set; }

        public List<FileInfo> ReferencePaths { get; set; }

        public List<ProjectFile> ReferenceProjects { get; set; }

        public ProjectFile()
        {

        }

        public ProjectFile(FileInfo file)
        {
            Project project = new Project(file.FullName); // item = metadata , directMetadata count > 0 . metadata[x] where name = HintPath then evaluatedInlcude of that
            this.ProjectPath = new FileInfo(project.FullPath);
            
            this.BuildProjectOutputPath = new FileInfo(project.Items.Single(item => item.ItemType.Equals(ProjectItemTypes.BuildOutputPath)).EvaluatedInclude); // EvaluatedInclude

            this.ProjectClassPaths = project.Items.Where(item => item.ItemType.Equals(ProjectItemTypes.CompilePath)).Select(item => new FileInfo(file.DirectoryName + item.EvaluatedInclude)).ToList();

            var refs = project.Items.Where(item => item.ItemType.Equals(ProjectItemTypes.Reference)).Where(refItem => refItem.Metadata.Any() && refItem.Metadata.Where(meta => meta.Name.Equals("HintPath")).Any()).Select(meta2 =>meta2.Metadata.Single(x => x.Name.Equals("HintPath"))); // Select(meta2 => meta2 // Select(fileInf => new FileInfo(fileInf.Metadata.Where(x => x.Name.Equals("HintPath")).Select(metaPath => metaPath.)
            this.ReferencePaths = refs.Select(item => new FileInfo(file.DirectoryName + item.EvaluatedValue)).ToList();

            var hello = "hello";
        }

        public static void PopulateReferenceProjects(List<ProjectFile> projectFiles)
        {
            foreach (var proj in projectFiles)
            {
                var refPaths = proj.ReferencePaths.Select(x => x.FullName).ToList();
                var stuff = projectFiles.Where(x => refPaths.Contains(x.BuildProjectOutputPath.FullName)).ToList();
                proj.ReferenceProjects = stuff;
            }
        }

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
