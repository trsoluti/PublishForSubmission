using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEngine;


namespace UnityEditor.TRSolutions.Publish
{
    public class Publisher : ScriptableObject {
        DirectoryInfo projectRoot;
        DirectoryInfo parent;
        string parentName;
        string targetFolderName;
        DirectoryInfo buildFolder;
        string recordingsFolderName;
        string documentationFolderName;

#if UNITY_EDITOR
        [MenuItem("File/Publish")]
        static void Publish() {
            Publisher.CreateInstance<Publisher>().PublishEverything();
        }

        private void Awake()
        {
#else
        public Publisher() {
#endif
            projectRoot = new DirectoryInfo(Directory.GetCurrentDirectory());
            parent = projectRoot.Parent;
            parentName = parent?.FullName ?? "";
            targetFolderName = projectRoot.Name + " Package";
            buildFolder = GetBuildFolder(projectRoot);
            recordingsFolderName = "Recordings";
            documentationFolderName = "Documentation";
        }

        private void PublishEverything()
        {
            // Find the total # of files we will be publishing
            // Passing null to the methods causes them to just
            // enumerate the number of files.
            int totalFilesToCopy = 0;
            if (buildFolder != null)
            {
                foreach (var _obj in ZipBuildFiles(null))
                {
                    totalFilesToCopy += 1;
                }
            }
            foreach (var _obj in ZipSourceFiles(null))
            {
                totalFilesToCopy += 1;
            }
            if (Directory.Exists("Recordings"))
            {
                foreach (var _obj in CopyRecordings(null))
                {
                    totalFilesToCopy += 1;
                }
            }
            if (Directory.Exists("Documentation"))
            {
                foreach (var _obj in CopyDocumentation(null))
                {
                    totalFilesToCopy += 1;
                }
            }
            Debug.Log($"Total files to copy: {totalFilesToCopy}");

            // Now do the copying for real, using a percentage of the total
            var target = Path.Combine(parentName, targetFolderName);
            Debug.Log(string.Format("Project will be published to {0}", target));
            Directory.CreateDirectory(target);

            int totalFilesCopied = 0;

            // Copy the build files
            if (buildFolder != null)
            {
                var buildTarget = Path.Combine(target, "Build.zip");
                using FileStream zipToOpen = new FileStream(buildTarget, FileMode.Create);
                using ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Create);
                foreach (var _obj in ZipBuildFiles(archive))
                {
                    totalFilesCopied++;
                    EditorUtility.DisplayProgressBar($"publishing {projectRoot.Name}", "Copying build files", (float)totalFilesCopied / (float)totalFilesToCopy);
                }
            }

            // Copy the source files
            var projectTarget = Path.Combine(target, projectRoot.Name + ".zip");
            using (FileStream zipToOpen = new FileStream(projectTarget, FileMode.Create))
            {
                using ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Create);
                var ignoreParser = new IgnoreParser();
                //+ print($"IgnoreParser:\n{ignoreParser}");
                foreach (var _obj in ZipSourceFiles(archive))
                {
                    totalFilesCopied++;
                    EditorUtility.DisplayProgressBar($"publishing {projectRoot.Name}", "Copying source files", (float)totalFilesCopied / (float)totalFilesToCopy);
                }
            }

            // Copy the recordings
            if (Directory.Exists("Recordings"))
            {
                foreach (var _obj in CopyRecordings(target))
                {
                    totalFilesCopied++;
                    EditorUtility.DisplayProgressBar($"publishing {projectRoot.Name}", "Copying recordings", (float)totalFilesCopied / (float)totalFilesToCopy);
                }
            }

            // Copy the documentation
            if (Directory.Exists("Documentation"))
            {
                foreach (var _obj in CopyDocumentation(target))
                {
                    totalFilesCopied++;
                    EditorUtility.DisplayProgressBar($"publishing {projectRoot.Name}", "Copying documentation", (float)totalFilesCopied / (float)totalFilesToCopy);
                }
            }
            EditorUtility.ClearProgressBar();
        }

        private static DirectoryInfo GetBuildFolder(DirectoryInfo projectRoot)
        {
            foreach (var fileInfo in projectRoot.EnumerateDirectories())
            {
                switch (fileInfo.Name)
                {
                    case "Build":
                    case "Builds":
                    case "build":
                    case "builds": return fileInfo;
                }
            }
            return null;
        }

        private static IEnumerable<Tuple<FileSystemInfo, string>> EnumerateFileSystemInfoOfFolderWithFilter(string folderRelativePath, DirectoryInfo folder, IgnoreParser ignoreParser)
        {
            foreach (var fileInfo in folder.EnumerateFileSystemInfos("*", SearchOption.TopDirectoryOnly))
            {
                // Construct the path in a format that our git-like ignore function recognizes:
                // - "/" is the root of our project;
                // - Directories end in "/"
                // - Unix-style file separators
                var isDirectory = (File.GetAttributes(fileInfo.FullName) & FileAttributes.Directory) == FileAttributes.Directory;
                var fileRelativePath = folderRelativePath + fileInfo.Name + (isDirectory ? "/" : "");
                //+ Debug.Log($"Processing {fileRelativePath}");
                if (ignoreParser?.IsIgnored(fileRelativePath) == true)
                {
                    //+ Debug.Log($"Skipping {fileRelativePath}");
                }
                else
                {
                    if (isDirectory)
                    {
                        foreach (var info in EnumerateFileSystemInfoOfFolderWithFilter(fileRelativePath, new DirectoryInfo(fileInfo.FullName), ignoreParser))
                        {
                            yield return info;
                        }
                    }
                    else
                    {
                        yield return new Tuple<FileSystemInfo, string>(fileInfo, fileRelativePath);
                    }
                }
            }
        }

        private IEnumerable ZipBuildFiles(ZipArchive zipArchive)
        {
            return ZipFolder(zipArchive, buildFolder, null);
        }

        private IEnumerable ZipSourceFiles(ZipArchive zipArchive)
        {
            return ZipFolder(zipArchive, projectRoot, new IgnoreParser());
        }

        private static IEnumerable ZipFolder(ZipArchive zipArchive, DirectoryInfo folder, IgnoreParser ignoreParser)
        {
            if (folder == null)
            {
                yield break;
            }
            foreach (var (fileInfo, fileRelativePath) in EnumerateFileSystemInfoOfFolderWithFilter("/", folder, ignoreParser))
            {
                if (zipArchive != null)
                {
                    var archivePath = folder.Name + fileRelativePath;
                    //+ Debug.Log($"Compressing {archivePath}");

                    var zipArchiveEntry = zipArchive.CreateEntry(archivePath);
                    using (var zipArchiveStream = zipArchiveEntry.Open())
                    {
                        using (var fileStream = File.OpenRead(fileInfo.FullName))
                        {
                            var buffer = new byte[4096];
                            int bytesRead;
                            do
                            {
                                bytesRead = fileStream.Read(buffer, 0, buffer.Length);
                                zipArchiveStream.Write(buffer, 0, bytesRead);
                            } while (bytesRead > 0);
                        }
                    }
                }
                yield return null;
            }
        }

        private IEnumerable CopyRecordings(string target)
        {
            return CopyFiles(target, new DirectoryInfo(recordingsFolderName));
        }

        private IEnumerable CopyDocumentation(string target)
        {
            return CopyFiles(target, new DirectoryInfo(documentationFolderName));
        }

        private static IEnumerable CopyFiles(string target, DirectoryInfo sourceDirectory)
        {
            foreach (var file in sourceDirectory.EnumerateFiles())
            {
                if (target != null)
                {
                    file.CopyTo(Path.Combine(target, file.Name), overwrite: true);
                }
                yield return null;
            }
        }
    }
}
