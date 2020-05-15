using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Drawing;
using System.Linq;
using AutoFixture;
using AutoFixture.Dsl;
using FluentAssertions;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using NUnit.Framework.Interfaces;

namespace TagsCloudVisualization.Tests
{
    public class CircularCloudLayouterTests
    {
        private readonly IFixture _fixture;
        private IEnumerable<Rectangle> _rectangles;

        public CircularCloudLayouterTests()
        {
            _fixture = new Fixture();
            _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
                .ForEach(b => _fixture.Behaviors.Remove(b));
            _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
            _fixture.Customize<Size>(ConfigureDefaults);
        }

        [SetUp]
        public void Setup()
        {
            _rectangles = new List<Rectangle>();
        }

        [TearDown]
        public void TearDown()
        {
            if (TestContext.CurrentContext.Result.Outcome.Status == TestStatus.Failed)
            {
                var bitmaper = new Bitmaper();
                bitmaper.Draw(_rectangles, $"rects_{TestContext.CurrentContext.Test.Name}_{DateTime.Now.Ticks}.bmp");
            }
        }

        [Test]
        public void PutNextRectangle_ShouldReturnRectangleWithSpecifiedSize()
        {
            Point center = _fixture.Create<Point>();
            CircularCloudLayouter sut = new CircularCloudLayouter(center);
            Size rectangleSize = _fixture.Create<Size>();
            var expectedSize = rectangleSize;

            var actual = sut.PutNextRectangle(rectangleSize);
            _rectangles = new List<Rectangle> { actual };

            actual.Size.Should().Be(expectedSize);
        }

        [Test]
        public void PutNextRectangle_ShouldCreateRectangleWithCenterInCloudCenter_WhenRectangleIsFirst()
        {
            Point center = _fixture.Create<Point>();
            CircularCloudLayouter sut = new CircularCloudLayouter(center);
            Size rectangleSize = _fixture.Create<Size>();
            var expectedLocation = new Point(center.X - rectangleSize.Width/2, center.Y - rectangleSize.Height/2);

            var actual = sut.PutNextRectangle(rectangleSize);
            
            actual.Location.Should().Be(expectedLocation);
        }

        [Test]
        public void PutNextRectangle_ShouldAddRectanglesInAccordanceToShapeOfACircle()
        {
            Point center = _fixture.Create<Point>();
            CircularCloudLayouter sut = new CircularCloudLayouter(center);
            var rectangleSizes = _fixture.Build<Size>()
                .With(size => size.Width, () => NumberValueFactories.InRange(10, 100))
                .With(size => size.Height, () => NumberValueFactories.InRange(10, 100))
                .CreateMany(100);

            _rectangles = rectangleSizes
                .Select(size => sut.PutNextRectangle(size));
            var roundness = CalculateRoundness(center, _rectangles);

            roundness.Should().BeGreaterOrEqualTo(0.8);
        }

        [Test]
        public void PutNextRectangle_ShouldCreateNonIntersectingRectangles()
        {
            Point center = _fixture.Create<Point>();
            CircularCloudLayouter sut = new CircularCloudLayouter(center);
            var rectangleSizes = _fixture.Build<Size>()
                .With(size => size.Width, () => NumberValueFactories.InRange(10, 100))
                .With(size => size.Height, () => NumberValueFactories.InRange(10, 100))
                .CreateMany(100);

            _rectangles = rectangleSizes
                .Select(size => sut.PutNextRectangle(size));

            var rectanglesIsIntersects = RectanglesIsIntersects(_rectangles.ToImmutableList());

            rectanglesIsIntersects.Should().BeFalse();
        }

        [Test]
        public void PutNextRectangle_ShouldAddRectanglesWithAllowableDensity()
        {
            Point center = _fixture.Create<Point>();
            CircularCloudLayouter sut = new CircularCloudLayouter(center);
            var rectangleSizes = _fixture.Build<Size>()
                .With(size => size.Width, () => NumberValueFactories.InRange(10, 100))
                .With(size => size.Height, () => NumberValueFactories.InRange(10, 100))
                .CreateMany(100);

            /*_rectangles = rectangleSizes
                .Select(size => sut.PutNextRectangle(size));
            _rectangles = _rectangles.Append(new Rectangle(new Point(center.X - 1, center.Y - 1), new Size(3, 3)));*/

            var bitmaper = new Bitmaper();

            foreach (var rectangleSize in rectangleSizes)
            {
                _rectangles = _rectangles.Append(sut.PutNextRectangle(rectangleSize));
                bitmaper.Draw(_rectangles, "debug.bmp");
            }
            var density = CalculateDensity(center, _rectangles);

            density.Should().BeGreaterOrEqualTo(0.8);
        }

        private bool RectanglesIsIntersects(IList<Rectangle> rectangles)
        {
            for (var i = 0; i < rectangles.Count - 1; i++)
            for (var j = i + 1; j < rectangles.Count; j++)
            {
                if (rectangles[i].IntersectsWith(rectangles[j]))
                {
                    return true;
                }
            }

            return false;
        }

        private double CalculateRoundness(Point center, IEnumerable<Rectangle> rectangles)
        {
            /*
             * Здесь упрощаем и возьмем минимальный прямоугольник, в который впишутся все наши прямоугольникки
             * Округлость будем считать как равномерность удаленности углов от точки центра облака
             */

            var mbr = GetMBRFromCenter(rectangles, center);

            var topLeft = mbr.Location;
            var topRight = new Point(mbr.Right, mbr.Top);
            var bottomRight = new Point(mbr.Right, mbr.Bottom);
            var bottomLeft = new Point(mbr.Left, mbr.Bottom);

            var distances = new List<double>
            {
                GetDistance(center, topLeft),
                GetDistance(center, topRight),
                GetDistance(center, bottomRight),
                GetDistance(center, bottomLeft)
            };

            double? minDistance = null, maxDistance = null;

            foreach (var distance in distances)
            {
                if (!minDistance.HasValue || distance < minDistance.Value)
                {
                    minDistance = distance;
                }

                if (!maxDistance.HasValue || distance > maxDistance.Value)
                {
                    maxDistance = distance;
                }
            }

            return maxDistance.Value / minDistance.Value;
        }

        private double CalculateDensity(Point center, IEnumerable<Rectangle> rectangles)
        {
            /*
             * Здесь попробуем подсчитать отношение общей площади треугольника к площади минимального прямоугольника,
             * в который они вписываются. Это не совсем правильный вариант, потому что он кореллирует только в случае,
             * если тэги располагаются ближе к окружности, но для примера подойдет
             */

            var rectanglesTotalArea = 0;

            foreach (var rectangle in rectangles)
            {
                rectanglesTotalArea += rectangle.Width * rectangle.Height;
            }

            var mbr = GetMBRFromCenter(rectangles, center);
            var mbrArea = (double) mbr.Width * mbr.Height;

            return rectanglesTotalArea / mbrArea;
        }

        private Rectangle GetMBRFromCenter(IEnumerable<Rectangle> rectangles, Point center)
            => rectangles
                .Append(new Rectangle(center, new Size(0, 0)))
                .GetMBR();

        private static double GetDistance(Point a, Point b)
        {
            return Math.Sqrt(Math.Pow((b.X - a.X), 2) + Math.Pow((b.Y - a.Y), 2));
        }

        private IPostprocessComposer<Size> ConfigureDefaults(ICustomizationComposer<Size> composer)
            => composer
                .With(size => size.Width, NumberValueFactories.NonNegative)
                .With(size => size.Height, NumberValueFactories.NonNegative);
    }

    public class TagsCloudTestPoints
    {
        public static Point ACenter()
        {
            return new Point(50, 50);
        }
    }

    public class NumberValueFactories
    {
        private static Random rnd = new Random();

        public static int InRange(int from, int to)
            => rnd.Next(from, to);

        public static int NonNegative()
            => InRange(0, int.MaxValue);
    }
}