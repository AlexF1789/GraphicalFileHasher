using System;
using System.Collections.Generic;
using System.Text;

namespace GraphicalFileHasher.Exceptions;

public class InvalidPathException(InvalidPathException.Cause motivation) : Exception
{
    public enum Cause
    {
        NO_RECURSIVE_FLAG,
        INVALID_PATH,
        NOT_EXISTS
    };

    public Cause Motivation { get; init; } = motivation;
}
