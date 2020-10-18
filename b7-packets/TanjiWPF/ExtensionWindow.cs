using System;
using System.Threading.Tasks;
using System.Windows;

using Sulakore.Communication;
using Sulakore.Habbo.Messages;
using Sulakore.Protocol;

namespace TanjiWPF
{
    public abstract class ExtensionWindow : Window
    {
        public WpfModule Module { get; internal set; }

        public IHConnection Connection => Module.Connection;
        public bool IsConnected => Module.IsConnected;
        public Incoming In => Module.In;
        public Outgoing Out => Module.Out;

        public ExtensionWindow() { }

        public virtual void OnAttach() { }

        public virtual void HandleIncoming(DataInterceptedEventArgs e) { }
        public virtual void HandleOutgoing(DataInterceptedEventArgs e) { }

        public Task<bool> SendToClientAsync(ushort header, params object[] values) => Module.SendToClientAsync(header, values);
        public Task<bool> SendToClientAsync(HMessage packet) => Module.SendToClientAsync(packet);
        public Task<bool> SendToClientAsync(byte[] data) => Module.SendToClientAsync(data);

        public Task<bool> SendToServerAsync(ushort header, params object[] values) => Module.SendToServerAsync(header, values);
        public Task<bool> SendToServerAsync(HMessage packet) => Module.SendToServerAsync(packet);
        public Task<bool> SendToServerAsync(byte[] data) => Module.SendToServerAsync(data);
    }
}
