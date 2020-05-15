using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace TagsCloudVisualization
{
    public class Bitmaper
    {
        private static IList<Color> _availableColors = new List<Color>
        {
            Color.Aqua,
            Color.Bisque,
            Color.Black,
            Color.Blue,
            Color.BlueViolet,
            Color.Brown,
            Color.Chartreuse,
            Color.DarkOrange
        };

        public void Draw(IEnumerable<Rectangle> rectangles, string fileName)
        {
            var colorPointer = 0;
            var mbr = rectangles.GetMBR();

            var offsetX = mbr.X < 0 ? -mbr.X + 200 : 200;
            var offsetY = mbr.Y < 0 ? -mbr.Y + 200 : 200;

            using (var bitmap = new Bitmap(mbr.Width + 400, mbr.Height + 400))
            using (var graphics = Graphics.FromImage(bitmap))
            {
                foreach (var rectangle in rectangles)
                {
                    var brush = new SolidBrush(_availableColors[colorPointer]);
                    var offsetedRect = new Rectangle(rectangle.X + offsetX, rectangle.Y + offsetY, rectangle.Width,
                        rectangle.Height);

                    graphics.FillRectangle(brush, offsetedRect);

                    colorPointer = colorPointer + 1;

                    if (colorPointer == _availableColors.Count)
                    {
                        colorPointer = 0;
                    }
                }

                bitmap.Save(fileName);
            }
        }
    }
}