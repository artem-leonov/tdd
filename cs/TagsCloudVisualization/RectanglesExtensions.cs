using System.Collections.Generic;
using System.Drawing;

namespace TagsCloudVisualization
{
    public static class RectanglesExtensions
    {
        public static Rectangle GetMBR(this IEnumerable<Rectangle> rectangles)
        {
            int? minX = null, maxX = null, minY = null, maxY = null;

            foreach (var rectangle in rectangles)
            {
                var rectMinX = rectangle.Location.X;
                var rectMaxX = rectMinX + rectangle.Width;
                var rectMaxY = rectangle.Location.Y;
                var rectMinY = rectMaxY - rectangle.Height;

                if (!minX.HasValue || rectMinX < minX.Value)
                {
                    minX = rectMinX;
                }

                if (!maxX.HasValue || rectMaxX > maxX.Value)
                {
                    maxX = rectMaxX;
                }

                if (!minY.HasValue || rectMinY < minY.Value)
                {
                    minY = rectMinY;
                }

                if (!maxY.HasValue || rectMaxY > maxY.Value)
                {
                    maxY = rectMaxY;
                }
            }

            var x = minX.Value;
            var y = maxY.Value;
            var width = maxX.Value - minX.Value;
            var height = maxY.Value - minY.Value;

            return new Rectangle(x, y, width, height);
        }
    }
}