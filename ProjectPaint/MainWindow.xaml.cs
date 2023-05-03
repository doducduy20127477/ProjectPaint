using GraphicsLibrary;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ProjectPaint
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        bool _isDrawing;
        Point2D _start = new Point2D(-1, -1);
        ShapeType _currentShapeType = ShapeType.Line2D;
        string dashStyle = "";
        bool shiftMode;
        bool ctrlMode;
        List<IShape> _listShapes = new List<IShape>();
        Dictionary<int, List<Image>> _images = new Dictionary<int, List<Image>>();
        List<IShape> _listRedo = new List<IShape>();
        IShape preview;
        bool _isZooming;
        string _color = "Black";
        double _thickness;
        public MainWindow()
        {
            InitializeComponent();
            DllLoader.execute();
            var types = DllLoader.Types;
            KeyDown += new KeyEventHandler(OnButtonKeyDown);
            KeyUp += new KeyEventHandler(OnButtonKeyUp);
        }

        private void LineBtn_Click(object sender, RoutedEventArgs e)
        {
            _currentShapeType = ShapeType.Line2D;
        }

        private void RectangleBtn_Click(object sender, RoutedEventArgs e)
        {
            _currentShapeType = ShapeType.Rectangle2D;
        }
        private void EllipseBtn_Click(object sender, RoutedEventArgs e)
        {
            _currentShapeType = ShapeType.Ellipse2D;
        }
        private void DrawingCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _listRedo.Clear();
             //thickness = double.Parse(thicknessBox.SelectedItem.ToString());
            preview = (IShape)GetType($"{_currentShapeType}");
            preview.StrokeThickness = _thickness;
            _isDrawing = true;
            Point currenCoord = e.GetPosition(DrawingCanvas);
            _start.X = currenCoord.X;
            _start.Y = currenCoord.Y;
            preview.HandleStart(_start);
        }

        private void DrawingCanvas_MouseMove(object sender, MouseEventArgs e)
        {                                                                                               
            if (_isDrawing)
            {
                Point coord = e.GetPosition(DrawingCanvas);

                Point2D point = new Point2D(coord.X, coord.Y);
                preview.HandleEnd(point);
                if (shiftMode)
                {
                    preview.HandleShiftMode();
                }
                preview.DashStyle = dashStyle;
                DrawingCanvas.Children.Clear();
                RedrawShape();
                preview.Color = GlobalOptions.previewColor;
                DrawingCanvas.Children.Add(preview.Draw());
            }
        }

        private void DrawingCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            _isDrawing = false;
            Point coord = e.GetPosition(DrawingCanvas);
            Point2D point = new Point2D(coord.X, coord.Y);
            if (preview != null)
            {
                preview.Color = GlobalOptions.strokeColor;
                preview.HandleEnd(point);
                if (shiftMode)
                {
                    preview.HandleShiftMode();
                }
                preview.DashStyle = dashStyle;
                System.Drawing.Color temp = System.Drawing.Color.FromName(_color);
                preview.Color = Color.FromArgb(temp.A, temp.R, temp.G, temp.B);
                _listShapes.Add(preview);
                DrawingCanvas.Children.Clear();
                RedrawShape();
            }
        }

        private void SaveAsBitmap(Canvas canvas, string filename)
        {
            RenderTargetBitmap renderBitmap = new RenderTargetBitmap((int)canvas.ActualWidth, (int)canvas.ActualHeight, 96d, 96d, PixelFormats.Pbgra32);
            canvas.Measure(new Size((int)canvas.ActualWidth, (int)canvas.ActualHeight));
            canvas.Arrange(new Rect(new Size((int)canvas.ActualWidth, (int)canvas.ActualHeight)));

            renderBitmap.Render(canvas);
            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(renderBitmap));

            using (FileStream file = File.Create(filename))
            {
                encoder.Save(file);
            }
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog();
            //saveFileDialog.InitialDirectory = @"C:\";
            saveFileDialog.Filter = "Images|*.png";
            saveFileDialog.Title = "Save as PNG";
            saveFileDialog.RestoreDirectory = true;
            Nullable<bool> result = saveFileDialog.ShowDialog();
            if (result == true)
            {
                String fileName = saveFileDialog.FileName;
                SaveAsBitmap(DrawingCanvas, fileName);
            }
        }

        private void LoadBitmapFile(ref Canvas canvas, string filename)
        {
            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(filename, UriKind.Absolute);
            bitmap.EndInit();

            Image image = new Image();
            image.Source = bitmap;
            image.Width = bitmap.Width;
            image.Height = bitmap.Height;
            if (bitmap.Width > canvas.Width || double.IsNaN(canvas.Width))
            {
                canvas.Width = bitmap.Width > canvas.ActualWidth ? bitmap.Width : double.NaN;
            }
            if (bitmap.Height > canvas.Height || double.IsNaN(canvas.Height))
            {
                canvas.Height = bitmap.Height > canvas.ActualHeight ? bitmap.Height : double.NaN;
            }
            if (!_images.ContainsKey(_listShapes.Count))
            {
                _images[_listShapes.Count] = new List<Image>();
            }
            _images[_listShapes.Count].Add(image);
            canvas.Children.Add(image);
        }

        private void LoadBtn_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Title = "Load image";
            openFileDialog.Filter = "Images|*.png;*.bmp;*.jpg";
            openFileDialog.RestoreDirectory = true;
            if (openFileDialog.ShowDialog() == true)
            {
                preview = null;
                LoadBitmapFile(ref DrawingCanvas, openFileDialog.FileName);
            };
        }

        private void OnButtonKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.LeftShift)
            {
                shiftMode = true;
            } else if (e.Key == Key.LeftCtrl)
            {
                ctrlMode = true;
            } else if (e.Key == Key.Z && ctrlMode)
            {
                if (_images.ContainsKey(0) && _listShapes.Count == 0)
                {
                    if (_images[0].Count > 0)
                    {
                        _images[0].RemoveAt(_images[0].Count - 1);
                        DrawingCanvas.Children.RemoveAt(DrawingCanvas.Children.Count - 1);
                    }
                }
                if (_listShapes.Count > 0)
                {
                    if (_images.ContainsKey(_listShapes.Count) && _images[_listShapes.Count].Count > 0)
                    {
                        _images[_listShapes.Count].RemoveAt(_images[_listShapes.Count].Count - 1);
                    } else
                    {
                        _listRedo.Add(_listShapes[_listShapes.Count - 1]);
                        _listShapes.RemoveAt(_listShapes.Count - 1);
                    }
                    DrawingCanvas.Children.RemoveAt(DrawingCanvas.Children.Count - 1);
                }
            } else if (e.Key == Key.Y && ctrlMode)
            {
                if (_listRedo.Count > 0)
                {
                    _listShapes.Add(_listRedo[_listRedo.Count - 1]);
                    DrawingCanvas.Children.Add(_listShapes[_listShapes.Count - 1].Draw());
                    _listRedo.RemoveAt(_listRedo.Count - 1);
                }
            }
        }
        private void OnButtonKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.LeftShift)
            {
                shiftMode = false;
            }
            else if (e.Key == Key.LeftCtrl)
            {
                ctrlMode = false;
            }
        }

        public object GetType(string strFullyQualifiedName)
        {
            Type type = Type.GetType(strFullyQualifiedName);
            if (type != null)
                return Activator.CreateInstance(type);
            var types = DllLoader.Types;
            foreach (var t in types)
            {
                if (t.Name == strFullyQualifiedName)
                    return Activator.CreateInstance(t);
            }
            return null;
        }

        private void DashCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            dashStyle = GlobalOptions.dashStyle;
        }

        private void DashCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            dashStyle = "";
        }
        private void RedrawShape()
        {
            for (int i = 0; i < _listShapes.Count; i++)
            {
                if (_images.ContainsKey(i))
                {
                    foreach (var image in _images[i])
                    {
                        DrawingCanvas.Children.Add(image);
                    }
                }
                var element = _listShapes[i].Draw();
                DrawingCanvas.Children.Add(element);
            }
        }

        //private void Pen_Click(object sender, RoutedEventArgs e)
        //{
        //    _currentShapeType = ShapeType.Point2D;
        //}

        private void ZoomCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            _isZooming = true;
        }

        private void ZoomCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            _isZooming = false;
        }

        public float Zoomfactor { get; set; } = 1.1f;
        private readonly MatrixTransform _transform = new MatrixTransform();

        private void DrawingCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if(_isZooming)
            {

                float scaleFactor = Zoomfactor;
                if (e.Delta < 0)
                {
                    scaleFactor = 1f / scaleFactor;
                }

                Point mousePostion = e.GetPosition(this);

                Matrix scaleMatrix = _transform.Matrix;
                scaleMatrix.ScaleAt(scaleFactor, scaleFactor, mousePostion.X, mousePostion.Y);
                _transform.Matrix = scaleMatrix;

                foreach (UIElement child in this.DrawingCanvas.Children)
                {
                    double x = Canvas.GetLeft(child);
                    double y = Canvas.GetTop(child);

                    double sx = x * scaleFactor;
                    double sy = y * scaleFactor;

                    Canvas.SetLeft(child, sx);
                    Canvas.SetTop(child, sy);

                    child.RenderTransform = _transform;
                }
            }
            
        }

        private void colorBtn_Click(object sender, RoutedEventArgs e)
        {
            Button clickedButton = (Button)sender;
            string buttonName = clickedButton.Name;
            _color = buttonName;
        }

        private void thicknessBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int index = thicknessBox.SelectedIndex;

            switch (index)
            {
                case 0:
                    _thickness = 1;
                    break;
                case 1:
                    _thickness = 2;
                    break;
                case 2:
                    _thickness = 3;
                    break;
                case 3:
                    _thickness = 4;
                    break;
                case 4:
                    _thickness = 5;
                    break;
                default:
                    break;
            }
        }
    }
}
