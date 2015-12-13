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
        /// <summary>
        /// C#'s ProjectFiles have metadata that have string types. Use to get the paritcular type
        /// </summary>
        class ProjectItemTypes
        {
            public static readonly string BuildOutputPath = "BuiltProjectOutputGroupKeyOutput";
            public static readonly string CompilePath = "Compile";
            public static readonly string Reference = "Reference";
            public static readonly string PropOutputPath = "OutputPath";
            public static readonly string PropOutputFileName = "TargetFileName";
            public static readonly string ReferenceProject = "ProjectReference";
        }

        public FileInfo ProjectPath { get; set; }

        public FileInfo BuildProjectOutputPath { get; set; }

        public List<FileInfo> ProjectClassPaths { get; set; }

        public List<FileInfo> ReferencePaths { get; set; } = new List<FileInfo>();

        public string ReferenceCompileArg { get; set; }

        public string TargetCompileArg { get; set; }

        public List<int> ReferenceProjects { get; set; } // index to the project in the array of projects

        public bool? NeedsToBeBuilt { get; set; }

        public bool ReadyToBuild { get; set; } = false;// default false since we initially have no idea if proj is ready to build without checking refs

        public bool? HasBuilt { get; set; }

        public ProjectFile(FileInfo file)
        {
            Project project = new Project(file.FullName); // item = metadata , directMetadata count > 0 . metadata[x] where name = HintPath then evaluatedInlcude of that

            this.ProjectPath = file;
            var dirName = file.DirectoryName;
            var outName = project.Properties.Single(prop1 => prop1.Name.Equals(ProjectItemTypes.PropOutputPath)).EvaluatedValue;
            var fileName = project.Properties.Single(prop2 => prop2.Name.Equals(ProjectItemTypes.PropOutputFileName)).EvaluatedValue;
            var temp = dirName + @"\" + outName + fileName;
            this.BuildProjectOutputPath = new FileInfo(file.DirectoryName + @"\" + project.Properties.Single(prop1 => prop1.Name.Equals(ProjectItemTypes.PropOutputPath)).EvaluatedValue + project.Properties.Single(prop2 => prop2.Name.Equals(ProjectItemTypes.PropOutputFileName)).EvaluatedValue);
            this.TargetCompileArg =  this.BuildProjectOutputPath.Extension.Equals(".dll") ? "/target:library" : "/target:exe";

            this.ProjectClassPaths = project.Items.Where(item => item.ItemType.Equals(ProjectItemTypes.CompilePath)).Select(item => new FileInfo(file.DirectoryName + "\\" + item.EvaluatedInclude)).ToList();
            var projDlls = project.Items.Where(item => item.ItemType.Equals(ProjectItemTypes.Reference)).ToList();

            var projRefs = project.Items.Where(item => item.ItemType.Equals(ProjectItemTypes.ReferenceProject)).ToList();
            foreach (var rp in projRefs)
            {
                Project proj = new Project(Path.Combine(file.DirectoryName, rp.EvaluatedInclude));
                var projDlls2 = proj.Items.Where(item => item.ItemType.Equals(ProjectItemTypes.Reference)).ToList();
                if (projDlls2.Any() ) projDlls.AddRange(projDlls2);
            }

            foreach (var r in projDlls.Distinct())
            {
                if (r.Metadata.Any() && r.Metadata.Where(x => x.Name.Equals("HintPath")).Any())
                {
                    r.Metadata.Where(meta => meta.Name.Equals("HintPath")).
                        RunFuncForEach(x =>
                        {
                            var refFile = new FileInfo(Path.Combine(file.DirectoryName, x.EvaluatedValue));

                            if (File.Exists(refFile.FullName))
                            {
                                this.ReferencePaths.Add(refFile);
                                this.ReferenceCompileArg += " /r:" + "\"" + refFile.FullName + "\"";
                            }
                            else
                            {
                                var tempRef = System.Reflection.Assembly.LoadWithPartialName(r.EvaluatedInclude);
                                if (tempRef != null)
                                {
                                    this.ReferenceCompileArg += " /r:" + "\"" + tempRef.Location + "\"";
                                }
                            }
                        });
                }
                else
                {
                    var tempRef = System.Reflection.Assembly.LoadWithPartialName(r.EvaluatedInclude);
                    if (tempRef != null)
                    {
                        this.ReferenceCompileArg += " /r:" + "\"" + tempRef.Location + "\"";
                    }
                }
            }
        }

        public static void PopulateNeedsToBeBuilt(List<int> projectList)
        {
            projectList.RunFuncForEach(x => ProjectFile.PopulateNeedsToBeBuilt(Projects.ProjectList[x]));
        }

        public static void PopulateNeedsToBeBuilt(ProjectFile projectFile)
        {
            var curProjBuiltTime = projectFile.BuildProjectOutputPath.LastWriteTime;
            projectFile.NeedsToBeBuilt = File.Exists(projectFile.BuildProjectOutputPath.FullName) ? projectFile.ProjectClassPaths.Any(classFile => classFile.LastWriteTime > curProjBuiltTime) : true;
        }
    }
}
