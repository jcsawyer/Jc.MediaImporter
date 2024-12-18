using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using FluentAvalonia.UI.Controls;

namespace Jc.MediaImporter.Views.Import;

public partial class ConfigureImportView : UserControl
{
    public ConfigureImportView()
    {
        InitializeComponent();
    }

    protected void UpdateSelectAll()
    {
        var selectAll = this.FindControl<CommandBarToggleButton>("SelectAll");
        var photos = this.FindControl<ListBox>("Photos");
        var videos = this.FindControl<ListBox>("Videos");

        selectAll.IsChecked = photos.SelectedItems.Count == photos.Items.Count &&
                              videos.SelectedItems.Count == videos.Items.Count;
        selectAll.Label = selectAll.IsChecked ?? false ? "Deselect All" : "Select All";
    }

    private void Photos_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        UpdateSelectAll();
    }

    private void Photos_OnDataContextChanged(object? sender, EventArgs e)
    {
        UpdateSelectAll();
    }

    private void Videos_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        UpdateSelectAll();
    }

    private void Videos_OnDataContextChanged(object? sender, EventArgs e)
    {
        UpdateSelectAll();
    }

    private void SelectAll_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (sender is CommandBarToggleButton selectAll)
        {
            var photos = this.FindControl<ListBox>("Photos");
            var videos = this.FindControl<ListBox>("Videos");

            Dispatcher.UIThread.Post(() =>
            {
                if (selectAll.IsChecked == true)
                {
                    videos.SelectAll();
                    photos.SelectAll();
                }
                else
                {
                    videos.SelectedItems.Clear();
                    photos.SelectedItems.Clear();
                }
            });
        }
    }
}