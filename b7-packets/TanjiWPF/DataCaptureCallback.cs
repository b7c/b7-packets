using System;

using Sulakore.Communication;
using Sulakore.Modules;

namespace TanjiWPF
{
    internal class DataCaptureCallback : IDisposable
    {
        public ushort Header { get; }
        public bool IsOutgoing => Attribute.IsOutgoing;
        public DataCaptureAttribute Attribute { get; }

        public  DataCaptureCallback(ushort header, DataCaptureAttribute attr)
        {
            Header = header;
            Attribute = attr;
        }

        public void Invoke(DataInterceptedEventArgs e) => Attribute.Invoke(e);

        public bool IsDisposed { get; private set; }
        public void Dispose() => Dispose(true);
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
                IsDisposed = true;
        }
    }
}
