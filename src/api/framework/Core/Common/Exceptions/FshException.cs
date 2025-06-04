using System;

namespace FSH.Framework.Core.Common.Exceptions;

public class FshException : Exception
{
    public FshException()
    {
    }

    public FshException(string message)
        : base(message)
    {
    }

    public FshException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
} 