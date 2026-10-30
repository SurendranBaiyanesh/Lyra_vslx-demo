using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace DependencyDeployer
{
    public sealed class Dependency
    {
        public string Name { get; set; }
        public string Version { get; set; }
    }

    /// <summary>
    /// Reads dependency.xml:
    /// &lt;Dependencies&gt;&lt;Dependency Name="Foo.dll" Version="1.2.3" /&gt;&lt;/Dependencies&gt;
    /// </summary>
    public static class DependencyManifest
    {
        // second name matches the spelling used in the original requirement
        public static readonly string[] FileNames = { "dependency.xml", "depandancy.xml" };

        public static string Find(string projectDir) =>
            FileNames.Select(f => Path.Combine(projectDir, f)).FirstOrDefault(File.Exists);

        public static List<Dependency> Load(string path)
        {
            var deps = XDocument.Load(path).Root.Elements("Dependency")
                .Select(e => new Dependency { Name = (string)e.Attribute("Name"), Version = (string)e.Attribute("Version") })
                .ToList();

            foreach (var d in deps)
            {
                if (string.IsNullOrWhiteSpace(d.Name) || string.IsNullOrWhiteSpace(d.Version))
                    throw new InvalidDataException($"{path}: every <Dependency> needs Name and Version.");
                // the name becomes a file path, so refuse anything that could escape the target folder
                if (d.Name != Path.GetFileName(d.Name))
                    throw new InvalidDataException($"{path}: invalid dependency name '{d.Name}'.");
            }
            return deps;
        }
    }
}
