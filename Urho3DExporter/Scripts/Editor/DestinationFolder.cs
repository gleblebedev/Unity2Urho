using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using UnityEditor;

namespace Urho3DExporter
{
    /// <summary>
    ///     Destination folder helper.
    /// </summary>
    public class DestinationFolder
    {
        private readonly string _folder;
        private bool _exportOnlyUpdated;
        private HashSet<string> _createdFiles = new HashSet<string>();

        public DestinationFolder(string folder, bool exportOnlyUpdated = false)
        {
            _folder = folder.FixDirectorySeparator();
            if (!_folder.EndsWith(Path.DirectorySeparatorChar.ToString()))
                _folder += Path.DirectorySeparatorChar;

            _exportOnlyUpdated = exportOnlyUpdated;
        }

        public override string ToString()
        {
            return _folder;
        }

        public void CopyFile(string sourceFilePath, string destinationFilePath)
        {
            if (destinationFilePath == null)
                return;
            if (!File.Exists(sourceFilePath))
                return;
            var targetPath = Path.Combine(_folder, destinationFilePath.FixDirectorySeparator()).FixDirectorySeparator();

            //Skip file if it already exported
            if (!_createdFiles.Add(targetPath))
            {
                return;
            }

            //Skip file if it is already up to date
            if (_exportOnlyUpdated)
            {
                if (File.Exists(targetPath))
                {
                    var sourceLastWriteTimeUtc = File.GetLastWriteTimeUtc(sourceFilePath);
                    var lastWriteTimeUtc = File.GetLastWriteTimeUtc(targetPath);
                    if (sourceLastWriteTimeUtc <= lastWriteTimeUtc)
                        return;
                }
            }

            var directoryName = Path.GetDirectoryName(targetPath);
            if (directoryName != null) Directory.CreateDirectory(directoryName);

            File.Copy(sourceFilePath, targetPath, true);
        }

        public FileStream Create(string relativePath, DateTime sourceFileTimestampUTC)
        {
            if (relativePath == null)
            {
                return null;
            }

            var targetPath = Path.Combine(_folder, relativePath.FixDirectorySeparator()).FixDirectorySeparator();

            //Skip file if it already exported
            if (!_createdFiles.Add(targetPath))
            {
                return null;
            }

            //Skip file if it is already up to date
            if (_exportOnlyUpdated)
            {
                if (File.Exists(targetPath))
                {
                    var lastWriteTimeUtc = File.GetLastWriteTimeUtc(targetPath);
                    if (sourceFileTimestampUTC <= lastWriteTimeUtc)
                        return null;
                }
            }

            var directoryName = Path.GetDirectoryName(targetPath);
            if (directoryName != null) Directory.CreateDirectory(directoryName);

            return File.Open(targetPath, FileMode.Create, FileAccess.Write, FileShare.Read);
        }

        public XmlTextWriter CreateXml(string relativePath, DateTime sourceFileTimestampUTC)
        {
            var fileStream = Create(relativePath, sourceFileTimestampUTC);
            if (fileStream == null)
                return null;
            return new XmlTextWriter(fileStream, new UTF8Encoding(false));
        }
    }
}