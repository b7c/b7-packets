using System;

using Sulakore.Communication;

using TanjiWPF;

namespace b7.Packets
{
    partial class MainWindow : ExtensionWindow
    {
        private bool _initialized;

        public new PacketsModule Module => (PacketsModule)base.Module;

        public MainWindow()
        {
            InitializeComponent();

            packetLogger.Window = this;
        }

        public override void OnAttach()
        {
            messagesView.LoadMessages(Module.Game);

            _initialized = true;
        }

        public override void HandleIncoming(DataInterceptedEventArgs e) => HandleData(e);
        public override void HandleOutgoing(DataInterceptedEventArgs e) => HandleData(e);

        private void HandleData(DataInterceptedEventArgs e)
        {
            if (!_initialized) return;

            packetLogger.HandleData(e);
        }

        public void LoadInStructuralizer(VmPacketLog log)
        {
            tabControlMain.SelectedItem = structuralizerTab;
            structuralizer.LoadMessage(log);
        }
    }
}
