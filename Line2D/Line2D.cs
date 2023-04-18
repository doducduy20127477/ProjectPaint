using GraphicsLibrary;
using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace GraphicsLibrary
{
    public class Line2D : IShape
    {
        public override string Name { get => "Line2D"; set => throw new NotImplementedException(); }
        public Point2D Start = new Point2D();
        public Point2D End = new Point2D();

        public override void HandleStart(Point2D point)
        {
            Start = new Point2D(point);
        }

        public override void HandleEnd(Point2D point)
        {
            End = new Point2D(point);
        }

        public override UIElement Draw()
        {
            Line l = new Line()
            {
                X1 = Start.X,
                Y1 = Start.Y,
                X2 = End.X,
                Y2 = End.Y,
                StrokeThickness = StrokeThickness,
                Stroke = new SolidColorBrush(Color),
                StrokeDashArray = DoubleCollection.Parse(DashStyle),
            };

            return l;
        }

        public override void HandleShiftMode()
        {
            double diff = Math.Abs(End.X - Start.X) - Math.Abs(End.Y - Start.Y);
            if (diff > 0)
            {
                End.Y = Start.Y;
            }
            else
            {
                End.X = Start.X;
            }
        }
    }
}
