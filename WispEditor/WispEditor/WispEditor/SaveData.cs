using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WispEditor
{
    public class NodeData
    {
        public Guid Id { get; set; }
        public Node_Type Type { get; set; }
        public double X { get; set; }
        public double Y { get; set; }

        public string? LeftOperand { get; set; }
        public string? Operator { get; set; }
        public string? RightOperand { get; set; }

        public int OutputCount { get; set; }
    }

    public class ConnectionData
    {
        public Guid OutputNodeID { get; set; }
        public int OutputIndex { get; set; }

        public Guid InputNodeID { get; set; }
    }

    public class SaveData
    {
        public List<NodeData> Nodes { get; set; } = new();
        public List<ConnectionData> Connections { get; set; } = new();
    }
}
