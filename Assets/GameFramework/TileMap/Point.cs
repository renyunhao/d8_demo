using System;
using UnityEngine;

namespace GameFramework
{
    [System.Serializable]
    public struct Point : IEquatable<Point>
    {
        public static Point Zero = new Point(0, 0);
        public static Point One = new Point(1, 1);

        public static Point TopOffset = new Point(0, 1);
        public static Point BottomOffset = new Point(0, -1);
        public static Point LeftOffset = new Point(-1, 0);
        public static Point RightOffset = new Point(1, 0);
        public static Point TopRightOffset = new Point(1, 1);
        public static Point TopLeftOffset = new Point(-1, 1);
        public static Point BottomRightOffset = new Point(1, -1);
        public static Point BottomLeftOffset = new Point(-1, -1);
        public static Point[] NeighbourOffset4 = new Point[] { BottomOffset, RightOffset, TopOffset, LeftOffset };
        public static Point[] NeighbourOffset8 = new Point[] { BottomOffset, RightOffset, TopOffset, LeftOffset, BottomRightOffset, TopRightOffset, TopLeftOffset, BottomLeftOffset };
        public static Point[] NeighbourOffset9 = new Point[] { Zero, BottomOffset, RightOffset, TopOffset, LeftOffset, BottomRightOffset, TopRightOffset, TopLeftOffset, BottomLeftOffset };

        public Point Top { get { return this + TopOffset; } }
        public Point Bottom { get { return this + BottomOffset; } }
        public Point Left { get { return this + LeftOffset; } }
        public Point Right { get { return this + RightOffset; } }

        public Point TopLeft { get { return this + TopLeftOffset; } }
        public Point TopRight { get { return this + TopRightOffset; } }
        public Point BottomLeft { get { return this + BottomLeftOffset; } }
        public Point BottomRight { get { return this + BottomRightOffset; } }

        public Point[] Neighbour4 { get { return new Point[] { Left, Top, Right, Bottom }; } }
        public Point[] Neighbour9 { get { return new Point[] { this, Left, Top, Right, Bottom, TopLeft, TopRight, BottomRight, BottomLeft }; } }

        public int x;
        public int y;

        public Point(Vector2 v)
        {
            this.x = Mathf.FloorToInt(v.x);
            this.y = Mathf.FloorToInt(v.y);
        }
        public Point(Vector3 v)
        {
            this.x = Mathf.FloorToInt(v.x);
            this.y = Mathf.FloorToInt(v.y);
        }
        public Point(Point p)
        {
            this.x = p.x;
            this.y = p.y;
        }
        public Point(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public override string ToString()
        {
            return "[" + x + "," + y + "]";
        }

        public bool CheckAdjacent(Point point)
        {
            return Mathf.Abs(point.x - this.x) == 1 || Mathf.Abs(point.y - this.y) == 1;
        }

        public override bool Equals(object obj)
        {
            if (obj is Point)
            {
                Point target = (Point)obj;
                return this.x == target.x && this.y == target.y;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return x.GetHashCode() * 100000 + y.GetHashCode();
        }

        public bool Equals(Point other)
        {
            return this == other;
        }

        public static explicit operator Point(Vector2 v)
        {
            return new Point(Mathf.FloorToInt(v.x), Mathf.FloorToInt(v.y));
        }
        public static explicit operator Point(Vector3 v)
        {
            return new Point(Mathf.FloorToInt(v.x), Mathf.FloorToInt(v.y));
        }
        public static explicit operator Vector2(Point c)
        {
            return new Vector2(c.x, c.y);
        }
        public static explicit operator Vector3(Point c)
        {
            return new Vector3(c.x, c.y);
        }
        public static Point operator +(Point a, Point b)
        {
            return new Point(a.x + b.x, a.y + b.y);
        }
        public static Point operator -(Point a, Point b)
        {
            return new Point(a.x - b.x, a.y - b.y);
        }
        public static Point operator *(Point a, int b)
        {
            return new Point(a.x * b, a.y * b);
        }

        public static bool operator ==(Point a, Point b)
        {
            return a.x == b.x && a.y == b.y;
        }
        public static bool operator !=(Point a, Point b)
        {
            return !(a == b);
        }
    }
}