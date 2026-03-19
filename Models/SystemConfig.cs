using System;
using System.Collections.Generic;
using System.Text;

namespace GraphicalFileHasher.Models;

public class SystemConfig
{
    public int ProcessorNumber { get; init; }
    public bool DeleteFiles { get; init; }

    public SystemConfig(int? processorNumber, bool deleteFiles)
    {
        int systemProcessors = Environment.ProcessorCount;

        if (processorNumber is null || processorNumber <= 0 || processorNumber > systemProcessors)
            ProcessorNumber = systemProcessors;
        else
            ProcessorNumber = (int) processorNumber;
    }
}
