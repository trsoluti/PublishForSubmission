/*
 * Publisher class
 * 
 * Copyright (c) 2021 TR Solutions Pte Ltd
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using UnityEngine;


namespace UnityEditor.TRSolutions.Publish
{
    /// <summary>
    /// Class <c>Publisher</c> provides the mechanism to publish a Unity project to a set of zip files.
    /// </summary>
    public class Publisher : ScriptableObject {
        DirectoryInfo projectRoot;
        DirectoryInfo parent;
        string parentName;
        string targetFolderName;
        DirectoryInfo buildFolder;
        string recordingsFolderName;
        string documentationFolderName;

#if UNITY_EDITOR
        /// <summary>
        /// This method is the entry point for one-click publishing
        /// using default locations for the build, recording and documentation
        /// folders.
        /// <summary>
        [MenuItem("File/Publish")]
        static void Publish() {
            Publisher.CreateInstance<Publisher>().PublishEverything();
        }

        private void Awake()
        {
#else
        /// <summary>
        /// Constructs a new Publisher class with defaults
        /// <example>Usually called as follows:
        /// <code>
        ///   new Publisher().PublishEverything();
        /// </code>
        /// </example>
        /// <para>From within the Unity Editor, use the <c>CreateInstance</c> method instead.</para>
        /// <para>The defaults are:
        /// <list type="bullet">
        /// <item><description>project root: current directory</description></item>
        /// <item><description>target root: <c>projectName package</c>(located in the same folder that contains the project)</description></item>
        /// <item><description>build folder: <c>Build</c> or <c>Builds</c></description></item>
        /// <item><description>recordings folder: <c>Recordings</c></description></item>
        /// <item><description>documentation folder: <c>Documentation</c></description></item>
        /// </list></para>
        /// </summary>
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

        /// <summary>
        /// Publishes the source files (zipped) and, if present:
        /// <list type="bullet">
        /// <item><description>executable build (zipped)</description></item>
        /// <item><description>recordings</description></item>
        /// <item><description>documentation</description></item>
        /// </list>
        /// </summary>
        public void PublishEverything()
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
            try
            {
                Directory.CreateDirectory(target);
            }
            catch (Exception e)
            {
                Debug.LogError($"Unable to create target folder! System return error {e}");
                return;
            }
            Debug.Log($"Project will be published to {target}");

            int totalFilesCopied = 0;

            // Copy the build files
            if (buildFolder != null)
            {
                var buildTarget = Path.Combine(target, "Build.zip");
                try
                {
                    using (FileStream zipToOpen = new FileStream(buildTarget, FileMode.Create))
                    {
                        using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Create))
                        {
                            foreach (var _obj in ZipBuildFiles(archive))
                            {
                                totalFilesCopied++;
                                EditorUtility.DisplayProgressBar($"publishing {projectRoot.Name}", "Copying build files", (float)totalFilesCopied / (float)totalFilesToCopy);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Unable to publish build files! System returned error {e}");
                }
            }

            // Copy the source files
            var projectTarget = Path.Combine(target, projectRoot.Name + ".zip");
            try
            {
                using (FileStream zipToOpen = new FileStream(projectTarget, FileMode.Create))
                {
                    using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Create))
                    {
                        var ignoreParser = new IgnoreParser();
                        //+ print($"IgnoreParser:\n{ignoreParser}");
                        foreach (var _obj in ZipSourceFiles(archive))
                        {
                            totalFilesCopied++;
                            EditorUtility.DisplayProgressBar($"publishing {projectRoot.Name}", "Copying source files", (float)totalFilesCopied / (float)totalFilesToCopy);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Unable to publish source code! System returned error {e}");
            }

            // Copy the recordings
            if (Directory.Exists("Recordings"))
            {
                try
                {
                    foreach (var _obj in CopyRecordings(target))
                    {
                        totalFilesCopied++;
                        EditorUtility.DisplayProgressBar($"publishing {projectRoot.Name}", "Copying recordings", (float)totalFilesCopied / (float)totalFilesToCopy);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Unable to publish recordings! System returned error {e}");
                }
            }

            // Copy the documentation
            if (Directory.Exists("Documentation"))
            {
                try
                {
                    foreach (var _obj in CopyDocumentation(target))
                    {
                        totalFilesCopied++;
                        EditorUtility.DisplayProgressBar($"publishing {projectRoot.Name}", "Copying documentation", (float)totalFilesCopied / (float)totalFilesToCopy);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Unable to publish documentation! System returned error {e}");
                }
            }
            EditorUtility.ClearProgressBar();
        }

        /// <summary>
        /// Gets the name of the executable build folder, if present.
        /// </summary>
        /// <param name="projectRoot">The project root directory.</param>
        /// <returns>The build folder, or <c>null</c> if no build folder found</returns>
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

        /// <summary>
        /// Enumerates the name of each non-ignored file in the given folder, descending recursively.
        /// </summary>
        /// <param name="folderRelativePath">The path relative to the project root</param>
        /// <param name="folder">The folder being processed</param>
        /// <param name="ignoreParser">A parser that can tell whether or not to ignore any given file path</param>
        /// <returns>The filesystem info of the next file in the list, along with the file's relative path.</returns>
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

        /// <summary>
        /// Compresses all the executable builds in our project.
        /// </summary>
        /// <param name="zipArchive">The archive into which to compress the files.</param>
        /// <returns>A yield of control after each file is archived.</returns>
        private IEnumerable ZipBuildFiles(ZipArchive zipArchive)
        {
            return ZipFolder(zipArchive, buildFolder, null);
        }

        /// <summary>
        /// Compresses all the git-archiveable source files in our project.
        /// </summary>
        /// <param name="zipArchive">The archive into which to compress the files.</param>
        /// <returns>A yield of control after each file is archived.</returns>
        private IEnumerable ZipSourceFiles(ZipArchive zipArchive)
        {
            return ZipFolder(zipArchive, projectRoot, new IgnoreParser());
        }

        /// <summary>
        /// Compresses all the non-ignored files in the given folder into the given archive.
        /// <para>The method yields control after each file is processed to allow for progress monitoring.</para>
        /// </summary>
        /// <param name="zipArchive">The archive into which to compress the files.</param>
        /// <param name="folder">The folder to compress</param>
        /// <param name="ignoreParser">If non-null, a parser that can decide whether or not to include a file.</param>
        /// <returns>A yield of control after each file is archived.</returns>
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

        /// <summary>
        /// Copies all the files in the recordings folder to the given target folder.
        /// </summary>
        /// <param name="target">Where to copy the files.</param>
        /// <returns>A yield of control after each file is copied.</returns>
        private IEnumerable CopyRecordings(string target)
        {
            return CopyFiles(target, new DirectoryInfo(recordingsFolderName));
        }

        /// <summary>
        /// Copies all the files in the documentation folder to the given target folder.
        /// </summary>
        /// <param name="target">Where to copy the files.</param>
        /// <returns>A yield of control after each file is copied.</returns>
        private IEnumerable CopyDocumentation(string target)
        {
            return CopyFiles(target, new DirectoryInfo(documentationFolderName));
        }

        /// <summary>
        /// Copies the files in the given folder to the given target folder.
        /// <para>The method yields control after each file is processed to allow for progress monitoring.</para>
        /// </summary>
        /// <param name="target">The folder from which to copy.</param>
        /// <param name="sourceDirectory">The folder to which to copy.</param>
        /// <returns>A yield of control after each file is copied.</returns>
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
