using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace TagsCloudVisualization
{
    public class CircularCloudLayouter
    {
        private readonly Point _center;
        private readonly IList<Rectangle> _rectangles;
        private Direction _direction;

        public CircularCloudLayouter(Point center)
        {
            _center = center;
            _rectangles = new List<Rectangle>();
            _direction = Direction.Up;
        }

        public Rectangle PutNextRectangle(Size rectangleSize)
        {
            Rectangle? rect = null;

            if (!_rectangles.Any())
            {
                rect = new Rectangle(new Point(_center.X - rectangleSize.Width / 2, _center.Y - rectangleSize.Height / 2), rectangleSize);
            }
            else
            {
                var lastRect = _rectangles.Last();
                rect = CreateRectangleAround(lastRect, rectangleSize, _direction);
                var offsettingDirection = (Direction) (((int) _direction - 1 + 4) % 4);
                var changedDirection = (Direction)(((int)_direction + 1) % 4);
                rect = CorrectPosition(rect.Value, _rectangles, offsettingDirection);
                rect = CorrectPositionTo(rect.Value, _rectangles, changedDirection, _center);

                /*
                 * Идея в том чтобы посмотреть можно ли рядом с новым прямоугольником поместить такой же в новом направлении.
                 * Если можно то переключаемся на это направление
                 */
                var newRectInChangedDirection = CreateRectangleAround(rect.Value, rect.Value.Size, changedDirection);

                if (_rectangles.All(rectangle => !rectangle.IntersectsWith(newRectInChangedDirection)))
                {
                    _direction = changedDirection;
                }
            }

            _rectangles.Add(rect.Value);

            return rect.Value;
        }

        private Rectangle CorrectPosition(
            Rectangle rect, 
            IEnumerable<Rectangle> otherRects,
            Direction corrrectionDirection)
        {
            Rectangle intersectingRectangle;

            while ((intersectingRectangle = otherRects.FirstOrDefault(rectangle => rectangle.IntersectsWith(rect))) != Rectangle.Empty)
            {
                rect = MoveRectangleOver(rect, intersectingRectangle, corrrectionDirection);
            }

            return rect;
        }

        private Rectangle CorrectPositionTo(
            Rectangle rect, 
            IEnumerable<Rectangle> otherRects,
            Direction correctionDirection,
            Point limiter)
        {
            Rectangle result = rect;
            Rectangle intersectingRectangle; 
            Rectangle movedRect = result;



            while (PointIsInDirection(movedRect, limiter, correctionDirection))
            {
                movedRect = MoveRectangle(movedRect, 1, correctionDirection);

                if (otherRects.All(rectangle => !rectangle.IntersectsWith(rect)))
                {
                    result = movedRect;
                }
            }

            return result;
        }

        private Rectangle CreateRectangleAround(Rectangle rect, Size newRectSize, Direction direction)
        {
            int x = rect.X, y = rect.Y, width = newRectSize.Width, height = newRectSize.Height;
            var newRect = new Rectangle(x, y, width, height);

            return MoveRectangleOver(newRect, rect, direction);
        }

        private Rectangle MoveRectangleOver(Rectangle rect, Rectangle otherRectangle, Direction direction)
        {
            int x = rect.X, y = rect.Y, width = rect.Width, height = rect.Height;

            switch (direction)
            {
                case Direction.Up:
                    y = otherRectangle.Y - rect.Height;
                    break;

                case Direction.Down:
                    y = otherRectangle.Y + otherRectangle.Height;
                    break;

                case Direction.Right:
                    x = otherRectangle.X + otherRectangle.Width;
                    break;

                case Direction.Left:
                    x = otherRectangle.X - rect.Width;
                    break;
            }

            return new Rectangle(x, y, width, height);
        }

        private Rectangle MoveRectangle(Rectangle rect, int offset, Direction direction)
        {
            int x = rect.X, y = rect.Y, width = rect.Width, height = rect.Height;

            switch (direction)
            {
                case Direction.Up:
                    y -= offset;
                    break;

                case Direction.Down:
                    y += offset;
                    break;

                case Direction.Right:
                    x += offset;
                    break;

                case Direction.Left:
                    x -= offset;
                    break;
            }

            return new Rectangle(x, y, width, height);
        }

        private bool PointIsInDirection(Rectangle rect, Point point, Direction direction)
        {
            switch (direction)
            {
                case Direction.Up:
                    return point.Y < rect.Y;

                case Direction.Down:
                    return point.Y > rect.Y + rect.Height;

                case Direction.Right:
                    return point.X > rect.X + rect.Width;

                case Direction.Left:
                    return point.X < rect.X;

                default:
                    return false;
            }
        }
    }

    public enum Direction
    {
        Up = 0,
        Right = 1,
        Down = 2,
        Left = 3
    }
}