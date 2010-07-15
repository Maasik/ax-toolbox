﻿using System;
using System.Text;
using System.Globalization;

namespace AXToolbox.Common
{
    [Flags]
    public enum PointData
    {
        None = 0,
        All = 0xffff,
        Date = 1,
        Time = 2,
        Altitude = 4,
        UTMCoords = 8,
        CompetitionCoords = 16,
        Validity = 32,
        Name = 64,
        Description = 128
    }

    public class Point
    {
        private UtmCoordinates coordinates;
        public UtmCoordinates Coordinates
        {
            get { return coordinates; }
            set { coordinates = value; }
        }

        private DateTime time;
        public DateTime Time
        {
            get { return time; }
            set { time = value; }
        }

        public Point(UtmCoordinates coordinates, DateTime time)
        {
            this.coordinates = coordinates;
            this.time = time;
        }

        public override string ToString()
        {
            //return ToString(PointData.Time | PointData.CompetitionCoords | PointData.Altitude);
            return ToString(PointData.All & ~PointData.Date);
        }
        public virtual string ToString(PointData data)
        {
            var str = new StringBuilder();

            if ((data & PointData.Date) > 0)
                str.Append(Time.ToLocalTime().ToString("yyyy/MM/dd "));

            if ((data & PointData.Time) > 0)
                str.Append(Time.ToLocalTime().ToString("HH:mm:ss "));

            if ((data & PointData.UTMCoords) > 0 || (data & PointData.CompetitionCoords) > 0)
            {
                if ((data & PointData.UTMCoords) > 0)
                    str.Append(string.Format("{0} {1:000000} {2:0000000} ", coordinates.Zone, coordinates.Easting, coordinates.Northing));

                if ((data & PointData.CompetitionCoords) > 0)
                    str.Append(string.Format("({0:0000}/{1:0000}) ", coordinates.Easting % 1e5 / 10, coordinates.Northing % 1e5 / 10));
            }

            if ((data & PointData.Altitude) > 0)
                str.Append(coordinates.Altitude.ToString("0 "));

            return str.ToString();
        }

        public static bool TryParse(string strPoint, out Point point)
        {
            if (TryParseCoordsOnly(strPoint, out point))
                return true;
            else
                return false;
        }

        private static bool TryParseCoordsOnly(string strPoint, out Point point)
        {
            var fields = strPoint.ToUpper().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            double easting, northing;

            if (fields.Length == 3 && double.TryParse(fields[1], out easting) && double.TryParse(fields[2], out northing))
            {
                throw new NotImplementedException();
                //var coords = new UtmCoordinates(
                //    zone: fields[0],
                //    easting: double.Parse(fields[1], NumberFormatInfo.InvariantInfo),
                //    northing: double.Parse(fields[2], NumberFormatInfo.InvariantInfo),
                //    altitude: 0
                //);
                //var time = DateTime.Now.StripTimePart().ToUniversalTime();

                //point = new Point(coords, time);

                //return true;
            }
            else
            {
                point = null;
                return false;
            }
        }
    }

    public class TrackPoint : Point
    {
        private bool isValid;
        public bool IsValid
        {
            get { return isValid; }
            set { isValid = value; }
        }

        public TrackPoint(Point point)
            : base(point.Coordinates, point.Time)
        {
            isValid = true;
        }
        public TrackPoint(UtmCoordinates coordinates, DateTime time)
            : base(coordinates, time)
        {
            isValid = true;
        }

        public override string ToString()
        {
            //return ToString(PointData.Time | PointData.CompetitionCoords | PointData.Altitude);
            return ToString(PointData.All & ~PointData.Date);
        }
        public override string ToString(PointData data)
        {
            var str = new StringBuilder();

            str.Append(base.ToString(data));

            if ((data & PointData.Validity) > 0)
                str.Append(IsValid ? "" : "invalid ");

            return str.ToString();
        }
    }
}
