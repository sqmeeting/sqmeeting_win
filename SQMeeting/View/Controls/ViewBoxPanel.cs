using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SQMeeting.View
{
    public class ViewBoxPanel : Panel
    {
        private double scale;
        protected override Size MeasureOverride(Size availableSize)
        {
            double width = 0;
            Size unlimitedSize = new Size(double.PositiveInfinity, double.PositiveInfinity);
            foreach (UIElement child in Children)
            {
                child.Measure(unlimitedSize);
                width += child.DesiredSize.Width;
            }
            scale = availableSize.Height / width;

            return availableSize;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            Transform scaleTransform = new ScaleTransform(scale, scale);
            double width = 0;
            foreach (UIElement child in Children)
            {
                child.RenderTransform = scaleTransform;
                child.Arrange(new Rect(new Point(0, scale * width), new Size(finalSize.Width / scale, child.DesiredSize.Height)));
                width += child.DesiredSize.Width;
            }

            return finalSize;
        }
    }
}
