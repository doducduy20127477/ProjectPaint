using GraphicsLibrary;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
        bool isDrawing;
        Point2D anchor = new Point2D(-1, -1);
        ShapeType currentShapeType = ShapeType.Line2D;
        string dashStyle = "";
        bool shiftMode;
        bool ctrlMode;
        List<IShape> shapes = new List<IShape>();
        Dictionary<int, List<Image>> images = new Dictionary<int, List<Image>>();
        List<IShape> redos = new List<IShape>();
        IShape preview;
        public MainWindow()
        {
            InitializeComponent();
            DllLoader.execute();
            var types = DllLoader.Types;
            KeyDown += new KeyEventHandler(OnButtonKeyDown);
            KeyUp += new KeyEventHandler(OnButtonKeyUp);
        }

        private void LineButton_Click(object sender, RoutedEventArgs e)
        {
            currentShapeType = ShapeType.Line2D;
        }

        private void Rectangle_Click(object sender, RoutedEventArgs e)
        {
            currentShapeType = ShapeType.Rectangle2D;
        }
        private void Ellipse_Click(object sender, RoutedEventArgs e)
        {
            currentShapeType = ShapeType.Ellipse2D;
        }
        private void DrawingCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            redos.Clear();
            double thickness = double.Parse(thicknessBox.Text);
            preview = (IShape)GetInstance($"{currentShapeType}");
            preview.StrokeThickness = thickness;
            isDrawing = true;
            Point currenCoord = e.GetPosition(DrawingCanvas);
            anchor.X = currenCoord.X;
            anchor.Y = currenCoord.Y;
            preview.HandleStart(anchor);
        }

        private void DrawingCanvas_MouseMove(object sender, MouseEventArgs e)
        {                                                                                               
            if (isDrawing)
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
                redraw();
                preview.Color = GlobalOptions.previewColor;
                DrawingCanvas.Children.Add(preview.Draw());
            }
        }

        private void DrawingCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            isDrawing = false;
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
                System.Drawing.Color temp = System.Drawing.Color.FromName(colorBox.Text);
                preview.Color = Color.FromArgb(temp.A, temp.R, temp.G, temp.B);
                shapes.Add(preview);
                DrawingCanvas.Children.Clear();
                redraw();
            }
        }

        private void CreateSaveBitmap(Canvas canvas, string filename)
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

        private void Save_Click(object sender, RoutedEventArgs e)
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
                CreateSaveBitmap(DrawingCanvas, fileName);
            }
        }

        private void CreateLoadBitmap(ref Canvas canvas, string filename)
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
            if (!images.ContainsKey(shapes.Count))
            {
                images[shapes.Count] = new List<Image>();
            }
            images[shapes.Count].Add(image);
            canvas.Children.Add(image);
        }

        private void Load_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Title = "Load image";
            openFileDialog.Filter = "Images|*.png;*.bmp;*.jpg";
            openFileDialog.RestoreDirectory = true;
            if (openFileDialog.ShowDialog() == true)
            {
                preview = null;
                CreateLoadBitmap(ref DrawingCanvas, openFileDialog.FileName);
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
                if (images.ContainsKey(0) && shapes.Count == 0)
                {
                    if (images[0].Count > 0)
                    {
                        images[0].RemoveAt(images[0].Count - 1);
                        DrawingCanvas.Children.RemoveAt(DrawingCanvas.Children.Count - 1);
                    }
                }
                if (shapes.Count > 0)
                {
                    if (images.ContainsKey(shapes.Count) && images[shapes.Count].Count > 0)
                    {
                        images[shapes.Count].RemoveAt(images[shapes.Count].Count - 1);
                    } else
                    {
                        redos.Add(shapes[shapes.Count - 1]);
                        shapes.RemoveAt(shapes.Count - 1);
                    }
                    DrawingCanvas.Children.RemoveAt(DrawingCanvas.Children.Count - 1);
                }
            } else if (e.Key == Key.Y && ctrlMode)
            {
                if (redos.Count > 0)
                {
                    shapes.Add(redos[redos.Count - 1]);
                    DrawingCanvas.Children.Add(shapes[shapes.Count - 1].Draw());
                    redos.RemoveAt(redos.Count - 1);
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

        public object GetInstance(string strFullyQualifiedName)
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

        private void DashSwitch_Checked(object sender, RoutedEventArgs e)
        {
            dashStyle = GlobalOptions.dashStyle;
        }

        private void DashSwitch_Unchecked(object sender, RoutedEventArgs e)
        {
            dashStyle = "";
        }
        private void redraw()
        {
            for (int i = 0; i < shapes.Count; i++)
            {
                if (images.ContainsKey(i))
                {
                    foreach (var image in images[i])
                    {
                        DrawingCanvas.Children.Add(image);
                    }
                }
                var element = shapes[i].Draw();
                DrawingCanvas.Children.Add(element);
            }
        }

        private void Pen_Click(object sender, RoutedEventArgs e)
        {
            currentShapeType = ShapeType.Point2D;
        }
    }
}
