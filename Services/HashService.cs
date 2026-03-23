using System;
using GraphicalFileHasher.Exceptions;
using GraphicalFileHasher.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Humanizer;
using MsBox.Avalonia;

namespace GraphicalFileHasher.Services;

public class HashService
{
    public ICollection<FileHasher> Files { get; init; }
    public HashingResults Results { get; private set; }
    public SystemConfig Config { get; init; }

    private readonly ParallelOptions _parallelOptions;

    public bool RedundantDeleted { get; private set; } = false;

    public HashService(ICollection<FileHasher> files, int? processorCount, bool deleteFiles)
    {
        Files = files;
        Results = new HashingResults(this);
        Config = new SystemConfig(processorCount, deleteFiles);
        _parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = Config.ProcessorNumber };
    }

    public async Task StartCalculation()
    {
        await Parallel.ForEachAsync(Files, _parallelOptions, async (item, token) =>
        {
            await Results.AddToHashedFiles(item);
            await Results.IncrementFinishedFiles();
        });
    }

    public async Task DeleteDuplicates()
    {
        CheckResultsCompleted();

        if (RedundantDeleted)
            throw new AlreadyDeletedException();
        
        await Parallel.ForEachAsync(Results.HashToFile, _parallelOptions, (item, token) =>
        {
            // let's check if the hash is linked to more than a file
            if (item.Value.Count > 1)
            {
                // let's delete all the files except the first one which is the "original" one
                item.Value
                    .Skip(1)
                    .ToList()
                    .ForEach(File.Delete);
            }

            return default;
        });

        // let's mark the redundant files as deleted and generate the log file
        RedundantDeleted = true;
        GenerateFileSummary();
    }

    /// <summary>
    /// Counts the number of duplicates and redundant files
    /// </summary>
    /// <returns>the number of deleted and redundant files</returns>
    public DuplicatedFilesResult GetNumberOfDuplicates()
    {
        CheckResultsCompleted();
        return Results.GetDuplicatedFiles();
    }

    /// <summary>
    /// Checks if the Result has been computed, throwing an exception if not
    /// </summary>
    /// <exception cref="NotExaminedException">in the case the method is called before the results are computed</exception>
    private void CheckResultsCompleted()
    {
        if (!Results.AreResultsComputed())
        {
            throw new NotExaminedException();
        }
    }

    /// <summary>
    /// Generates a file containing the summary of the computed hash indicating which files are duplicated
    /// </summary>
    /// <exception cref="NotExaminedException">in the case the method is called before the results are computed</exception>
    public void GenerateFileSummary()
    {
        if (!Results.AreResultsComputed())
        {
            throw new NotExaminedException();
        }

        var duplicatedFiles = Results.GetDuplicatedFiles();
        var dateTime = DateTime.Now;
        
        using var streamWriter = new StreamWriter(GetLogFilePath());
        streamWriter.AutoFlush = true;
        
        streamWriter.WriteLine("# === LOG FILE ===");
        streamWriter.WriteLine($"# date: {dateTime.ToLongDateString()} - time: {dateTime.ToLongTimeString()}");
        streamWriter.WriteLine($"#\n# total files: {Files.Count}");
        streamWriter.WriteLine($"# duplicated files: {duplicatedFiles.DuplicatedFiles} - redundant files: {duplicatedFiles.RedundantFiles}");

        if (RedundantDeleted)
            streamWriter.WriteLine("# Redundant files have been deleted!");
        else
            streamWriter.WriteLine("# The deletion process was not performed!");
        
        streamWriter.WriteLine("#\n#");

        Results.WriteResultsOnStreamWriter(streamWriter);
        streamWriter.Flush();
    }

    /// <summary>
    /// Returns a path for the log file to save data based on the system time
    /// </summary>
    /// <returns>a string suitable for the log file path</returns>
    private string GetLogFilePath()
    {
        var dateTime = DateTime.Now;

        return $"log_{dateTime.Year}_{dateTime:MM_dd:HH_mm_ss}.txt";
    }
}
