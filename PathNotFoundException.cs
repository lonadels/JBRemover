using System;

namespace JBRemover
{
    internal class PathNotFoundException : Exception
    {
        public PathNotFoundException(string? message) : base(message)
        {
        }
    }
}