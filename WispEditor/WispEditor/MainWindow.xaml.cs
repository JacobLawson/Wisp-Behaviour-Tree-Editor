using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Xml.Linq;

namespace WispEditor
{
    public partial class MainWindow : Window
    {

        private bool isPanning = false;
        private Point lastMidleMousePos;

        private const double SnapDistance = 15;
        private NodePort? _startPort = null;
        private Line? _tempLine = null;

        private readonly List<Node> allNodes = new();

        public MainWindow()
        {
            InitializeComponent();

            EditorCanvas.PreviewMouseDown += EditorCanvas_MiddleMousePanStart;
            EditorCanvas.PreviewMouseUp += EditorCanvas_MiddleMousePanEnd;
            EditorCanvas.PreviewMouseMove += EditorCanvas_MiddleMousePan;

            CenterCanvas();

            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            AddStartNode();
        }

        private void CenterCanvas()
        {
            Point canvas_center = new Point(EditorCanvas.Width/2, EditorCanvas.Height/2);
            Point scrollOffset = new Point(canvas_center.X - MainScrollViewer.ViewportWidth/2, canvas_center.Y - MainScrollViewer.ViewportHeight/2);

            MainScrollViewer.ScrollToHorizontalOffset(scrollOffset.X);
            MainScrollViewer.ScrollToVerticalOffset(scrollOffset.Y);
        }

        private Point GetViewportCenterOnCanvas()
        {
            return new Point(
                MainScrollViewer.HorizontalOffset +  MainScrollViewer.ViewportWidth/2, 
                MainScrollViewer.VerticalOffset + MainScrollViewer.ViewportHeight/2
                );
        }

        private void AddStartNode()
        {
            Node node = new Node(false);

            node.OnOutputPortCreated = port =>
            {
                port.ConnectorDot.MouseLeftButtonDown += (s, e) =>
                {
                    e.Handled = true;
                    BeginPortConnectionDrag(s, e);
                };
            };

            Initialise_Node(node);
        }

        private void CreateNewNode_Click(object sender, RoutedEventArgs e)
        {
            Node node = new Node(true);

            node.OnOutputPortCreated = port =>
            {
                port.ConnectorDot.MouseLeftButtonDown += (s, e) =>
                {
                    e.Handled = true;
                    BeginPortConnectionDrag(s, e);
                };
            };

            Initialise_Node(node);
        }

        private void Initialise_Node(Node _node)
        {
            if (_node.inputPort != null)
            {
                _node.inputPort.ConnectorDot.MouseLeftButtonUp += EndPortConnectionDrag;
            }   

            foreach (var port in _node.outputPorts)
            {
                port.ConnectorDot.MouseLeftButtonDown += (s, e) =>
                {
                    e.Handled = true;
                    BeginPortConnectionDrag(s, e);

                };
            }

            Point center = GetViewportCenterOnCanvas();

            _node.SetPosition(EditorCanvas, center.X - _node.width / 2, center.Y - _node.height / 2);
            _node.EnableDragging();
            _node.Dragged += (s, e) => _node.Update();

            allNodes.Add(_node);
        }

        private void BeginPortConnectionDrag(object sender, MouseButtonEventArgs e)
        {
            if (sender is Ellipse dot)
            {
                _startPort = FindPortByDot(dot);
                if (_startPort == null) { return; }

                Point start = _startPort.GetGlobalPosition(EditorCanvas);

                _tempLine = new Line
                {
                    Stroke = Brushes.Red,
                    StrokeThickness = 2,
                    X1 = start.X,
                    Y1 = start.Y,
                    X2 = start.X,
                    Y2 = start.Y
                };

                EditorCanvas.Children.Add(_tempLine);
                EditorCanvas.MouseMove += UpdatePortConnectionDrag;
                EditorCanvas.MouseLeftButtonUp += EndPortConnectionDrag;
            }
        }

        private void EndPortConnectionDrag(object sender, MouseButtonEventArgs e)
        {
            if (_tempLine == null || _startPort == null) { return; }

            EditorCanvas.MouseMove -= UpdatePortConnectionDrag;
            EditorCanvas.MouseLeftButtonUp -= EndPortConnectionDrag;

            NodePort? targetInput = FindNearestInputPort(e.GetPosition(EditorCanvas));

            if (targetInput != null)
            {
                if (_startPort.connection != null)
                {
                    var old = _startPort.connection;
                    EditorCanvas.Children.Remove(old.Line);
                    old.inputPort.DetachConnection();
                    _startPort.DetachConnection();
                }

                if (targetInput.connection != null)
                {
                    var old = targetInput.connection;
                    EditorCanvas.Children.Remove(old.Line);
                    old.outputPort.DetachConnection();
                    targetInput.DetachConnection();
                }

                var connection = new NodeConnection(_tempLine, _startPort, targetInput, EditorCanvas);
                _startPort.AttachConnection(connection);
                targetInput.AttachConnection(connection);
            }
            else
            {
                EditorCanvas.Children.Remove(_tempLine);
            }

            _tempLine = null;
            _startPort = null;
        }

        private void UpdatePortConnectionDrag(object sender, MouseEventArgs e)
        {
            if (_tempLine != null)
            {
                Point pos = e.GetPosition(EditorCanvas);
                _tempLine.X2 = pos.X;
                _tempLine.Y2 = pos.Y;
            }
        }

        private NodePort? FindPortByDot(Ellipse dot)
        {
            foreach (var node in allNodes) // you'll need to track this list
            {
                if (node.inputPort?.ConnectorDot == dot)
                    return node.inputPort;

                foreach (var port in node.outputPorts)
                    if (port.ConnectorDot == dot)
                        return port;
            }

            return null;
        }

        private NodePort? FindNearestInputPort(Point position)
        {
            NodePort? closest = null;
            double closestDist = double.MaxValue;

            foreach (var node in allNodes)
            {
                if (node.inputPort == null) continue;

                var port = node.inputPort;
                Point portPos = port.GetGlobalPosition(EditorCanvas);

                double dist = (portPos - position).Length;
                if (dist < SnapDistance && dist < closestDist)
                {
                    closest = port;
                    closestDist = dist;
                }
            }

            return closest;
        }

        private void EditorCanvas_MiddleMousePanStart(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Middle)
            {
                isPanning = true;
                lastMidleMousePos = e.GetPosition(MainScrollViewer);
                EditorCanvas.CaptureMouse();
                e.Handled = true;
            }
        }

        private void EditorCanvas_MiddleMousePanEnd(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Middle)
            {
                isPanning = false;
                EditorCanvas.ReleaseMouseCapture();
                e.Handled = true;
            }
        }

        private void EditorCanvas_MiddleMousePan(object sender, MouseEventArgs e)
        {
            if (isPanning && e.MiddleButton == MouseButtonState.Pressed)
            {
                Point currentPos = e.GetPosition(MainScrollViewer);
                Vector delta = currentPos - lastMidleMousePos;

                MainScrollViewer.ScrollToHorizontalOffset(MainScrollViewer.HorizontalOffset - delta.X);
                MainScrollViewer.ScrollToVerticalOffset(MainScrollViewer.VerticalOffset - delta.Y);

                lastMidleMousePos = currentPos;
                e.Handled = true;
            }
        }

        private void SaveBehaviourGraph(string path)
        {
            var save = new SaveData();

            foreach (var node in allNodes)
            {
                var pos = node.GetPosition(EditorCanvas);
                var nodeData = new NodeData()
                {
                    Id = node.Id,
                    Type = node.NodeType,
                    X = pos.X,
                    Y = pos.Y,
                    OutputCount = node.outputPorts.Count,
                    LeftOperand = node.LeftOperand,
                    Operator = node.ConditionOperator,
                    RightOperand = node.RightOperand
                };
                save.Nodes.Add(nodeData);
            }

            foreach (var node in allNodes)
            {
                foreach (var port in node.outputPorts)
                {
                    if (port.connection != null)
                    {
                        save.Connections.Add(new ConnectionData()
                        {
                            OutputNodeID = node.Id,
                            OutputIndex = port.index,
                            InputNodeID = port.connection.inputPort.parentNode.Id
                        });
                    }
                }
            }

            var json = System.Text.Json.JsonSerializer.Serialize(save, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(path, json);
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog()
            {
                Filter = "JSON Files (*.json)|*.json"
            };

            if (dialog.ShowDialog() == true)
            {  
                SaveBehaviourGraph(dialog.FileName);
            }
        }

        private void LoadBehaviourGraph(string path)
        {
            if (!File.Exists(path)) return;

            // Clear the existing canvas
            EditorCanvas.Children.Clear();
            allNodes.Clear();

            // Load JSON
            string json = File.ReadAllText(path);
            var save = JsonSerializer.Deserialize<SaveData>(json);
            if (save == null) return;

            // Map for later connection resolution
            Dictionary<Guid, Node> nodeMap = new();

            // Recreate all nodes
            foreach (var data in save.Nodes)
            {
                Node node = new(data.Type != Node_Type.START, data.OutputCount);

                // Set private node_type via reflection
                typeof(Node)
                    .GetField("node_type", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                    .SetValue(node, data.Type);

                node.SetPosition(EditorCanvas, data.X, data.Y);
                node.EnableDragging();
                node.Dragged += (s, e) => node.Update();

                // Assign handler for future ports
                node.OnOutputPortCreated = port =>
                {
                    port.ConnectorDot.MouseLeftButtonDown += (s, e) =>
                    {
                        e.Handled = true;
                        BeginPortConnectionDrag(s, e);
                    };
                };

                // 🟢 Fix: Apply handler to already-existing ports
                foreach (var port in node.outputPorts)
                {
                    port.ConnectorDot.MouseLeftButtonDown += (s, e) =>
                    {
                        e.Handled = true;
                        BeginPortConnectionDrag(s, e);
                    };
                }

                if (node.inputPort != null)
                {
                    node.inputPort.ConnectorDot.MouseLeftButtonUp += EndPortConnectionDrag;
                }

                if (node.nodeType_dropDown != null)
                {
                    node.nodeType_dropDown.SelectedIndex = (int)data.Type;
                }

                node.UpdateNodeCanvas(); // Add fields
                node.LoadConditionFields(data.LeftOperand ?? "", data.Operator ?? "", data.RightOperand ?? "");
                allNodes.Add(node);
                nodeMap[data.Id] = node;
            }

            // Reconnect wires
            foreach (var connection in save.Connections)
            {
                if (nodeMap.TryGetValue(connection.OutputNodeID, out var outNode) &&
                    nodeMap.TryGetValue(connection.InputNodeID, out var inNode) &&
                    connection.OutputIndex < outNode.outputPorts.Count &&
                    inNode.inputPort != null)
                {
                    var outputPort = outNode.outputPorts[connection.OutputIndex];
                    var inputPort = inNode.inputPort;

                    Point start = outputPort.GetGlobalPosition(EditorCanvas);
                    Point end = inputPort.GetGlobalPosition(EditorCanvas);

                    var line = new Line
                    {
                        Stroke = Brushes.Red,
                        StrokeThickness = 2,
                        X1 = start.X,
                        Y1 = start.Y,
                        X2 = end.X,
                        Y2 = end.Y
                    };

                    EditorCanvas.Children.Add(line);
                    var nodeConnection = new NodeConnection(line, outputPort, inputPort, EditorCanvas);
                    outputPort.AttachConnection(nodeConnection);
                    inputPort.AttachConnection(nodeConnection);
                }
            }

            // Force connection redraw after layout is ready
            Dispatcher.InvokeAsync(() =>
            {
                foreach (var node in allNodes)
                {
                    node.Update();
                }
            }, System.Windows.Threading.DispatcherPriority.Loaded);
        }

        private void Load_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog()
            {
                Filter = "JSON Files (*.json)|*.json"
            };

            if (dialog.ShowDialog() == true)
            {
                LoadBehaviourGraph(dialog.FileName);
            }
        }
    }
}
