using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Features
{
    public static class QmlResourceManager
    {
        public static async Task<(string, SystemItemDisposableCollection)> BuildResources(string initQml, string[] args)
        {
            // Add logic here to determine if assembly is a WPF app. Currently the assumption is that
            // we are not running a WPF app.
            var isWpfApp = default(bool);

            return isWpfApp 
                ? await BuildResourcesFromWpfAssembly(initQml, args)
                : await BuildResourceFromAssembly(initQml, args);
        }

        public static async Task<(string, SystemItemDisposableCollection)> BuildResourceFromAssembly(string initQml, string[] args)
        {
            var mainResourceName = Assembly.GetExecutingAssembly()
                .GetManifestResourceNames()[0].Split(".")[0];

            var resourcesList = Assembly.GetExecutingAssembly()
                .GetManifestResourceNames()
                .Select(x => {
                    var name = x.Replace(mainResourceName + ".", "");
                    var ext = new FileInfo(name).Extension;

                    var tempSomething = name.Replace(ext, "");
                    return name.Replace(ext, "").Replace(".", "/") + ext;
                })
                .ToList();

            var resourceDirectoryName = Path.Combine(Environment.CurrentDirectory, "~$" + mainResourceName);

            // Build resource directory paths
            var alreadyExists = Directory.Exists(resourceDirectoryName);

            Directory.CreateDirectory(resourceDirectoryName);
            File.SetAttributes(resourceDirectoryName, FileAttributes.Hidden);

            // Get and build directory tree
            resourcesList
                .Select(item =>
                {
                    var path = Path.GetDirectoryName(item);
                    var parts = path.Split(Environment.CurrentDirectory + "\\");
                    return parts.Length > 1 ? parts[1] : "";
                }).Where(x => !string.IsNullOrEmpty(x))
                .Distinct()
                .ToList()
                .ForEach(dir => Directory.CreateDirectory($"{resourceDirectoryName}\\{dir}"));

            // initiate disposable collection list
            var disposables = new List<SystemItem>();
            await Task.Run(() =>
            {
                // Get content from master resource list
                resourcesList.ForEach(resourceName =>
                {
                    var pathName = $"{resourceDirectoryName}\\{resourceName.Replace("/", "\\")}";
                    var bytes = GetResourceContentAsBytes(resourceName);

                    if (resourceName.Contains(".png"))
                    {
                        using MemoryStream ms = new MemoryStream(bytes);

                        var image = Image.FromStream(ms);
                        image.Save(pathName);
                        disposables.Add(new SystemItem(image, pathName));
                    }
                    else
                    {
                        var content = Encoding.UTF8.GetString(bytes, 0, bytes.Length);

                        File.Create(pathName).Close();
                        File.WriteAllText(pathName, content);
                        disposables.Add(new SystemItem(pathName));
                    }
                });
            });

            var collection = new SystemItemDisposableCollection(disposables, resourceDirectoryName)
            {
                KeepFiles = args.Any(x => x == "--keep") || alreadyExists
            };

            return ($"{resourceDirectoryName}\\{initQml}",  collection);
        }

        public static async Task<(string, SystemItemDisposableCollection)> BuildResourcesFromWpfAssembly(string initQml, string[] args)
        {
            var mainResourceName = Assembly.GetExecutingAssembly()
                .GetManifestResourceNames()
                .First()
                .Replace(".resources", "");

            var resourcesList = new ResourceManager(mainResourceName, Assembly.GetExecutingAssembly())
                .GetResourceSet(CultureInfo.CurrentUICulture, true, true)
                .Cast<DictionaryEntry>()
                .Select(item => item.Key.ToString())
                .ToList();

            var resourceDirectoryName = Path.Combine(Environment.CurrentDirectory, "~$" + mainResourceName.Replace(".g", ""));

            // Build the main resource directory path and any children directories
            var alreadyExists = Directory.Exists(resourceDirectoryName);

            Directory.CreateDirectory(resourceDirectoryName);
            File.SetAttributes(resourceDirectoryName, FileAttributes.Hidden);
            GetResourcePathsForWpf(mainResourceName).ForEach(x => Directory.CreateDirectory($"{resourceDirectoryName}\\{x}"));

            // initiate disposable collection list
            var disposables = new List<SystemItem>();
            await Task.Run(() =>
            {
                // Get content from master resource list
                var wpfMasterResourceList = "resources.txt";

                var bytes = GetResourceContentAsBytes(wpfMasterResourceList);
                var knownResourceNames = Encoding.UTF8.GetString(bytes, 0, bytes.Length)
                    .Split("\r\n")
                    .ToDictionary(x => x.ToLower(), x => x);

                resourcesList.ForEach(resourceName =>
                {
                    var tempName = resourceName.Replace("/", @"\");
                    var pathName = $"{resourceDirectoryName}\\{resourceName.Replace("/", "\\")}";
                    if (knownResourceNames.ContainsKey(tempName))
                    {
                        var name = knownResourceNames[tempName];
                        pathName = $"{resourceDirectoryName}\\{name.Replace("/", "\\")}";
                    }

                    var bytes = GetResourceContentAsBytes(resourceName);
                    if (resourceName.Contains(".png"))
                    {
                        using MemoryStream ms = new MemoryStream(bytes);

                        var image = Image.FromStream(ms);
                        image.Save(pathName);
                        disposables.Add(new SystemItem(image, pathName));
                    }
                    else
                    {
                        var content = Encoding.UTF8.GetString(bytes, 0, bytes.Length);

                        File.Create(pathName).Close();
                        File.WriteAllText(pathName, content);
                        disposables.Add(new SystemItem(pathName));
                    }
                });
            });

            var collection = new SystemItemDisposableCollection(disposables, resourceDirectoryName)
            {
                KeepFiles = args.Any(x => x == "--keep") || alreadyExists
            };

            return ($"{resourceDirectoryName}\\{initQml.ToLower()}",  collection);
        }

        private static List<string> GetResourcePathsForWpf(string resourceName)
        {
            var resourceManager = new ResourceManager(resourceName, Assembly.GetExecutingAssembly());
            var resources = resourceManager.GetResourceSet(CultureInfo.CurrentUICulture, true, true);

            return resources.Cast<DictionaryEntry>()
                .Select(item =>
                {
                    var entry = item.Key.ToString();
                    var path = new DirectoryInfo(entry).Parent.FullName;
                    var parts = path.Split(Environment.CurrentDirectory + "\\");

                    return parts.Length > 1 ? parts[1] : "";
                }).Where(x => !string.IsNullOrEmpty(x))
                .Distinct()
                .ToList();
        }

        private static byte[] GetResourceContentAsBytes(string uriPath)
        {
            // From here: https://bit.ly/2JXcWHS
            var streamResourceInfo = Application.GetResourceStream(new Uri(uriPath, UriKind.Relative));

            byte[] bytes = { };
            if (streamResourceInfo != null)
            {
                var length = streamResourceInfo.Stream.Length;
                bytes = new byte[length];
                streamResourceInfo.Stream.Read(bytes, 0, (int)length);
            }

            return bytes;
        }
    }
}