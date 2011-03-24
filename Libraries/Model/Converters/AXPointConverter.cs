﻿using System;
using System.Globalization;
using System.Windows.Data;
using AXToolbox.Common;


namespace AXToolbox.Model.Converters
{
    [ValueConversion(typeof(AXPoint), typeof(String))]
    public class AXPointConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            AXPoint point = value as AXPoint;
            return point.ToString(AXPointInfo.Coords | AXPointInfo.Altitude).TrimEnd();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string strValue = value as string;
            AXPoint resultPoint;
            if (AXPoint.TryParse(strValue, out resultPoint))
                return resultPoint;
            else
            {
                return value;
            }
        }
    }
}