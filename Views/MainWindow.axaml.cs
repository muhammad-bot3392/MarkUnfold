using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using MarkItDownGUI.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MarkItDownGUI.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        AvaloniaXamlLoader.Load(this);
        Closing += OnWindowClosing;

        AddHandler(DragDrop.DropEvent, OnWindowDrop);
        AddHandler(DragDrop.DragOverEvent, OnWindowDragOver);
    }

    private void OnWindowClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (DataContext is MainViewModel vm && vm.IsConverting)
        {
            vm.CancelConversionCommand.Execute(null);
            e.Cancel = true;
        }
    }

    private void OnWindowDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = DragDropEffects.Copy;
        e.Handled = true;
    }

    private void OnWindowDrop(object? sender, DragEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            ExtractAndAddFiles(e);
        }
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = DragDropEffects.Copy;
        e.Handled = true;
    }

    private void OnDragEnter(object? sender, DragEventArgs e)
    {
        if (sender is Border border)
        {
            border.Background = this.FindResource("DropZoneActiveBrush") as IBrush;
        }
    }

    private void OnDragLeave(object? sender, DragEventArgs e)
    {
        if (sender is Border border)
        {
            border.Background = this.FindResource("CardBackgroundBrush") as IBrush;
        }
    }

    private void OnDrop(object? sender, DragEventArgs e)
    {
        if (sender is Border border)
        {
            border.Background = this.FindResource("CardBackgroundBrush") as IBrush;
        }

        ExtractAndAddFiles(e);
    }

    private void OnFileItemPointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        if (sender is Border border && border.DataContext is Models.FileItem fileItem
            && DataContext is MainViewModel vm)
        {
            vm.SelectFileCommand.Execute(fileItem);
        }
    }

    private void ExtractAndAddFiles(DragEventArgs e)
    {
        if (DataContext is not MainViewModel vm) return;

        var paths = new List<string>();

        foreach (var item in e.DataTransfer.Items)
        {
            foreach (var fmt in item.Formats)
            {
                if (item.TryGetRaw(fmt) is IStorageItem storageItem)
                {
                    var localPath = storageItem.Path?.LocalPath;
                    if (!string.IsNullOrEmpty(localPath))
                    {
                        if (File.Exists(localPath) || Directory.Exists(localPath))
                            paths.Add(localPath);
                    }
                }
            }
        }

        foreach (var item in e.DataTransfer.Items)
        {
            foreach (var fmt in item.Formats)
            {
                if (item.TryGetRaw(fmt) is string text)
                {
                    var trimmed = text.Trim().Trim('"').Trim('\'');
                    if (string.IsNullOrWhiteSpace(trimmed)) continue;
                    if (File.Exists(trimmed) || Directory.Exists(trimmed))
                        paths.Add(trimmed);
                }
            }
        }

        if (paths.Count > 0)
            vm.AddFiles(paths);
    }
}
