﻿using System;
using System.Diagnostics;

namespace AXToolbox.Scripting
{
    public class ScriptingTask : ScriptingObject
    {
        protected string resultUnit;
        protected int resultPrecission;
        public Result Result { get; protected set; }

        internal ScriptingTask(ScriptingEngine engine, string name, string type, string[] parameters, string displayMode, string[] displayParameters)
            : base(engine, name, type, parameters, displayMode, displayParameters)
        { }

        public override void CheckConstructorSyntax()
        {
            AssertNumberOfParametersOrDie(ObjectParameters.Length == 1 && ObjectParameters[0] == "");

            resultPrecission = 2;
            switch (ObjectType)
            {
                default:
                    throw new ArgumentException("Unknown task type '" + ObjectType + "'");

                case "PDG":
                    resultUnit = "m";
                    break;
                case "JDG":
                    resultUnit = "m";
                    break;
                case "HWZ":
                    resultUnit = "m";
                    break;
                case "FIN":
                    resultUnit = "m";
                    break;
                case "FON":
                    resultUnit = "m";
                    break;
                case "HNH":
                    resultUnit = "m";
                    break;
                case "WSD":
                    resultUnit = "m";
                    break;
                case "GBM":
                    resultUnit = "m";
                    break;
                case "CRT":
                    resultUnit = "m";
                    break;
                case "RTA":
                    resultUnit = "s";
                    resultPrecission = 0;
                    break;
                case "ELB":
                    resultUnit = "°";
                    break;
                case "LRN":
                    resultUnit = "km^2";
                    break;
                case "MDT":
                    resultUnit = "m";
                    break;
                case "SFL":
                    resultUnit = "m";
                    break;
                case "MDD":
                    resultUnit = "m";
                    break;
                case "XDT":
                    resultUnit = "m";
                    break;
                case "XDI":
                    resultUnit = "m";
                    break;
                case "XDD":
                    resultUnit = "m";
                    break;
                case "ANG":
                    resultUnit = "°";
                    break;
                case "3DT":
                    resultUnit = "m";
                    break;
            }
        }
        public override void CheckDisplayModeSyntax()
        { }
        public override void Display()
        { }

        public override void Process()
        {
            base.Process();

            //removes filter if any
            Engine.ValidTrackPoints = Engine.Report.FlightTrack.ToArray();
            Engine.Report.AddNote(string.Format("{0}: track contains {1} valid points", ObjectName, Engine.ValidTrackPoints.Length));
        }

        public Result NewResult(double value)
        {
            return Result = Result.NewResult(ObjectName, ObjectType, value, resultUnit);
        }
        public Result NewNoResult()
        {
            return Result = Result.NewNoResult(ObjectName, ObjectType);
        }
        public Result NewNoFlight()
        {
            return Result = Result.NewNoFlight(ObjectName, ObjectType);
        }
    }
}
