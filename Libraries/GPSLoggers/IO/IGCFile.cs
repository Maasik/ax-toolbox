﻿using AXToolbox.Common;
using Netline.BalloonLogger.SignatureLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AXToolbox.GpsLoggers
{
    [Serializable]
    public class IGCFile : LoggerFile
    {
        private double altitudeCorrection;

        private int dLatOffset = int.MinValue; //offset of the additional digit for latitude minutes relative to position data origin
        private int dLonOffset = int.MinValue; //offset of the additional digit for latitude minutes relative to position data origin
        private int vSpOffset = int.MinValue; //offset of the variometer vertical speed relative to position data origin

        public IGCFile(string logFilePath, TimeSpan utcOffset, string altitudeCorrectionsFilePath = null)
            : base(logFilePath, utcOffset)
        {
            IsAltitudeBarometric = true;
            LogFileExtension = ".igc";

            //get signature info
            var v = new Verifier() { AcceptOldKey = true };
            if (v.Verify(logFilePath))
                SignatureStatus = SignatureStatus.Genuine;
            else
                SignatureStatus = SignatureStatus.Counterfeit;

            //get logger info
            try
            {
                var loggerInfo = TrackLogLines.First(l => l.StartsWith("AXXX"));
                LoggerModel = loggerInfo.Substring(7);
                LoggerSerialNumber = loggerInfo.Substring(4, 3);
            }
            catch (InvalidOperationException) { }

            //get pilot info
            try
            {
                var pilotInfo = TrackLogLines.First(l => l.StartsWith("HFPID"));
                PilotId = int.Parse(pilotInfo.Substring(5));
            }
            catch (InvalidOperationException) { }


            //get date
            try
            {
                var dateInfo = TrackLogLines.First(l => l.StartsWith("HFDTE"));
                loggerDate = ParseDateAt(dateInfo, 9);

            }
            catch (InvalidOperationException) { }
            try
            {
                var dateInfo = TrackLogLines.Last(l => l.StartsWith("K"));
                loggerDate = ParseDateAt(dateInfo, 11);
            }
            catch (InvalidOperationException) { }

            //get IGC B record format
            try
            {
                var format = TrackLogLines.Last(l => l.StartsWith("I"));
                var BRecordVersion = int.Parse(format.Substring(1, 2));
                if (BRecordVersion >= 5)
                {
                    var latInfoPos = format.IndexOf("LAD");
                    dLatOffset = int.Parse(format.Substring(latInfoPos - 4, 2)) - 1 - 7; //7 is the offset to position data origin

                    var lonInfoPos = format.IndexOf("LOD");
                    dLonOffset = int.Parse(format.Substring(lonInfoPos - 4, 2)) - 1 - 7;

                    var vspInfoPos = format.IndexOf("VAR");
                    vSpOffset = int.Parse(format.Substring(vspInfoPos - 4, 2)) - 1 - 7;
                }
            }
            catch (InvalidOperationException) { }


            //check datum
            try
            {
                var datumInfo = TrackLogLines.Last(l => l.StartsWith("HFDTM"));
                loggerDatum = Datum.GetInstance(datumInfo.Substring(8));
                if (loggerDatum.Name != "WGS84")
                    throw new InvalidOperationException("IGC file datum must be WGS84");
            }
            catch (InvalidOperationException) { }

            //load altitude correction
            altitudeCorrection = 0;
            try
            {
                var strCorrection = File.ReadAllLines(altitudeCorrectionsFilePath).First(l => l.Trim().StartsWith(LoggerSerialNumber)).Split(new char[] { '=' })[1];
                altitudeCorrection = double.Parse(strCorrection) / 10; //altitude correction in file is in dm, convert to m
            }
            catch { }
            Debug.WriteLine(string.Format("Logger altitude correction={0}", altitudeCorrection));
        }

        public override GeoPoint[] GetTrackLog()
        {
            var lines = TrackLogLines.Where(l => l.StartsWith("B")).ToArray();
            var points = new GeoPoint[lines.Length];
            Parallel.For(0, lines.Length, i =>
            {
                points[i] = ParseTrackPoint(lines[i]);
            });

            return points.Where(p => p != null).ToArray();
        }
        public override List<GeoWaypoint> GetMarkers()
        {
            var markers = new List<GeoWaypoint>();
            foreach (var line in TrackLogLines.Where(l => l.StartsWith("E") && l.Substring(7, 3) == "XX0"))
            {
                var wp = ParseMarker(line);
                if (wp != null)
                    markers.Add(wp);
            }
            return markers;
        }
        public override List<GoalDeclaration> GetGoalDeclarations()
        {
            var declarations = new List<GoalDeclaration>();
            foreach (var line in TrackLogLines.Where(l => l.StartsWith("E") && l.Substring(7, 3) == "XX1"))
            {
                var wp = ParseDeclaration(line);
                declarations.Add(wp);
            }
            return declarations;
        }

        //main parser functions
        private GeoPoint ParseTrackPoint(string line)
        {
            return ParseFixAt(line, 7);
        }
        private GeoWaypoint ParseMarker(string line)
        {
            var number = line.Substring(10, 2);
            var p = ParseFixAt(line, 12);

            if (p != null)
                return new GeoWaypoint(number, p);
            else
                return null;
        }
        private GoalDeclaration ParseDeclaration(string line)
        {
            GoalDeclaration declaration = null;

            var time = ParseTimeAt(line, 1);
            var number = int.Parse(line.Substring(10, 2));
            var description = "[" + line.Substring(10) + "]";

            //parse altitude
            var strAltitude = line.Substring(12).Split(',')[1];
            var altitude = Parsers.ParseLengthOrNaN(strAltitude);

            // position declaration
            var strGoal = line.Substring(12).Split(',')[0];
            declaration = new GoalDeclaration(number, time, strGoal, altitude) { Description = description };

            return declaration;
        }

        //aux parser functions
        private DateTime ParseDateAt(string line, int pos)
        {
            int year = int.Parse(line.Substring(pos, 2));
            int month = int.Parse(line.Substring(pos - 2, 2));
            int day = int.Parse(line.Substring(pos - 4, 2));
            return new DateTime(year + ((year > 69) ? 1900 : 2000), month, day, 0, 0, 0, DateTimeKind.Local) + utcOffset; // utc to local
        }
        private DateTime ParseTimeAt(string line, int pos)
        {
            int hour = int.Parse(line.Substring(pos, 2));
            int minute = int.Parse(line.Substring(pos + 2, 2));
            int second = int.Parse(line.Substring(pos + 4, 2));
            return loggerDate + new TimeSpan(hour, minute, second);
        }
        private GeoPoint ParseFixAt(string line, int pos)
        {
            var isValid = line.Substring(pos + 17, 1) == "A";
            if (isValid)
            {
                var time = ParseTimeAt(line, 1); // the time is always at pos 1

                var dLat = (dLatOffset == int.MinValue) ? "0" : line.Substring(pos + dLatOffset, 1); //additional digit for minutes
                var latitude =
                    (
                    double.Parse(line.Substring(pos, 2)) + //degrees
                    double.Parse(line.Substring(pos + 2, 5) + dLat) / (60 * 1e4) //minutes
                    ) *
                    (line.Substring(pos + 7, 1) == "S" ? -1 : 1); //sign

                var dLon = (dLonOffset == int.MinValue) ? "0" : line.Substring(pos + dLonOffset, 1); //additional digit for minutes
                var longitude =
                    (
                    double.Parse(line.Substring(pos + 8, 3)) + //degrees
                    double.Parse(line.Substring(pos + 11, 5) + dLon) / (60 * 1e4) //minutes
                    ) *
                    (line.Substring(pos + 16, 1) == "W" ? -1 : 1); //sign

                var altitude = double.Parse(line.Substring(pos + 18, 5)) + altitudeCorrection;
                //var gpsAltitude = double.Parse(line.Substring(pos + 23, 5));
                //var accuracy = int.Parse(line.Substring(pos + 28, 4));
                //var satellites = int.Parse(line.Substring(pos + 32, 2));

                double vspeed = (vSpOffset == int.MinValue) ? double.NaN : double.Parse(line.Substring(pos + vSpOffset, 4)) / 10; //vertical speed (variometer)

                var p = new GeoPoint(
                    time: time,
                    datum: Datum.WGS84,
                    latitude: latitude,
                    longitude: longitude,
                    altitude: altitude
                    ) { VSpeed = vspeed };

                return p;
            }
            else
                return null;
        }
    }
}