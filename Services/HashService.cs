using GraphicalFileHasher.Exceptions;
using GraphicalFileHasher.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GraphicalFileHasher.Services;

public class HashService
{
    public ICollection<FileHasher> Files { get; init; }
    public HashingResults Results { get; private set; }
    public SystemConfig Config { get; init; }

    private readonly ParallelOptions ParallelOptions;

    public HashService(ICollection<FileHasher> files, int? processorCount, bool deleteFiles)
    {
        Files = files;
        Results = new HashingResults(this);
        Config = new SystemConfig(processorCount, deleteFiles);
        ParallelOptions = new ParallelOptions { MaxDegreeOfParallelism = Config.ProcessorNumber };
    }

    public async Task StartCalculation()
    {
        await Parallel.ForEachAsync(Files, ParallelOptions, async (item, token) =>
        {
            await Results.AddToHashedFiles(item);
            await Results.IncrementFinishedFiles();
        });
    }

    public async Task DeleteDuplicates()
    {
        CheckResultsCompleted();

        await Parallel.ForEachAsync(Results.HashToFile, ParallelOptions, async (item, token) =>
        {
            // let's check if the hash is linked to more than a file
            if (item.Value.Count > 1)
            {
                // let's delete all the files except the first one which is the "original" one
                item.Value
                    .Skip(1)
                    .ToList()
                    .ForEach(path => File.Delete(path));
            }
        });
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
}
