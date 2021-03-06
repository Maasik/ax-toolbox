﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace AXToolbox.GpsLoggers
{
    [Serializable]
    public class TRKFile : LoggerFile
    {
        public TRKFile(string filePath, TimeSpan utcOffset)
            : base(filePath, utcOffset)
        {
            LogFileExtension = ".trk";
            SignatureStatus = SignatureStatus.NotSigned;

            //get logger info
            LoggerModel = TrackLogLines.FirstOrDefault(l => l[0] == 'P');
            if (!string.IsNullOrEmpty(LoggerModel))
                LoggerModel = LoggerModel.Substring(2).Trim();
        }

        public override GeoPoint[] GetTrackLog()
        {
            var utm = false;
            var track = new List<GeoPoint>();

            foreach (var line in TrackLogLines.Where(l => l.Length > 0))
            {
                switch (line[0])
                {
                    case 'G':
                        {
                            //Datum
                            var strFileDatum = line.Substring(2).Trim();
                            if (strFileDatum == "WGS 84") //Dirty hack!!!
                                strFileDatum = "WGS84";
                            loggerDatum = Datum.GetInstance(strFileDatum);
                        }
                        break;
                    //case 'L':
                    //    //Timezone
                    //    var tz = TimeZoneInfo.CreateCustomTimeZone("x", -TimeSpan.Parse(fields[1]), "", "");
                    //    break;
                    case 'T':
                        {
                            //Track point
                            var fields = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                            DateTime time = DateTime.MinValue;
                            if (DateTime.TryParse(fields[4] + " " + fields[5], out time)) // if datetime is invalid, time is 0000/00/00 00:00:00
                                time += utcOffset; // utc to local
                            var altitude = double.Parse(fields[7], NumberFormatInfo.InvariantInfo);
                            GeoPoint p;

                            if (utm)
                            {
                                //file with utm coordinates
                                p = new GeoPoint(
                                    time: time,
                                    datum: loggerDatum,
                                    zone: fields[1],
                                    easting: double.Parse(fields[2], NumberFormatInfo.InvariantInfo),
                                    northing: double.Parse(fields[3], NumberFormatInfo.InvariantInfo),
                                    altitude: altitude);
                            }
                            else
                            {
                                //file with latlon coordinates
                                // WARNING: 'º' is out of ASCII table: don't use split
                                var strLatitude = fields[2].Substring(0, fields[2].Length - 2);
                                var ns = fields[2][fields[2].Length - 1];
                                var strLongitude = fields[3].Substring(0, fields[3].Length - 2);
                                var ew = fields[3][fields[3].Length - 1];

                                var lat = double.Parse(strLatitude, NumberFormatInfo.InvariantInfo) * (ns == 'S' ? -1 : 1);
                                var lon = double.Parse(strLongitude, NumberFormatInfo.InvariantInfo) * (ew == 'W' ? -1 : 1);

                                p = new GeoPoint(
                                    time: time,
                                    datum: loggerDatum,
                                    latitude: lat,
                                    longitude: lon,
                                    altitude: altitude
                                    );
                            }

                            track.Add(p);
                        }
                        break;
                    case 'U':
                        {
                            //file coordinate units
                            var fields = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            utm = (fields[1] == "0");
                        }
                        break;
                }
            }

            return track.ToArray();
        }
        public override List<GeoWaypoint> GetMarkers()
        {
            return new List<GeoWaypoint>();
        }
        public override List<GoalDeclaration> GetGoalDeclarations()
        {
            return new List<GoalDeclaration>();
        }
    }
}