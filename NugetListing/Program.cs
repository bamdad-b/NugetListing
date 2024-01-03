using Ionic.Zip;
using System.Xml;

namespace NugetListing
{
    internal class Program
    {
        static HashSet<string> packageNames = new();
        static string packagesPath = @"C:\Users\Afagh B\.nuget\packages";
        static void Main(string[] args)
        {
            string path = GetInput(".csproj file path (0 to start):");
            HashSet<(string, string)> projectPackages = new();
            while (path != "0")
            {
                foreach (var p in GetPackage(path))
                    projectPackages.Add(p);
                path = GetInput("another: ");
            }

            foreach (var package in projectPackages)
                ProcessPackage(package.Item1, package.Item2);

            Console.WriteLine("Done getting packages.");

            foreach (var package in packageNames)
                Console.WriteLine(package);

            Console.WriteLine("Press enter to deploy zip.");
            Console.ReadLine();

            CreateZipFile(@"D:\temp\packs.zip", packageNames);

            Console.WriteLine();
            Console.WriteLine("Done.");
            Console.ReadLine();
        }

        static IEnumerable<(string, string)> GetPackage(string projectPath)
        {
            XmlDocument xml = new();
            xml.Load(projectPath);
            var itemGroups = xml.GetElementsByTagName("ItemGroup").OfType<XmlElement>();
            var packageReferences = itemGroups.SelectMany(itemGroup => itemGroup.ChildNodes.OfType<XmlElement>()).Where(el => el.Name == "PackageReference");
            var packages = packageReferences.Select(x => (x.Attributes["Include"]!.Value, x.Attributes["Version"]!.Value));
            return packages;
        }

        static string GetInput(string prompt)
        {
            string? input = null;
            while (input == null)
            {
                Console.WriteLine(prompt);
                input = Console.ReadLine();
            }
            return input;
        }

        public static void ProcessPackage(string name, string version, int depth = 0)
        {
            if (depth == 1)
                return;

            var path = Path.Combine(packagesPath, name, version);
            if (!Directory.Exists(path))
            {
                path = Path.Combine(packagesPath, name);
                if (!Directory.Exists(path))
                {
                    Console.WriteLine($"Error: Package not found [{name} {version}]");
                    return;
                }
                var alternatives = Directory.GetDirectories(path);
                if (!alternatives.Any())
                {
                    Console.WriteLine($"Error: Package not found [{name} {version}]");
                    return;
                }
                foreach (var alternative in alternatives)
                {
                    ProcessPackage(name, Path.GetFileName(alternative), depth);
                }
            }
            if (packageNames.Add(path))
                GetDependencies(Path.Combine(path, $"{name}.nuspec"), depth++);
        }

        public static void GetDependencies(string nuspecFile, int depth = 0)
        {
            if (!File.Exists(nuspecFile))
                return;

            XmlDocument xmlDocument = new();
            xmlDocument.Load(nuspecFile);   

            var dependencies = xmlDocument["package"]?["metadata"]?["dependencies"];
            dependencies = dependencies?["group"] ?? dependencies;
            if (dependencies != null)
            {
                var dependencyList = dependencies.ChildNodes.OfType<XmlElement>().Where(x => x.Name == "dependency").Select(x => (x.Attributes["id"]!.Value, x.Attributes["version"]!.Value));
                foreach (var dependency in dependencyList)
                    ProcessPackage(dependency.Item1, dependency.Item2, depth);
            }
        }

        public static void CreateZipFile(string fileName, IEnumerable<string> files)
        {
            Console.WriteLine();
            using (ZipFile zip = new ZipFile())
            {
                //zip.UseUnicodeAsNecessary = true;  // utf-8
                foreach (var file in files)
                {
                    try
                    {
                        zip.AddDirectory(file, file.Remove(0, 33));
                    }
                    catch(Exception ex) { Console.Write("E"); }
                    Console.Write("*");
                }

                Console.WriteLine();
                zip.SaveProgress += (o, s) =>
                {
                    Console.CursorLeft = 0;
                    if (s.EventType == ZipProgressEventType.Saving_AfterWriteEntry)
                        Console.Write($"{s.EntriesSaved}/{s.EntriesTotal}             ");
                };

                zip.Save(fileName);
            }
        }
    }
}