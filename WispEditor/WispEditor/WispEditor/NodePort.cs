using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace WispEditor
{
    public enum NodePortType
    {
        Input,
        Output
    }

    public class NodePort
    {
        public Ellipse ConnectorDot { get; }
        public Node parentNode { get; }
        public NodePortType connectionType { get; }
        public int index { get; }
        public string? Label { get; set; }

        public NodeConnection? connection { get; private set; }

        public NodePort(Node _parent, NodePortType _connectionType, int _index)
        {
            parentNode = _parent;
            connectionType = _connectionType;
            index = _index;

            ConnectorDot = new Ellipse
            {
                Width = 10,
                Height = 10,
                Fill = connectionType == NodePortType.Input ? Brushes.Blue : Brushes.Black,
                Cursor = Cursors.Hand
            };
        }

        public Point GetGlobalPosition(Canvas _targetCanvas)
        {
            return ConnectorDot.TranslatePoint(new Point(ConnectorDot.Width / 2, ConnectorDot.Height / 2), _targetCanvas);
        }

        public void AttachConnection(NodeConnection _connection)
        {
            connection = _connection;
        }

        public void DetachConnection()
        {
            connection = null;
        }
    }
}
