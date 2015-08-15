using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace BuildSolution
{
    class ProjectFile
    {
        public ProjectFile()
        {

        }

        public ProjectFile(string filepath)
        {
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
