﻿using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Windows;

namespace HVACCalculator;

/// <summary>
/// Global window state manager for synchronized minimize behavior
/// </summary>
public static class WindowStateManager
{
    private static readonly List<Window> TrackedWindows = [];
    private static bool _isApplyingBulkStateChange = false;

    public static void RegisterWindow(Window window)
    {
        if (!TrackedWindows.Contains(window))
        {
            TrackedWindows.Add(window);
            window.StateChanged += Window_StateChanged;
            window.Closed += (s, e) => TrackedWindows.Remove(window);
        }
    }

    private static void Window_StateChanged(object? sender, System.EventArgs e)
    {
        if (_isApplyingBulkStateChange || sender is not Window window) return;

        if (window.WindowState == WindowState.Minimized)
        {
            // Minimize all other tracked windows
            _isApplyingBulkStateChange = true;
            foreach (var w in TrackedWindows)
            {
                if (w != window && w.WindowState != WindowState.Minimized)
                {
                    w.WindowState = WindowState.Minimized;
                }
            }
            _isApplyingBulkStateChange = false;
        }
        else if (window.WindowState == WindowState.Normal)
        {
            // Restore all other tracked windows to normal if ANY window is restored
            _isApplyingBulkStateChange = true;
            foreach (var w in TrackedWindows)
            {
                if (w != window && w.WindowState == WindowState.Minimized)
                {
                    w.WindowState = WindowState.Normal;
                }
            }
            _isApplyingBulkStateChange = false;
        }
    }
}

/// <summary>
/// Interactielogica voor App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        AppSettings.Load();
        base.OnStartup(e);
    }
}
