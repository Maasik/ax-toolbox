﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using AXToolbox.Common;
using AXToolbox.Common.Geodesy;
using AXToolbox.Common.IO;
using FlightAnalyzer.Properties;
using GMap.NET;
using GMap.NET.WindowsPresentation;

namespace FlightAnalyzer
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MapType[] allowedMaptypes = new MapType[]{
            MapType.GoogleMap, MapType.GoogleHybrid, 
            MapType.BingMap, MapType.BingHybrid
        };

        private FlightSettings flightSettings;
        private CoordAdapter caToGMap;
        private FlightReport report;
        private GMapMarker trackMarker;
        private GMapMarker pointerMarker;
        private int mapTypeIdx = 0;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (new SettingsWindow().ShowDialog() == true)
            {
                // config gmaps
                GMaps.Instance.UseRouteCache = true;
                GMaps.Instance.UseGeocoderCache = true;
                GMaps.Instance.UsePlacemarkCache = true;
                GMaps.Instance.Mode = AccessMode.ServerAndCache;

                // config map
                MainMap.MapType = allowedMaptypes[mapTypeIdx];
                MainMap.DragButton = MouseButton.Left;
                MainMap.MouseWheelZoomType = MouseWheelZoomType.MousePositionAndCenter;
                MainMap.MaxZoom = 20; //tiles available up to zoom 17
                MainMap.MinZoom = 10;
                MainMap.Zoom = 12;
                MainMap.CurrentPosition = new PointLatLng(41.97, 2.78);

                InitSettings();
            }
            else
            {
                Close();
            }
        }
        private void MainWindow_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] droppedFilePaths = e.Data.GetData(DataFormats.FileDrop, true) as string[];

                Cursor = Cursors.Wait;

                try
                {
                    report = FlightReport.LoadFromFile(droppedFilePaths[0], flightSettings);
                    report.Notes.Add("This is a note");
                    DataContext = report;

                    SetSlider();

                    //Clear Map
                    MainMap.Markers.Clear();

                    //Add track to map
                    trackMarker = GetTrackMarker();
                    MainMap.Markers.Add(trackMarker);

                    //Add movable pointer
                    pointerMarker = GetMarker(GetVisibleTrack()[0], "*", GetVisibleTrack()[0].ToString(), Brushes.Orange);
                    MainMap.Markers.Add(pointerMarker);

                    //// Add launch and landing to map
                    //MainMap.Markers.Add(GetMarker(report.LaunchPoint, "↑", "Launch Point: " + report.LaunchPoint.ToString(), Brushes.Lime));
                    //MainMap.Markers.Add(GetMarker(report.LandingPoint, "↓", "Landing Point: " + report.LandingPoint.ToString(), Brushes.Lime));

                    // Add dropped markers to map
                    foreach (var m in report.Markers)
                        MainMap.Markers.Add(GetMarker(m, m.Name, "Marker " + m.ToString(), Brushes.Yellow));

                    // Add goal declarations to map
                    foreach (var m in report.DeclaredGoals)
                        MainMap.Markers.Add(GetMarker(m, m.Name, "Declaration " + m.ToString(), Brushes.Red));

                    MainMap.CurrentPosition = pointerMarker.Position;

                }
                catch (InvalidOperationException)
                {
                    //silently reject unknown log files
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
                finally
                {
                    Cursor = Cursors.Arrow;
                }
            }
        }
        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.OemPlus:
                case Key.Add:
                    MainMap.Zoom += 1;
                    break;
                case Key.OemMinus:
                case Key.Subtract:
                    MainMap.Zoom -= 1;
                    break;
                case Key.OemPeriod:
                    MainMap.Zoom = 12;
                    break;
                case Key.P:
                    PrefetchTiles();
                    break;
                case Key.M:
                    MainMap.MapType = allowedMaptypes[++mapTypeIdx % allowedMaptypes.Length];
                    break;
            }
        }
        private void slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdatePointer();
        }
        private void radio_Checked(object sender, RoutedEventArgs e)
        {
            if (report != null)
            {

                SetSlider();

                MainMap.Markers.Remove(trackMarker);
                trackMarker = GetTrackMarker();
                MainMap.Markers.Add(trackMarker);

                UpdatePointer();
            }
        }
        private void checkLock_Checked(object sender, RoutedEventArgs e)
        {
            UpdatePointer();
        }

        private void InitSettings()
        {
            //TODO: check for errors and/or use a constructor
            flightSettings = new FlightSettings()
            {
                Date = Settings.Default.Date,
                Am = Settings.Default.Am,
                TimeZone = Settings.Default.TimeZone,
                Datum = Settings.Default.Datum,
                UtmZone = Settings.Default.UtmZone,
                Qnh = Settings.Default.Qnh,
                AllowedGoals = WPTFile.Load(Settings.Default.GoalsFile, Settings.Default.Datum, Settings.Default.UtmZone),
                DefaultAltitude = Settings.Default.DefaultAltitude,
                MinVelocity = Settings.Default.MinVelocity,
                MaxAcceleration = Settings.Default.MaxAcceleration,
                InterpolationInterval = Settings.Default.InterpolationInterval
            };

            caToGMap = new CoordAdapter(flightSettings.Datum, "WGS84");
        }

        private GMapMarker GetTrackMarker()
        {
            List<PointLatLng> points = new List<PointLatLng>();

            var ca = new CoordAdapter(Settings.Default.Datum, "WGS84");
            foreach (var p in GetVisibleTrack())
            {
                var llp = ca.ConvertToLatLong(
                    new AXToolbox.Common.Point() { Zone = Settings.Default.UtmZone, Easting = p.Easting, Northing = p.Northing }
                    );
                points.Add(new PointLatLng(llp.Latitude, llp.Longitude));
            }

            GMapMarker marker = new GMapMarker(points[0]);
            marker.Route.AddRange(points);
            marker.RegenerateRouteShape(MainMap);

            // Override default shape
            var myPath = new System.Windows.Shapes.Path()
            {
                Data = (marker.Shape as Path).Data, //use the generated geometry
                Effect = new BlurEffect() { KernelType = KernelType.Box, Radius = 0.25 },
                Stroke = (radioLogger.IsChecked.Value) ? Brushes.Red : Brushes.Blue,
                StrokeThickness = 2
            };
            marker.Shape = myPath;
            marker.ZIndex = -1;
            marker.ForceUpdateLocalPosition(MainMap);

            return marker;
        }
        private GMapMarker GetMarker(IPosition p, string text, string toolTip, Brush brush)
        {
            var llp = caToGMap.ConvertToLatLong(new AXToolbox.Common.Point() { Zone = flightSettings.UtmZone, Easting = p.Easting, Northing = p.Northing });
            var marker = new GMapMarker(new PointLatLng(llp.Latitude, llp.Longitude));
            marker.Shape = new Tag(text, toolTip, brush);
            marker.ForceUpdateLocalPosition(MainMap);
            return marker;
        }

        private IList<AXToolbox.Common.Point> GetVisibleTrack()
        {
            return (radioLogger.IsChecked.Value) ? report.Track : report.OriginalTrack;
        }
        private void UpdatePointer()
        {
            if (pointerMarker != null)
            {
                var t = (int)sliderTime.Value;
                var p = GetVisibleTrack()[t];
                var llp = caToGMap.ConvertToLatLong(p);

                pointerMarker.Position = new PointLatLng()
                {
                    Lat = llp.Latitude,
                    Lng = llp.Longitude
                };
                ((Tag)pointerMarker.Shape).SetTooltip(p.ToString());
                pointerMarker.ForceUpdateLocalPosition(MainMap);
                
                if (checkLock.IsChecked.Value)
                    MainMap.CurrentPosition = pointerMarker.Position;

                textblockTime.Text = p.Time.ToString("HH:mm:ss");
            }
        }
        private void SetSlider()
        {
            var visibleTrack = GetVisibleTrack();

            sliderTime.Minimum = 0;
            sliderTime.Maximum = visibleTrack.Count - 1;
            sliderTime.Value = 0;
        }
        private void PrefetchTiles()
        {
            RectLatLng area = MainMap.SelectedArea;

            if (!area.IsEmpty)
            {
                for (int z = (int)MainMap.Zoom; z <= 17; z++) //tiles available up to zoom 17
                {
                    var tiles = MainMap.Projection.GetAreaTileList(area, z, 0);
                    TilePrefetcher pref = new TilePrefetcher();
                    pref.Start(tiles, z, MainMap.MapType, 0);
                    tiles.Clear();
                    /*
                    MessageBoxResult res = MessageBox.Show(string.Format("PrefetchTiles {0} tiles at zoom {1}?", tiles.Count, z), "PrefetchTiles map", MessageBoxButton.YesNoCancel);

                    if (res == MessageBoxResult.Yes)
                    {
                        TilePrefetcher pref = new TilePrefetcher();
                        pref.ShowCompleteMessage = true;
                        pref.Start(tiles, z, MainMap.MapType, 100);
                    }
                    else if (res == MessageBoxResult.No)
                    {
                        continue;
                    }
                    else if (res == MessageBoxResult.Cancel)
                    {
                        break;
                    }
                    */
                }
            }
            else
            {
                MessageBox.Show("Select map area holding right mouse button + ALT", "PrefetchTiles map", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
        }
    }
}
