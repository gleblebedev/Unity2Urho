using System.IO;
using System.Text;
using System.Xml;
using UnityEditor;
using UnityEngine;

namespace Urho3DExporter
{
    /// <summary>
    /// Destination folder helper.
    /// </summary>
    public class DestinationFolder
    {
        private readonly string _folder;
        private bool? _overrideFiles;


        public DestinationFolder(string folder, bool? overrideFiles = null)
        {
            _folder = folder.FixDirectorySeparator();
            if (!_folder.EndsWith(Path.DirectorySeparatorChar.ToString()))
                _folder += Path.DirectorySeparatorChar;

            _overrideFiles = overrideFiles;
        }

        public void CopyFile(string sourceFilePath, string destinationFilePath)
        {
            if (destinationFilePath == null)
                return;
            var targetPath = Path.Combine(_folder, destinationFilePath.FixDirectorySeparator()).FixDirectorySeparator();
            if (File.Exists(targetPath))
            {
                if (!_overrideFiles.HasValue)
                {
                    _overrideFiles = EditorUtility.DisplayDialog("File already exists", "Destination file " + targetPath + " already exist.", "Override all", "Skip all");
                }

                if (!_overrideFiles.Value)
                {
                    return;
                }
            }

            var directoryName = Path.GetDirectoryName(targetPath);
            if (directoryName != null)
            {
                Directory.CreateDirectory(directoryName);
            }

            File.Copy(sourceFilePath, targetPath, true);

        }

        public FileStream Create(string relativePath)
        {
            if (relativePath == null)
                return null;

            var targetPath = Path.Combine(_folder, relativePath.FixDirectorySeparator()).FixDirectorySeparator();
            if (File.Exists(targetPath))
            {
                if (!_overrideFiles.HasValue)
                {
                    _overrideFiles = EditorUtility.DisplayDialog("File already exists", "Destination file "+ targetPath + " already exist.", "Override all", "Skip all");
                }

                if (!_overrideFiles.Value)
                {
                    return null;
                }
            }

            var directoryName = Path.GetDirectoryName(targetPath);
            if (directoryName != null)
            {
                Directory.CreateDirectory(directoryName);
            }

            return File.Open(targetPath, FileMode.Create, FileAccess.Write, FileShare.Read);
        }

        public override string ToString()
        {
            return _folder;
        }

        public XmlTextWriter CreateXml(string relativePath)
        {
            var fileStream = Create(relativePath);
            if (fileStream == null)
                return null;
            return new XmlTextWriter(fileStream, new UTF8Encoding(false));
        }

     
    }
}