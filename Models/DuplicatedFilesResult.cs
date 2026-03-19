using System;
using System.Collections.Generic;
using System.Text;

namespace GraphicalFileHasher.Models;

public class DuplicatedFilesResult
{
    public int DuplicatedFiles { get; set; } = 0;
    public int RedundantFiles { get; set; } = 0;

    public bool DuplicatedExist()
    {
        return DuplicatedFiles > 0;
    }
}
