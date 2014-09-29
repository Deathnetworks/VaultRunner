using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Input;
using Zeta.Common;
using Zeta.Game;
using Zeta.Game.Internals.Actors;
using Zeta.Common.Xml;
using Zeta.XmlEngine;

namespace VaultRunner
{
    public class GreedWindow
    {
        private static Window _greedWindow;

        private static void CloseWindow()
        {
            _greedWindow.Close();
        }

        public static Window GetDisplay()
        {
            if (_greedWindow == null)
            {
                _greedWindow = new Window();
            }

            _greedWindow.Title = "Greeds Domain";
            _greedWindow.Width = 150;
            _greedWindow.Height = 150;
            _greedWindow.ResizeMode = ResizeMode.NoResize;
            _greedWindow.Background = Brushes.DarkGray;

            _greedWindow.Closed += WindowClosed;
            Demonbuddy.App.Current.Exit += WindowClosed;

            return _greedWindow;
        }

        private static void WindowClosed(object sender, EventArgs e)
        {
            if (_greedWindow != null)
            {
                _greedWindow.Closed -= WindowClosed;
                _greedWindow = null;
            }
        }
    }
}
