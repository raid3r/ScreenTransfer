using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibrary
{
    public static class TreeHelper
    {
        public static TreeNodeData CreateTree(string serverFolderPath, bool withFiles = true)
        {

            var rootDir = new DirectoryInfo(serverFolderPath);

            var rootNode = new TreeNodeData
            {
                Name = rootDir.Name,
                FullName = rootDir.FullName,
            };

            PopulateTreeNodeData(rootNode, rootDir, withFiles);

            return rootNode;
        }

        private static void PopulateTreeNodeData(TreeNodeData parentNode, DirectoryInfo directoryInfo, bool withFiles)
        {
            try
            {
                foreach (var directory in directoryInfo.GetDirectories())
                {
                    var directoryNode = new TreeNodeData()
                    {
                        Name = directory.Name,
                        FullName = directory.FullName,
                        IsDirectory = true,
                    };

                    parentNode.Nodes.Add(directoryNode);
                    PopulateTreeNodeData(directoryNode, directory, withFiles);
                }

                if (withFiles)
                {
                    foreach (var file in directoryInfo.GetFiles())
                    {
                        var fileNode = new TreeNodeData()
                        {
                            Name = file.Name,
                            FullName = file.FullName,
                            IsDirectory = false,
                        };

                        parentNode.Nodes.Add(fileNode);
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Обробка помилок доступу
            }
        }
    }
}
