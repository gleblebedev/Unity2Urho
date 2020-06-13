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
        private bool? _overrideAllFiles;
        private HashSet<string> _createdFiles = new HashSet<string>();

        public DestinationFolder(string folder, bool? overrideAllFiles = null)
        {
            _folder = folder.FixDirectorySeparator();
            if (!_folder.EndsWith(Path.DirectorySeparatorChar.ToString()))
                _folder += Path.DirectorySeparatorChar;

            _overrideAllFiles = overrideAllFiles;
        }

        public override string ToString()
        {
            return _folder;
        }

        public void CopyFile(string sourceFilePath, string destinationFilePath)
        {
            if (destinationFilePath == null)
                return;
            var targetPath = Path.Combine(_folder, destinationFilePath.FixDirectorySeparator()).FixDirectorySeparator();

            //Skip file if it already exported
            if (!_createdFiles.Add(targetPath))
            {
                return;
            }

            //Skip file if override is disabled and file exists
            if (_overrideAllFiles != true && File.Exists(targetPath))
            {
                if (!_overrideAllFiles.HasValue)
                    _overrideAllFiles = EditorUtility.DisplayDialog("File already exists",
                        "Destination file " + targetPath + " already exist.", "Override all", "Skip all");

                if (!_overrideAllFiles.Value) return;
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

            //Skip file if override is disabled and file exists
            if (_overrideAllFiles != true && File.Exists(targetPath))
            {
                if (!_overrideAllFiles.HasValue)
                    _overrideAllFiles = EditorUtility.DisplayDialog("File already exists",
                        "Destination file " + targetPath + " already exist.", "Override all", "Skip all");

                if (!_overrideAllFiles.Value) return null;
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