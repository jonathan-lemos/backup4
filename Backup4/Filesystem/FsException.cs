using System;
using System.IO;
using Mono.Unix.Native;

namespace Backup4.Filesystem
{
    public class FsException : IOException
    {
        public FsException() :
            this(Stdlib.GetLastError())
        {
        }

        public FsException(string message) :
            this(message, Stdlib.GetLastError())
        {
        }

        public FsException(string message, Errno errnum, Exception innerException = null) :
            base($"{message}: {Stdlib.strerror(errnum)}.", innerException)
        {
        }

        public FsException(string message, string path) :
            this(path, message, Stdlib.GetLastError())
        {
        }

        public FsException(string message, string path, Errno errnum, Exception innerException = null) :
            base($"{message}. {path}: {Stdlib.strerror(errnum)}", innerException)
        {
        }

        public FsException(Errno errnum, Exception innerException = null) :
            base(Stdlib.strerror(errnum), innerException)
        {
        }
    }
}