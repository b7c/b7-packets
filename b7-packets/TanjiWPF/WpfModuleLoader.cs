using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Serialization;

using Sulakore.Communication;
using Sulakore.Habbo;

namespace TanjiWPF
{
    [DesignerCategory("")]
    public abstract class WpfModuleLoader<T> : WpfModule
        where T : ExtensionWindow
    {
        public T Window { get; private set; }

        private bool deferredAttach = false;

        protected WpfModuleLoader()
        {
            Debug.WriteLine($"[WpfModuleLoader] Instantiating (window type = {typeof(T).FullName})");
        }

        #region /* Window handling */
        protected override void OnLoad(EventArgs e)
        {
            Debug.WriteLine($"[WpfModuleLoader] OnLoad (faulted = {faulted})");

            try
            {
                var type = typeof(T);
                Window = (T)FormatterServices.GetUninitializedObject(type);
                Window.Module = this;
                type.GetConstructor(Type.EmptyTypes).Invoke(Window, null);
            }
            catch (Exception ex)
            {
                if (ex is TargetInvocationException)
                    ex = ex.InnerException;

                Debug.WriteLine($"[WpfModuleLoader] Error creating window {typeof(T).FullName}: {ex.Message}\r\n{ex.StackTrace}");
                System.Windows.Forms.MessageBox.Show(
                    ParentForm,
                    $"{ex.Message}\r\n{ex.StackTrace}",
                    "TanjiWPF - Error creating window",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Error
                );

                faulted = true;
            }

            Visible = false;
            Opacity = 0;
            ShowInTaskbar = false;

            if (!faulted)
            {
                FormClosing += OnFormClosing;
                Window.Module = this;
                Window.Closed += Window_Closed;
                Window.Show(true);

                if (deferredAttach)
                    Attach();
            }

            loaded = true;

            base.OnLoad(e);
        }

        protected override void OnShown(EventArgs e)
        {
            Debug.WriteLine($"[WpfModuleLoader] OnShown (faulted = {faulted})");

            base.OnShown(e);

            if (faulted)
                Close();
        }

        private void OnFormClosing(object sender, System.Windows.Forms.FormClosingEventArgs e)
        {
            Debug.WriteLine("[WpfModuleLoader] FormClosing");

            Window?.Close();
            Window = null;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Debug.WriteLine("[WpfModuleLoader] Window_Closed");

            Close();
        }

        protected override void Dispose(bool disposing)
        {
            Debug.WriteLine($"[WpfModuleLoader] Dispose (disposing = {disposing})");

            base.Dispose(disposing);

            Window?.Close();
            Window = null;
        }

        protected override void OnGotFocus(EventArgs e)
        {
            Debug.WriteLine("[WpfModuleLoader] OnGotFocus");

            base.OnGotFocus(e);
            Window?.Focus();
        }
#endregion

        public override void ModifyGame(HGame game)
        {
            Debug.WriteLine($"[WpfModuleLoader] ModifyGame (loaded = {loaded})");

            base.ModifyGame(game);

            if (loaded)
            {
                Attach();
            }
            else
            {
                deferredAttach = true;
            }
        }

        private void Attach()
        {
            Debug.WriteLine($"[WpfModuleLoader] AttachWindow (deferred = {deferredAttach})");

            try
            {
                Attach(Window);
                Window.OnAttach();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[WpfModuleLoader] ModifyGame error: {ex.Message}");

                System.Windows.Forms.MessageBox.Show(
                    ParentForm,
                    ex.Message + "\r\n" + ex.StackTrace,
                    "TanjiWPF - Error attaching window",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Error
                );

                faulted = true;
            }
        }

        public override void HandleIncoming(DataInterceptedEventArgs e)
        {
            Window?.HandleIncoming(e);
            base.HandleIncoming(e);
        }

        public override void HandleOutgoing(DataInterceptedEventArgs e)
        {
            Window?.HandleOutgoing(e);
            base.HandleOutgoing(e);
        }
    }
}
