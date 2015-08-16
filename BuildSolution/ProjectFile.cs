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
        }

        public FileInfo ProjectPath { get; set; }

        public FileInfo BuildProjectOutputPath { get; set; }

        public List<FileInfo> ProjectClassPaths { get; set; }

        public ProjectFile()
        {

        }

        public ProjectFile(FileInfo file)
        {
            Project project = new Project(file.FullName);
            this.ProjectPath = new FileInfo(project.FullPath);
            
            this.BuildProjectOutputPath = new FileInfo(project.Items.Single(item => item.ItemType.Equals(ProjectItemTypes.BuildOutputPath)).EvaluatedInclude); // EvaluatedInclude

            this.ProjectClassPaths = project.Items.Where(item => item.ItemType.Equals(ProjectItemTypes.CompilePath)).Select(item => new FileInfo(file.DirectoryName + item.EvaluatedInclude)).ToList();
            var hello = "hello";
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
