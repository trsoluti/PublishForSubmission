using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using UnityEngine;


namespace UnityEditor.TRSolutions.Publish
{
    public class Compress
    {
        private DirectoryInfo rootDirectory;
        private string rootFolderName;
        private ZipArchive archive;
        private IgnoreParser ignoreParser;
        
        public Compress(DirectoryInfo rootDirectory, ZipArchive zipArchive, IgnoreParser ignoreParser = null)
        {
            this.rootDirectory = rootDirectory;
            this.rootFolderName = rootDirectory.Name;
            this.archive = zipArchive;
            this.ignoreParser = ignoreParser;
        }
        public Compress(string rootFolderName, ZipArchive zipArchive, IgnoreParser ignoreParser = null)
        {
            this.rootDirectory = null;
            this.rootFolderName = rootFolderName;
            this.archive = zipArchive;
            this.ignoreParser = ignoreParser;
        }

        public void CompressRoot()
        {
            CompressFolder("/", rootDirectory);
        }

        public void CompressFolder(string folderRelativePath, DirectoryInfo folder)
        {
            //+ Debug.Log($"Compressing relative path {folderRelativePath}");

            foreach (var fileInfo in folder.EnumerateFileSystemInfos("*", SearchOption.TopDirectoryOnly))
            {
                //var path = fileInfo.FullName;
                //print(string.Format("Processing {0}", path));
                // Construct the path in a format that our git-like ignore function recognizes:
                // - "/" is the root of our project;
                // - Directories end in "/"
                // - Unix-style file separators
                var isDirectory = (File.GetAttributes(fileInfo.FullName) & FileAttributes.Directory) == FileAttributes.Directory;
                var fileRelativePath = folderRelativePath + fileInfo.Name + (isDirectory ? "/" : "");
                //+ Debug.Log($"Processing {fileRelativePath}");
                if (ignoreParser != null && ignoreParser.IsIgnored(fileRelativePath))
                {
                    //+ Debug.Log($"Skipping {fileRelativePath}");
                }
                else
                {
                    if (isDirectory)
                    {
                        CompressFolder(fileRelativePath, new DirectoryInfo(fileInfo.FullName));
                    }
                    else
                    {
                        var archivePath = ArchivePath(fileRelativePath);
                        //+ Debug.Log($"Compressing {archivePath}");
                        var zipArchiveEntry = archive.CreateEntry(archivePath);
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
                }
            }
        }

        private string ArchivePath(string fileRelativePath)
        {
            return rootFolderName + fileRelativePath;
        }
    }
}
