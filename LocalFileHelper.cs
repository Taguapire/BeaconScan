using System;
using System.Collections.Generic;
using System.IO;

namespace BeaconScan
{
    public static class LocalFileHelper
    {
        // Method to retrieve a list of files in the specified directory
        public static List<FileItem> GetLocalFileList(string localPath)
        {
            var fileList = new List<FileItem>();

            try
            {
                if (Directory.Exists(localPath))
                {
                    var entries = Directory.EnumerateFileSystemEntries(localPath);
                    foreach (var entry in entries)
                    {
                        fileList.Add(new FileItem
                        {
                            Name = Path.GetFileName(entry), // Extract the file or directory name
                            Type = Directory.Exists(entry) ? "D" : "F" // Determine type (Directory/File)
                        });
                    }
                }
                else
                {
                    throw new DirectoryNotFoundException($"Directory {localPath} does not exist.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving file list: {ex.Message}");
            }

            return fileList;
        }
    }
}
