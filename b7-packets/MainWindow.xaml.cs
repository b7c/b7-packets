using System;

using Sulakore.Communication;

using TanjiWPF;

namespace b7.Packets
{
    partial class MainWindow : ExtensionWindow
    {
        public new PacketsModule Module => (PacketsModule)base.Module;

        public MainWindow()
        {
            InitializeComponent();

            packetLogger.Window = this;
        }

        public override void OnAttach()
        {
            messagesView.LoadMessages(Module.Game);
        }

        public override void HandleIncoming(DataInterceptedEventArgs e) => packetLogger.HandleData(e);
        public override void HandleOutgoing(DataInterceptedEventArgs e) => packetLogger.HandleData(e);

        public void LoadInStructuralizer(VmPacketLog log)
        {
            tabControlMain.SelectedItem = structuralizerTab;
            structuralizer.LoadMessage(log);
        }
    }
}
