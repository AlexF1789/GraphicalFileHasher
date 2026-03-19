using GraphicalFileHasher.Services;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace GraphicalFileHasher.Models;

public class HashingResults(HashService hashService) : INotifyPropertyChanged
{
    public HashService HashService = hashService;
    public ConcurrentDictionary<string, ConcurrentBag<string>> HashToFile { get; } = new();
    private int _finishedFilesCounter = 0;
    public int FinishedFilesCounter
    {
        get => _finishedFilesCounter;
        set
        {
            _finishedFilesCounter = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FinishedFilesCounter)));
        }
    }
    private readonly SemaphoreSlim CounterSemaphore = new(1, 1);

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Increments the completed file count
    /// </summary>
    /// <returns></returns>
    public async Task IncrementFinishedFiles()
    {
        // let's wait for the semaphore to access the counter
        await CounterSemaphore.WaitAsync();

        try
        {
            FinishedFilesCounter++;
        }
        finally
        {
            CounterSemaphore.Release();
        }
    }

    /// <summary>
    /// Adds the computed hash to the final collection
    /// </summary>
    /// <param name="fileHasher">is the FileHasher used to perform the calculations</param>
    public async Task AddToHashedFiles(FileHasher fileHasher)
    {
        HashToFile.GetOrAdd(await fileHasher.GetHash(), _ => new ConcurrentBag<string>())
            .Add(fileHasher.Path);
    }

    /// <summary>
    /// Checks if the results have been computed and stored
    /// </summary>
    /// <returns>a boolean indicating if the store operations have been completed</returns>
    public bool AreResultsComputed()
    {
        return !HashToFile.IsEmpty;
    }

    /// <summary>
    /// Counts the number of duplicates and redundant files
    /// </summary>
    /// <returns>the number of deleted and redundant files</returns>
    public DuplicatedFilesResult GetDuplicatedFiles()
    {
        DuplicatedFilesResult results = new DuplicatedFilesResult();

        foreach(var element in HashToFile)
        {
            if (element.Value.Count > 1)
            {
                results.DuplicatedFiles++;
                results.RedundantFiles += element.Value.Count - 1;
            }
        }

        return results;
    }
}
