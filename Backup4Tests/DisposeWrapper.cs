using System;

namespace Backup4Tests
{
    public class DisposeWrapper<T> : IDisposable
    {
        public T Object { get; }
        private readonly Action<T> _onDisposal;
        private bool _isDisposed;

        public DisposeWrapper(T value, Action<T> onDisposal)
        {
            Object = value;
            _onDisposal = onDisposal;
            _isDisposed = false;
        }

        private void ReleaseUnmanagedResources()
        {
            _onDisposal(Object);
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        ~DisposeWrapper()
        {
            ReleaseUnmanagedResources();
        }
    }
}