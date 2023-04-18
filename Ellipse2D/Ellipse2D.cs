using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace GraphicsLibrary
{
    public class Ellipse2D : IShape
    {
        public Point2D TopLeft = new Point2D();
        public Point2D BottomRight = new Point2D();
        public new string Name { get => "Ellipse2D"; set => throw new NotImplementedException(); }

        public override void HandleStart(Point2D point)
        {
            TopLeft = new Point2D() { X = point.X, Y = point.Y };
        }

        public override void HandleEnd(Point2D point)
        {
            BottomRight = new Point2D() { X = point.X, Y = point.Y };
        }

        public override void HandleShiftMode()
        {
            double tWidth = Math.Abs(BottomRight.X - TopLeft.X);
            double tHeight = Math.Abs(BottomRight.Y - TopLeft.Y);
            double diff = tWidth < tHeight ? tWidth : tHeight;

            if (BottomRight.X > TopLeft.X && BottomRight.Y > TopLeft.Y)
            {
                if (tWidth > tHeight)
                {
                    BottomRight.X = TopLeft.X + diff;
                }
                else
                {
                    BottomRight.Y = TopLeft.Y + diff;
                }
            }
            else if (BottomRight.X > TopLeft.X && BottomRight.Y < TopLeft.Y)
            {
                if (tWidth > tHeight)
                {
                    BottomRight.X = TopLeft.X + diff;
                }
                else
                {
                    BottomRight.Y = TopLeft.Y - diff;
                }
            }
            else if (BottomRight.X < TopLeft.X && BottomRight.Y > TopLeft.Y)
            {
                if (tWidth > tHeight)
                {
                    BottomRight.X = TopLeft.X - diff;
                }
                else
                {
                    BottomRight.Y = TopLeft.Y + diff;
                }
            }
            else
            {
                if (tWidth > tHeight)
                {
                    BottomRight.X = TopLeft.X - diff;
                }
                else
                {
                    BottomRight.Y = TopLeft.Y - diff;
                }
            }
        }

        public override UIElement Draw()
        {
            double tWidth = BottomRight.X - TopLeft.X;
            double tHeight = BottomRight.Y - TopLeft.Y;
            var rect = new Ellipse()
            {
                Width = Math.Abs(tWidth),
                Height = Math.Abs(tHeight),
                StrokeThickness = StrokeThickness,
                Stroke = new SolidColorBrush(Color),
                StrokeDashArray = DoubleCollection.Parse(DashStyle),
            };
            if (tWidth > 0)
            {
                Canvas.SetLeft(rect, TopLeft.X);
            }
            else
            {
                Canvas.SetLeft(rect, BottomRight.X);
            }
            if (tHeight > 0)
            {
                Canvas.SetTop(rect, TopLeft.Y);

            }
            else
            {
                Canvas.SetTop(rect, BottomRight.Y);
            }
            return rect;
        }
    }
}
