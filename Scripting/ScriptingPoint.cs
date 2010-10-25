﻿using System;
using System.Globalization;
using AXToolbox.Common;
using AXToolbox.MapViewer;

namespace AXToolbox.Scripting
{
    public class ScriptingPoint : ScriptingObject
    {
        protected Point point = null;

        public ScriptingPoint(string name, string type, string[] parameters, string displayMode, string[] displayParameters)
            : base(name, type, parameters, displayMode, displayParameters)
        {
        }

        public override void Resolve(FlightReport report)
        {
            switch (type)
            {
                case "SLL":
                    //WGS84 lat/lon
                    //SLL(<lat>, <long>, <alt>)
                    {
                        var lat = double.Parse(parameters[0], NumberFormatInfo.InvariantInfo);
                        var lng = double.Parse(parameters[1], NumberFormatInfo.InvariantInfo);
                        var alt = double.Parse(parameters[2], NumberFormatInfo.InvariantInfo) * 0.3048;
                        //point = new Point(DateTime.MinValue, Datum.WGS84, lat, lng, alt, settings.ReferencePoint.Datum, settings.ReferencePoint.Zone);
                        throw new NotImplementedException();
                    }
                    break;
                case "SUTM":
                    //UTM
                    //SUTM(<latZone>, <longZone>, <easting>, <northing>, <alt>)
                    {
                        var zone = parameters[0] + parameters[1];
                        var easting = double.Parse(parameters[2], NumberFormatInfo.InvariantInfo);
                        var northing = double.Parse(parameters[3], NumberFormatInfo.InvariantInfo);
                        var alt = double.Parse(parameters[4], NumberFormatInfo.InvariantInfo) * 0.3048;
                        throw new NotImplementedException();
                    }
                    break;
                case "LNP":
                    //nearest to point from list
                    //LNP(<desiredPoint>, <listPoint1>, <listPoint2>, ...)
                    //TODO: what kind of distance should be used? d2d, d3d or drad?
                    throw new NotImplementedException();
                case "LFT":
                    //first in time from list
                    //LFT(<listPoint1>, <listPoint2>, …)
                    throw new NotImplementedException();
                case "LLT":
                    //last in time from list
                    //LLT(<listPoint1>, <listPoint2>)
                    throw new NotImplementedException();
                case "LFNN":
                    //LFNN: first not null from list
                    //LFNN(<listPoint1>, <listPoint2>, …)
                    throw new NotImplementedException();
                case "LLNN":
                    //last not null
                    //LLNN(<listPoint1>, <listPoint2>, …)
                    throw new NotImplementedException();
                case "MVMD":
                    //MVMD: virtual marker drop
                    //MVMD(<number>)
                    throw new NotImplementedException();
                case "MPDG":
                    //pilot declared goal
                    //MPDG(<number>, <minTime>, <maxTime>)
                    throw new NotImplementedException();
                case "TLCH":
                    //TLCH: launch
                    //TLCH()
                    {
                        if (report != null)
                            point = report.LaunchPoint;
                    }
                    break;
                case "TLND":
                    //TLND: landing
                    //TLND()
                    {
                        if (report != null)
                            point = report.LandingPoint;
                    }
                    break;
                case "TNP":
                    //nearest to point
                    //TNP(<pointName>)
                    //TODO: what kind of distance should be used? d2d, d3d or drad?
                    throw new NotImplementedException();
                case "TNL":
                    //nearest to point list
                    //TNL(<listPoint1>, <listPoint2>, ...)
                    //TODO: what kind of distance should be used? d2d, d3d or drad?
                    throw new NotImplementedException();
                case "TDT":
                    //delayed in time
                    //TDT(<pointName>, <timeDelay>[, <maxTime>])
                    throw new NotImplementedException();
                case "TDD":
                    //delayed in distance
                    //TDD(<pointName>, <distanceDelay>[, <maxTime>])
                    throw new NotImplementedException();
                case "TAFI":
                    //area first in
                    //TAFI(<areaName>)
                    throw new NotImplementedException();
                case "TAFO":
                    //area first out
                    //TAFO(<areaName>)
                    throw new NotImplementedException();
                case "TALI":
                    //area last in
                    //TALI(<areaName>)
                    throw new NotImplementedException();
                case "TALO":
                    //area last out
                    //TALO(<areaName>)
                    throw new NotImplementedException();
            }
        }

        public override MapOverlay Display()
        {
            MapOverlay overlay = null;
            if (point != null)
            {
                var position = new System.Windows.Point(point.Easting, point.Northing);
                overlay = new WaypointOverlay(position, name);

            }
            return overlay;
        }
    }
}
