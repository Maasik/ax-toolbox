﻿using System;

namespace AXToolbox.Common
{
    public static class Physics
    {
        public static TimeSpan TimeDiff(Point point1, Point point2)
        {
            return point2.Time - point1.Time;
        }

        public static double Distance2D(Point point1, Point point2)
        {
            return Math.Sqrt(Math.Pow(point1.Coordinates.Easting - point2.Coordinates.Easting, 2) + Math.Pow(point1.Coordinates.Northing - point2.Coordinates.Northing, 2));
        }
        public static double Distance3D(Point point1, Point point2)
        {
            return Math.Sqrt(Math.Pow(point1.Coordinates.Easting - point2.Coordinates.Easting, 2) 
                + Math.Pow(point1.Coordinates.Northing - point2.Coordinates.Northing, 2)
                + Math.Pow(point1.Coordinates.Altitude - point2.Coordinates.Altitude, 2));
        }

        public static double Velocity2D(Point point1, Point point2)
        {
            return Distance2D(point1, point2) / TimeDiff(point1, point2).TotalSeconds;
        }
        public static double Velocity3D(Point point1, Point point2)
        {
            return Distance3D(point1, point2) / TimeDiff(point1, point2).TotalSeconds;
        }

        static public double Acceleration2D(Point point1, Point point2, Point point3)
        {
            return (Velocity2D(point2, point3) - Velocity2D(point1, point2)) / TimeDiff(point1, point3).TotalSeconds;
        }
        static public double Acceleration3D(Point point1, Point point2, Point point3)
        {
            return (Velocity2D(point2, point3) - Velocity3D(point1, point2)) / TimeDiff(point1, point3).TotalSeconds;
        }
        /// <summary>
        /// Computes the direction from the second point to the first. 0 is grid north.
        /// </summary>
        /// <param Name="point1">First point</param>
        /// <param Name="point2">Second point</param>
        /// <returns>Direction in degrees</returns>
        static public double Direction2D(Point point1, Point point2)
        {
            if (Distance2D(point1, point2) == 0)
                throw new ArgumentException("DuplicatedPoint: " + point1.ToString() + "/" + point2.ToString());

            var angle = Math.Acos((point1.Coordinates.Easting - point2.Coordinates.Northing) / Distance2D(point1, point2));
            if (point2.Coordinates.Northing < point1.Coordinates.Northing)
                angle = -angle;

            return (360 + 180 * (Math.PI / 2 + angle) / Math.PI) % 360;
        }
        /// <summary>
        /// Computes the angle between two given directions.
        /// </summary>
        /// <param Name="direction1">Direction 1</param>
        /// <param Name="direction2">Direction 2</param>
        /// <returns>Angle=Direction1-Direction2</returns>
        static public double DirectionSubstract(double direction1, double direction2)
        {
            var angle = Math.Abs(direction1 - direction2);
            if (angle > 180)
                angle = 360 - angle;

            return angle;
        }
    }
}