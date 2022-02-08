﻿using PowerAccent.Core.Services;
using PowerAccent.Core.Tools;
using System;
using System.Windows;
using static PowerAccent.Core.Tools.Enums;

namespace PowerAccent.UI;

public partial class Selector : Window
{
    private PowerAccentService _powerAccentService = new PowerAccentService();
    private readonly SettingsService _settingService = new SettingsService();

    private int index = -1;

    private bool _useCaretPosition = false;

    public Selector()
    {
        InitializeComponent();
        _useCaretPosition = _settingService.UseCaretPosition;
        Application.Current.MainWindow.ShowActivated = false;
        Application.Current.MainWindow.Topmost = true;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        _powerAccentService.KeyDown += PowerAccent_KeyDown;
        _powerAccentService.KeyUp += PowerAccent_KeyUp;
        this.Visibility = Visibility.Hidden;
    }

    private void PowerAccent_KeyUp(LetterKey? letterKey)
    {
        if (this.Visibility == Visibility.Visible && !letterKey.HasValue)
        {
            this.Visibility = Visibility.Collapsed;
            index = -1;
            if (characters.SelectedItem != null)
            {
                WindowsFunctions.Insert((char)characters.SelectedItem);
            }
        }
    }

    private bool PowerAccent_KeyDown(LetterKey? letterKey, ArrowKey? arrowKey)
    {
        if (this.Visibility != Visibility.Visible && letterKey.HasValue && arrowKey.HasValue)
        {
            FillListBox(letterKey.Value);
            this.Visibility = Visibility.Visible;
            CenterWindow();
        }

        if (this.Visibility == Visibility.Visible && arrowKey.HasValue)
        {
            if (index == -1)
            {
                if (arrowKey.Value == ArrowKey.Left)
                    index = characters.Items.Count / 2 - 1;

                if (arrowKey.Value == ArrowKey.Right)
                    index = characters.Items.Count / 2;

                if (index < 0) index = 0;
                if (index > characters.Items.Count - 1) index = characters.Items.Count - 1;

                characters.SelectedIndex = index;
                return false;
            }

            if (arrowKey.Value == ArrowKey.Left && index > 0)
                --index;
            if (arrowKey.Value == ArrowKey.Right && index < characters.Items.Count - 1)
                ++index;

            characters.SelectedIndex = index;
            return false;
        }


        return true;
    }

    private void FillListBox(LetterKey letter)
    {
        characters.ItemsSource = _settingService.GetLetterKey(letter);
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        Settings settings = new Settings();
        settings.Show();
    }

    private void MenuExit_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }

    private void CenterWindow()
    {
        //Method1
        UpdateLayout();
        Point position = CalculatePosition();
        this.Left = position.X;
        this.Top = position.Y;
    }

    private Point CalculatePosition()
    {
        var activeDisplay = WindowsFunctions.GetActiveDisplay();
        Rect screen = new Rect(new Point(activeDisplay.Location.X, activeDisplay.Location.Y), new Size(activeDisplay.Size.Width, activeDisplay.Size.Height));
        Point window = new Point(((System.Windows.Controls.Panel)Application.Current.MainWindow.Content).ActualWidth, ((System.Windows.Controls.Panel)Application.Current.MainWindow.Content).ActualHeight);
        if (!_useCaretPosition)
        {
            return GetPosition(screen, window);
        }

        System.Drawing.Point carretPixel = WindowsFunctions.GetCaretPosition();
        if (carretPixel.X == 0 && carretPixel.Y == 0)
        {
            return GetPosition(screen, window);
        }

        PresentationSource source = PresentationSource.FromVisual(this);
        if (source == null)
        {
            return GetPosition(screen, window);
        }

        Point dpi = new Point(activeDisplay.Dpi, activeDisplay.Dpi);
        Point carret = new Point(carretPixel.X / dpi.X, carretPixel.Y / dpi.Y);
        var left = carret.X - window.X / 2; // X default position
        var top = carret.Y - window.Y - 20; // Y default position

        return new Point(left < screen.X ? screen.X : (left + window.X > (screen.X + screen.Width) ? (screen.X + screen.Width) - window.X : left)
            , top < screen.Y ? carret.Y + 20 : top);
    }

    protected override void OnClosed(EventArgs e)
    {
        _powerAccentService.Dispose();
        base.OnClosed(e);
    }

    private Point GetPosition(Rect screen, Point window)
    {
        int offset = 10;
        Position position = _settingService.Position;

        double pointX = position switch
        {
            var x when
                x == Position.Top ||
                x == Position.Bottom ||
                x == Position.Center
                => screen.X + screen.Width / 2 - window.X / 2,
            var x when
                x == Position.TopLeft ||
                x == Position.Left ||
                x == Position.BottomLeft
                => screen.X + offset,
            var x when
                x == Position.TopRight ||
                x == Position.Right ||
                x == Position.BottomRight
                => screen.X + screen.Width - (window.X + offset),
        };

        double pointY = position switch
        {
            var x when
                x == Position.TopLeft ||
                x == Position.Top ||
                x == Position.TopRight
                => screen.Y + offset,
            var x when
                x == Position.Left ||
                x == Position.Center ||
                x == Position.Right
                => screen.Y + screen.Height / 2 - window.Y / 2,
            var x when
                x == Position.BottomLeft ||
                x == Position.Bottom ||
                x == Position.BottomRight
                => screen.Y + screen.Height - (window.Y + offset),
        };

        return new Point(pointX, pointY);
    }

    public void RefreshSettings()
    {
        _settingService.Reload();
        _useCaretPosition = _settingService.UseCaretPosition;
    }
}