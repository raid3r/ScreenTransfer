using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibrary
{
    [Serializable]
    public class TreeNodeData
    {
        public string Name { get; set; }
        public string FullName { get; set; }
        public bool IsDirectory { get; set; }   

        public List<TreeNodeData> Nodes { get; set; }

        public TreeNodeData()
        {
            Nodes = new List<TreeNodeData>();
        }
    }
}
