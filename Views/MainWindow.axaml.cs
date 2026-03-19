using Avalonia.Controls;
using GraphicalFileHasher.Exceptions;
using GraphicalFileHasher.Models;
using GraphicalFileHasher.Services;
using Humanizer;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace GraphicalFileHasher.Views;

public partial class MainWindow : Window
{
    public ObservableCollection<FileHasher> Paths { get; set; } = new();
    public HashSet<FileHasher> PathsSet { get; private set; } = new();
    public HashService HashService { get; private set; }

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
        HashService = new(Paths, null, false);
    }

    public void AddPaths(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        try
        {
            int previousPaths = PathsSet.Count;
            LinkedList<FileHasher> newPaths = FileService.AddFilesFromPath(PathInput.Text, RecursiveInput.IsChecked);

            foreach (FileHasher newEntry in newPaths)
            {
                if (PathsSet.Add(newEntry))
                    Paths.Add(newEntry);
            }

            int count = newPaths.Count - previousPaths;

            UpdateDebugMessage($"Added {count} paths! [Total {Paths.Count}]");
            if (count > 0)
            {
                MessageBoxManager.GetMessageBoxStandard(
                    "Added paths",
                    $"Added {count} paths to the queue",
                    ButtonEnum.Ok,
                    MsBox.Avalonia.Enums.Icon.Plus
                ).ShowAsync();
            }

        }
        catch (InvalidPathException err)
        {
            switch (err.Motivation)
            {
                case InvalidPathException.Cause.INVALID_PATH:
                    UpdateDebugMessage("The path is not valid!");
                    MessageBoxManager.GetMessageBoxStandard(
                            "Invalid path",
                            "The provided path is not valid since it's blank!",
                            ButtonEnum.Ok,
                            MsBox.Avalonia.Enums.Icon.Error
                        ).ShowAsync();
                    break;

                case InvalidPathException.Cause.NOT_EXISTS:
                    UpdateDebugMessage("The path is not valid!");
                    MessageBoxManager.GetMessageBoxStandard(
                            "Invalid path",
                            "The path you specified is not valid since it doesn't exist!",
                            ButtonEnum.Ok,
                            MsBox.Avalonia.Enums.Icon.Error
                        ).ShowAsync();
                    break;

                case InvalidPathException.Cause.NO_RECURSIVE_FLAG:
                    UpdateDebugMessage("The specified path is a directory but the recursive option is off");
                    break;

            }
        }
    }

    public void UpdateDebugMessage(string text)
    {
        DebugText.Text = text;
    }

    private async void StartHashing(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        UpdateDebugMessage($"Starting the hashing procedure by using {HashService.Config.ProcessorNumber} CPUs");

        DateTime startingTime = DateTime.Now;
        await HashService.StartCalculation();
        TimeSpan timeInterval = DateTime.Now - startingTime;

        UpdateDebugMessage($"Hashed {Paths.Count} files in {timeInterval.Humanize(precision: 2)} using {HashService.Config.ProcessorNumber} CPUs");
    }

    private async void DeleteDuplicates(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        try
        {
            DuplicatedFilesResult numOfDuplicates = HashService.GetNumberOfDuplicates();

            // let's check if there are any files to delete
            if (!numOfDuplicates.DuplicatedExist())
            {
                await MessageBoxManager.GetMessageBoxStandard(
                    "No deletion needed",
                    "There are no files to delete!",
                    ButtonEnum.Ok,
                    MsBox.Avalonia.Enums.Icon.Info
                ).ShowAsync();
            }

            ButtonResult result = await MessageBoxManager.GetMessageBoxStandard(
                "Are you sure?",
                $"Are you sure you want to delete {numOfDuplicates.RedundantFiles} redundant files coming from {numOfDuplicates.DuplicatedFiles} duplicated files?",
                ButtonEnum.YesNo,
                MsBox.Avalonia.Enums.Icon.Question
            ).ShowAsync();

            if(result == ButtonResult.Yes)
            {
                await HashService.DeleteDuplicates();
                await MessageBoxManager.GetMessageBoxStandard(
                    "Duplicated files deleted",
                    "The duplicated files were succesfully deleted!",
                    ButtonEnum.Ok,
                    MsBox.Avalonia.Enums.Icon.Info
                ).ShowAsync();
            } 
            else
            {
                await MessageBoxManager.GetMessageBoxStandard(
                    "Operation aborted",
                    "You succesfully aborted the operation so no file was deleted!",
                    ButtonEnum.Ok,
                    MsBox.Avalonia.Enums.Icon.Info
                ).ShowAsync();
            }
        }
        catch(NotExaminedException _)
        {
            await MessageBoxManager.GetMessageBoxStandard(
                    "Hashing needed",
                    "You can't delete the files since the hashing operation hasn't started yet!",
                    ButtonEnum.Ok,
                    MsBox.Avalonia.Enums.Icon.Error
                ).ShowAsync();
        }
    }
}