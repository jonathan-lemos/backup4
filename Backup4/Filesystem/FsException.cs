using System;
using System.IO;
using Mono.Unix.Native;

namespace Backup4.Filesystem
{
    public class FsException : IOException
    {
        public FsException(Errno errnum) : this(Stdlib.strerror(errnum))
        {
        }

        public FsException(string message) : base(message)
        {
        }

        public FsException(Errno errnum, Exception innerException) : this(Stdlib.strerror(errnum), innerException)
        {
        }

        public FsException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}