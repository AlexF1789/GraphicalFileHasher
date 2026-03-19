using Avalonia.Controls.Converters;
using System;
using System.ComponentModel;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace GraphicalFileHasher.Models;

public class FileHasher(string path) : INotifyPropertyChanged
{
    private static readonly int BUFFER_READER_SIZE = 131072;

    public string Path { get; init; } = path;
    private string? _hash;
    public string Hash
    {
        get => _hash ?? "TODO";
        private set
        {
            _hash = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Hash)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Returns the file's SHA1 hash
    /// </summary>
    /// <returns>a string which represent the SHA1 hash of the file</returns>
    public async Task<string> GetHash()
    {
        if (_hash is null)
        {
            await ComputeHash();
        }

        return Hash;
    }

    /// <summary>
    /// Computes the File's SHA1 hash and saves it into the correct attribute
    /// </summary>
    private async Task ComputeHash()
    {
        // let's check if the file exists
        if (!File.Exists(Path))
        {
            throw new IOException($"The file {Path} does not exist in the system!");
        }

        using SHA1 hasher = SHA1.Create();
        using FileStream fileStream = File.OpenRead(Path);
        using BufferedStream bufferedStream = new BufferedStream(fileStream, BUFFER_READER_SIZE);

        byte[] computedHash = hasher.ComputeHash(fileStream);

        Hash = Convert.ToHexStringLower(computedHash);
    }

    public override bool Equals(object? obj)
    {
        if (obj is FileHasher otherFile)
        {
            return otherFile.Path == Path;
        }

        return false;
    }

    public override int GetHashCode()
    {
        return Path.GetHashCode();
    }

    /// <summary>
    /// Compares the FileHasher based on the hash instead of the path
    /// </summary>
    /// <param name="otherFileHasher">is the FileHasher we want to compare to this</param>
    /// <returns>a boolean which indicates if they have the same hash indicating their content is the same</returns>
    public bool HasSameHash(FileHasher otherFileHasher)
    {
        return GetHash() == otherFileHasher.GetHash();
    }

    public override string ToString()
    {
        return $"{Path} - {Hash}";
    }
}
