﻿using System.Collections.ObjectModel;
using System.Windows;
using AiurVersionControl.SampleWPF.ViewModels;
using AiurVersionControl.SampleWPF.Views;

namespace AiurVersionControl.SampleWPF
{
    internal sealed partial class App
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var crudPresenter = new BooksCRUDPresenter();
            var mainWindow = new MainWindow { DataContext = crudPresenter };

            mainWindow.Show();
        }
    }
}