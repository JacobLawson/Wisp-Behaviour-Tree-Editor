using System;
using System.Collections.Generic;
using System.Security.Authentication;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace WispEditor
{
    public enum Node_Type
    {
        ACTION,
        CONDITION,
        PARALLEL,
        SELECTOR,
        SEQUENCE,
        SCRIPT,
        START
    }

    public class NodeDragEventArgs : EventArgs
    {
        public Point Position { get; }

        public NodeDragEventArgs(Point position)
        {
            Position = position;
        }
    }

    public class Node
    {
        public Border nodeContainerUI { get; private set; }
        public Canvas nodeCanvas { get; private set; }

        public NodePort? inputPort { get; private set; }
        public List<NodePort> outputPorts { get; } = new();

        public double width => nodeContainerUI.Width;
        public double height => nodeContainerUI.Height;

        // Events
        public event EventHandler<NodeDragEventArgs>? DragStarted;
        public event EventHandler<NodeDragEventArgs>? Dragged;
        public event EventHandler<NodeDragEventArgs>? DragEnded;

        //delegates
        public Action<NodePort> OnOutputPortCreated;

        // Dragging
        private bool isDragging = false;
        private Point dragOffset;

        //design / node type
        Node_Type node_type;

        public Guid Id { get; } = Guid.NewGuid();

        public Node_Type NodeType => node_type;
        public ComboBox? nodeType_dropDown;
        private TextBox leftField;
        private TextBox rightField;
        private ComboBox opperator_combo;

        public string LeftOperand { get; private set; } = "";
        public string RightOperand { get; private set; } = "";
        public string ConditionOperator { get; private set; } = "";

        public Node(bool hasInput, int outputCount = 1)
        {
            nodeContainerUI = new Border
            {
                Width = 250,
                Height = 100,
                Background = Brushes.White,
                BorderBrush = Brushes.DarkSlateGray,
                BorderThickness = new Thickness(2),
                Padding = new Thickness(2)
            };

            nodeCanvas = new Canvas();
            nodeContainerUI.Child = nodeCanvas;

            if (hasInput)
            {
                inputPort = new NodePort(this, NodePortType.Input, 0);
                Canvas.SetLeft(inputPort.ConnectorDot, (width - inputPort.ConnectorDot.Width) / 2);
                Canvas.SetTop(inputPort.ConnectorDot, -10);
                nodeCanvas.Children.Add(inputPort.ConnectorDot);

                nodeType_dropDown = new ComboBox
                {
                    Width = 120,
                    Height = 20,
                    ItemsSource = Enum.GetValues(typeof(Node_Type)).Cast<Node_Type>().Take(Enum.GetValues(typeof(Node_Type)).Length - 1).ToList(),
                    SelectedIndex = 0
                };
                nodeType_dropDown.SelectionChanged += DropDown_selectionChanged;

                Canvas.SetLeft(nodeType_dropDown, 2);
                Canvas.SetTop(nodeType_dropDown, 2);
                nodeCanvas.Children.Add(nodeType_dropDown);
            }
            else
            {
                node_type = Node_Type.START;
            }

            for (int i = 0; i < outputCount; i++)
            {
                var port = new NodePort(this, NodePortType.Output, i);
                outputPorts.Add(port);

                double spacing = width / (outputCount + 1);
                double x = (spacing * (i + 1)) - (port.ConnectorDot.Width / 2);
                double y = height - port.ConnectorDot.Height;

                Canvas.SetLeft(port.ConnectorDot, x);
                Canvas.SetTop(port.ConnectorDot, y);
                nodeCanvas.Children.Add(port.ConnectorDot);
            }

            UpdateNodeCanvas();
        }

        public void Update()
        {
            UpdateConnections();
        }

        private void UpdateConnections()
        {
            inputPort?.connection?.Update();
            foreach (var port in outputPorts)
            {
                port.connection?.Update();
            }
        }

        private void DropDown_selectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is not ComboBox comboBox) {  return; }

            node_type = (Node_Type)comboBox.SelectedIndex;

            UpdateNodeCanvas();
        }

        public void UpdateNodeCanvas()
        {
            for (int i = nodeCanvas.Children.Count - 1; i >= 0; i--)
            {
                if (nodeCanvas.Children[i] is FrameworkElement fe && fe.Tag as string == "Dynamic")
                {
                    nodeCanvas.Children.RemoveAt(i);
                }
            }

            switch (node_type)
            {
                case Node_Type.ACTION:
                {
                        leftField = new TextBox
                        {
                            Width = 200,
                            Height = 22,
                            Text = "",
                            Tag = "Dynamic"
                        };
                        leftField.TextChanged += (s, e) => LeftOperand = leftField.Text;
                        Canvas.SetLeft(leftField, 20);
                        Canvas.SetTop(leftField, 35);
                        nodeCanvas.Children.Add(leftField);
                    } break;
                case Node_Type.CONDITION:
                {
                        // Left operand textbox
                        leftField = new TextBox
                        {
                            Width = 80,
                            Height = 22,
                            Text = "",
                            Tag = "Dynamic"
                        };
                        leftField.TextChanged += (s, e) => LeftOperand = leftField.Text;
                        Canvas.SetLeft(leftField, 5);
                        Canvas.SetTop(leftField, 35);
                        nodeCanvas.Children.Add(leftField);

                        // Operator dropdown
                        opperator_combo = new ComboBox
                        {
                            Width = 50,
                            Height = 22,
                            ItemsSource = new List<string> { "==", "!=", "<", ">", "<=", ">=" },
                            SelectedIndex = 0,
                            Tag = "Dynamic"
                        };
                        opperator_combo.SelectionChanged += (s, e) =>
                        {
                            if (opperator_combo.SelectedItem is string selected)
                            {
                                ConditionOperator = selected;
                            }
                        };

                        Canvas.SetLeft(opperator_combo, 90);
                        Canvas.SetTop(opperator_combo, 35);
                        nodeCanvas.Children.Add(opperator_combo);

                        // Right operand textbox
                        rightField = new TextBox
                        {
                            Width = 80,
                            Height = 22,
                            Text = "",
                            Tag = "Dynamic"
                        };
                        rightField.TextChanged += (s, e) => RightOperand = rightField.Text;
                        Canvas.SetLeft(rightField, 145);
                        Canvas.SetTop(rightField, 35);
                        nodeCanvas.Children.Add(rightField);
                } break;
                case Node_Type.PARALLEL:
                {
                    AddOutputNodeButtons();
                }
                break;
                case Node_Type.SCRIPT:
                {
                    leftField = new TextBox
                    {
                        Width = 200,
                        Height = 22,
                        Text = "",
                        Tag = "Dynamic"
                    };
                    leftField.TextChanged += (s, e) => LeftOperand = leftField.Text;
                    Canvas.SetLeft(leftField, 20);
                    Canvas.SetTop(leftField, 35);
                    nodeCanvas.Children.Add(leftField);
                }
                break;
                case Node_Type.SELECTOR:
                {
                    AddOutputNodeButtons();
                } break;
                case Node_Type.SEQUENCE:
                {
                    AddOutputNodeButtons();
                } break;
                case Node_Type.START:
                {
                    var labelBlock = new TextBlock
                    {
                        Text = "Start",
                        FontWeight = FontWeights.Bold,
                        FontSize = 14,
                        Foreground = Brushes.Black,
                        TextAlignment = TextAlignment.Center,
                        Width = nodeContainerUI.Width, // match node width for centering
                        TextWrapping = TextWrapping.Wrap
                    };

                    Canvas.SetLeft(labelBlock, 0);
                    Canvas.SetTop(labelBlock, (nodeContainerUI.Height-20)/ 2);
                    nodeCanvas.Children.Add(labelBlock);
                 } break;

            }
        }

        private void AddOutputNodeButtons()
        {
            var addButton = new Button
            {
                Content = "+",
                Width = 25,
                Height = 25,
                Tag = "Dynamic"
            };
            Canvas.SetLeft(addButton, 5);
            Canvas.SetTop(addButton, 35);
            nodeCanvas.Children.Add(addButton);

            addButton.Click += (s, e) =>
            {
                AddOutputPort();
                UpdateOutputPortLayout();
            };

            var removeButton = new Button
            {
                Content = "-",
                Width = 25,
                Height = 25,
                Tag = "Dynamic"
            };
            Canvas.SetLeft(removeButton, 35);
            Canvas.SetTop(removeButton, 35);
            nodeCanvas.Children.Add(removeButton);

            removeButton.Click += (s, e) =>
            {
                RemoveOutputPort();
                UpdateOutputPortLayout();
            };
        }

        private void AddOutputPort()
        {
            var port = new NodePort(this, NodePortType.Output, outputPorts.Count);
            outputPorts.Add(port);
            nodeCanvas.Children.Add(port.ConnectorDot);
            OnOutputPortCreated?.Invoke(port);

        }

        private void RemoveOutputPort()
        {
            if (outputPorts.Count <= 1) { return; }
            var lastPort = outputPorts[^1];

            if (lastPort.connection != null)
            {
                var connection = lastPort.connection;
                connection.inputPort.DetachConnection();
                connection.outputPort.DetachConnection();

                GetParentCanvas().Children.Remove(connection.Line);
            }

            nodeCanvas.Children.Remove(lastPort.ConnectorDot);
            outputPorts.RemoveAt(outputPorts.Count - 1);

            UpdateOutputPortLayout();
            //Update();
        }

        public void UpdateOutputPortLayout()
        {
            int count = outputPorts.Count;
            for (int i = 0; i < count; i++)
            {
                var port = outputPorts[i];

                double spacing = width / (count + 1);
                double x = (spacing * (i + 1)) - (port.ConnectorDot.Width / 2);
                double y = height - port.ConnectorDot.Height;

                Canvas.SetLeft(port.ConnectorDot, x);
                Canvas.SetTop(port.ConnectorDot, y);

                if (port.connection != null)
                {
                    port.ConnectorDot.UpdateLayout();
                    nodeCanvas.UpdateLayout();
                    nodeContainerUI.UpdateLayout();
                    port.connection.Update();

                }
            }
        }

        public void SetPosition(Canvas canvas, double x, double y)
        {
            Canvas.SetLeft(nodeContainerUI, x);
            Canvas.SetTop(nodeContainerUI, y);
            if (!canvas.Children.Contains(nodeContainerUI))
            {
                canvas.Children.Add(nodeContainerUI);
            }
        }

        public Point GetPosition(Canvas canvas)
        {
            return new Point(Canvas.GetLeft(nodeContainerUI), Canvas.GetTop(nodeContainerUI));
        }

        private Canvas GetParentCanvas()
        {
            return nodeContainerUI.Parent as Canvas ?? throw new Exception("Node must be inside a Canvas.");
        }

        private Point GetPositionFromParentCanvas()
        {
            return new Point(Canvas.GetLeft(nodeContainerUI), Canvas.GetTop(nodeContainerUI));
        }

        public void EnableDragging()
        {
            nodeContainerUI.MouseLeftButtonDown += OnMouseDown;
            nodeContainerUI.MouseLeftButtonUp += OnMouseUp;
            nodeContainerUI.MouseMove += OnMouseMove;
        }

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            dragOffset = e.GetPosition(nodeContainerUI);
            isDragging = true;
            nodeContainerUI.CaptureMouse();

            DragStarted?.Invoke(this, new NodeDragEventArgs(GetPositionFromParentCanvas()));
        }

        private void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (isDragging)
            {
                isDragging = false;
                nodeContainerUI.ReleaseMouseCapture();
                DragEnded?.Invoke(this, new NodeDragEventArgs(GetPositionFromParentCanvas()));
            }
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (!isDragging) return;

            Point pos = e.GetPosition(GetParentCanvas());
            SetPosition(GetParentCanvas(), pos.X - dragOffset.X, pos.Y - dragOffset.Y);
            Dragged?.Invoke(this, new NodeDragEventArgs(GetPositionFromParentCanvas()));
        }

        public void LoadConditionFields(string left, string op, string right)
        {
            LeftOperand = left;
            ConditionOperator = op;
            RightOperand = right;

            if (leftField != null) {
                leftField.Text = LeftOperand; 
            }
            if (opperator_combo != null && opperator_combo.Items.Contains(ConditionOperator)) 
            {
                opperator_combo.Text = ConditionOperator; 
            }
            if (rightField != null) 
            { 
                rightField.Text = RightOperand; 
            }
        }
    }
}