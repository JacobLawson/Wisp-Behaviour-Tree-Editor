using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Shapes;

namespace WispEditor
{
    public class NodeConnection
    {
        public Line Line { get; }
        public NodePort outputPort { get; }
        public NodePort inputPort { get; }
        private readonly Canvas canvas;

        public NodeConnection(Line _line, NodePort _output, NodePort _input, Canvas canvas)
        {
            Line = _line;
            outputPort = _output;
            inputPort = _input;
            this.canvas = canvas;
        }

        public void Update()
        {
            Point start = outputPort.GetGlobalPosition(canvas);
            Point end = inputPort.GetGlobalPosition(canvas);

            Line.X1 = start.X;
            Line.Y1 = start.Y;
            Line.X2 = end.X;
            Line.Y2 = end.Y;
        }
    }
}
