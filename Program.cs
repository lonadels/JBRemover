using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using static System.IO.Directory;
using static System.IO.File;
using Microsoft.Win32;


namespace JBRemover
{
    internal static class Program
    {
        private static void Main()
        {
            Console.WriteLine(
                $"Welcome to JetBrains License Remover Tool {Assembly.GetExecutingAssembly().GetName().Version}");

            Start();
        }

        private static void Start()
        {
            while (true)
            {
                var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var jbPath = Path.Combine(appData, "JetBrains");

                if (!Directory.Exists(jbPath)) throw new PathNotFoundException("JetBrains not installed to AppData");

                var projects = new[] {"DataGrip", "IntelliJIdea", "Rider", "WebStorm", "PhpStorm", "CLion"};

                var availableProjects = new List<Project>()!;

                foreach (var fullPath in GetDirectories(jbPath).ToList()
                    .Select(path => Path.Combine(jbPath, path)))
                {
                    Debug.WriteLine(fullPath);
                    if (!GetAttributes(fullPath).HasFlag(FileAttributes.Directory)) continue;

                    availableProjects.AddRange(from project in projects
                        where new DirectoryInfo(fullPath).Name.ToLower().StartsWith(project.ToLower())
                        select new Project {Name = project, Path = fullPath});
                }

                switch (availableProjects.Count)
                {
                    case 0:
                        throw new NullReferenceException("Not found available JetBrains projects");
                    case 1:
                        var firstProject = availableProjects.FirstOrDefault();
                        Console.WriteLine($"Found {firstProject?.Name}");
                        RemoveLicense(firstProject);
                        break;
                    case > 1:
                        Console.WriteLine("Please, select JetBrains project from list:");

                        foreach (var it in availableProjects.Select((x, i) => new {Project = x, Index = i}))
                            Console.WriteLine($"{it.Index + 1}. {it.Project.Name}");

                        short index;

                        do
                        {
                            Console.Write("> ");
                            var enter = Convert.ToChar(Console.ReadLine()?.First());

                            if (!char.IsNumber(enter)) continue;

                            index = Convert.ToInt16(char.GetNumericValue(enter));

                            if (index < 1 || index > availableProjects.Count) continue;

                            break;
                        } while (true);

                        RemoveLicense(availableProjects.ElementAt(index - 1));

                        break;
                }
            }
        }

        private static bool Confirmation(string message)
        {
            do
            {
                Console.Write($"{message} (Y/n): ");
                var enter = Convert.ToChar(Console.ReadLine()?.ToLower().First());

                if (!char.IsLetter(enter)) continue;
                if (enter != 'y' && enter != 'n') continue;

                return enter == 'y';
            } while (true);
        }

        private static void RemoveLicense(Project? project)
        {
            if (project is null)
                throw new NullReferenceException("Invalid project");

            if (!Confirmation($"Remove license from {project.Name}?")) return;

            if (project.Path is null) return;

            foreach (var file in GetFiles(project.Path))
            {
                var path = Path.Combine(project.Path, file);
                if (Path.GetExtension(path).ToLower() is not "key") continue;
                File.Delete(path);
                break;
            }

            var key = GetFiles(project.Path).FirstOrDefault(file => Path.GetExtension(file).ToLower() is "key");
            if (!string.IsNullOrEmpty(key))
            {
                Console.WriteLine($"Deleting {key}...");
                File.Delete(Path.Combine(project.Path, key));
            }

            var eval = Path.Combine(project.Path, "eval");
            if( Directory.Exists(eval))
            {
                Console.WriteLine("Deleting eval...");
                Delete(eval, true);
            } 

            const string jbPrefPath = @"Software\JavaSoft\Prefs\jetbrains";

            using var prefKey = Registry.CurrentUser.OpenSubKey(jbPrefPath, true);

            if (prefKey != null && key != null)
            {
                Console.WriteLine($"Deleting {jbPrefPath}/{Path.GetFileNameWithoutExtension(key)}...");
                prefKey.DeleteSubKeyTree(Path.GetFileNameWithoutExtension(key)!);
            }
            
            const string jbRegPath = @"Software";

            using var jbKey = Registry.CurrentUser.OpenSubKey(jbRegPath, true);

            if (jbKey != null)
            {
                Console.WriteLine($"Deleting {jbRegPath}/JetBrains...");
                jbKey.DeleteSubKeyTree("JetBrains");
            }

            Console.WriteLine("License successfully removed!");
        }
    }
}