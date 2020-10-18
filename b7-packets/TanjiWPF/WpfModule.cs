using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using Sulakore.Communication;
using Sulakore.Habbo;
using Sulakore.Habbo.Messages;
using Sulakore.Modules;
using Sulakore.Protocol;

using Tangine;

namespace TanjiWPF
{
    [DesignerCategory("")]
    public abstract class WpfModule : ExtensionForm
    {
        private static IEnumerable<MethodInfo> FindMethods(Type type) =>
            type
            .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(method =>
            {
                var @params = method.GetParameters();
                return @params.Length == 1 && @params[0].ParameterType.Equals(typeof(DataInterceptedEventArgs));
            });

        private static List<DataCaptureCallback> CallbackListFactory(ushort id) => new List<DataCaptureCallback>();

        private readonly ConcurrentDictionary<ushort, List<DataCaptureCallback>>
            inDataCaptures = new ConcurrentDictionary<ushort, List<DataCaptureCallback>>(),
            outDataCaptures = new ConcurrentDictionary<ushort, List<DataCaptureCallback>>();

        private readonly ConcurrentDictionary<object, List<DataCaptureCallback>>
            interceptors = new ConcurrentDictionary<object, List<DataCaptureCallback>>();

        protected bool disassembledGame = false;

        protected internal bool loaded = false;
        protected internal bool faulted = false;

        protected virtual void OnUnhandledException(Exception ex)
        {
            System.Windows.Forms.MessageBox.Show(
                ParentForm,
                ex.Message,
                "TanjiWPF - Unhandled exception",
                System.Windows.Forms.MessageBoxButtons.OK,
                System.Windows.Forms.MessageBoxIcon.Error
            );
        }

        public bool IsConnected => Connection?.Remote?.IsConnected ?? false;

        private static bool IsHash(string s)
        {
            if (s.Length != 32) return false;
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (('a' <= c && c <= 'f') ||
                    ('A' <= c && c <= 'F') ||
                    ('0' <= c && c <= '9')) continue;
                return false;
            }
            return true;
        }

        internal WpfModule()
        {
            Debug.WriteLine("[WpfModule] Instantiating");
        }

        public override void ModifyGame(HGame game)
        {
            Debug.WriteLine("[WpfModule] ModifyGame");

            base.ModifyGame(game);

            disassembledGame = true;
        }

        #region - Interceptors -
        private void AddCallback(DataCaptureCallback callback)
        {
            var map = callback.Attribute.IsOutgoing ? outDataCaptures : inDataCaptures;
            var list = map.GetOrAdd(callback.Header, CallbackListFactory);
            lock (list) list.Add(callback);
        }

        private void RemoveCallback(DataCaptureCallback callback)
        {
            var map = callback.Attribute.IsOutgoing ? outDataCaptures : inDataCaptures;
            if (map.TryGetValue(callback.Header, out var callbacks))
                lock (callbacks) callbacks.Remove(callback);
        }

        private bool TryResolveAttribute(DataCaptureAttribute attr, out ushort header)
        {
            header = ushort.MaxValue;

            if (attr.Id.HasValue)
            {
                header = attr.Id.Value;
            }
            else
            {
                if (!disassembledGame)
                    throw new Exception("The game has not yet been disassembled, cannot resolve identifier in DataCaptureAttribute.");

                var identifier = attr.Identifier;

                if (IsHash(identifier))
                {
                    var ids = Game.GetMessageIds(identifier);
                    if (ids == null || ids.Length == 0)
                        return false;
                    header = ids[0];
                }
                else
                {
                    var identifiers = attr.IsOutgoing ? Out : (Identifiers)In;
                    if (!identifiers.TryGetId(identifier, out header))
                    {
                        throw new UnknownIdentifierException($"Unknown identifier name \"{identifier}\" defined in " +
                            $"{(attr.IsOutgoing ? "Out" : "In")}DataCapture attribute for method {attr.Target.GetType().FullName}.{attr.Method.Name}");
                    }
                }
            }

            return header > 0 && header < ushort.MaxValue;
        }

        public bool Attach(object interceptor)
        {
            var list = new List<DataCaptureCallback>();
            var unresolved = new List<Identifier>();

            var interceptorType = interceptor.GetType();
            foreach (var method in FindMethods(interceptorType))
            {
                foreach (var attr in method.GetCustomAttributes<DataCaptureAttribute>(true))
                {
                    if (attr == null) continue;

                    attr.Method = method;
                    attr.Target = interceptor;

                    if (!TryResolveAttribute(attr, out ushort header))
                    {
                        unresolved.Add(new Identifier(attr.IsOutgoing, attr.Identifier));
                        continue;
                    }

                    list.Add(new DataCaptureCallback(header, attr));
                }
            }

            if (unresolved.Count > 0)
                throw new HashResolvingException(unresolved);

            if (list.Count == 0) return false;

            if (!interceptors.TryAdd(interceptor, list)) return false;

            foreach (var callback in list)
                AddCallback(callback);

            return true;
        }

        public bool TryAttach(object interceptor)
        {
            var list = new List<DataCaptureCallback>();

            var type = interceptor.GetType();
            foreach (var method in FindMethods(type))
            {
                foreach (var attr in method.GetCustomAttributes<DataCaptureAttribute>(true))
                {
                    if (attr == null) continue;

                    attr.Method = method;
                    attr.Target = interceptor;

                    if (!TryResolveAttribute(attr, out ushort header))
                        return false;

                    list.Add(new DataCaptureCallback(header, attr));
                }
            }

            if (list.Count == 0) return false;

            if (!interceptors.TryAdd(interceptor, list)) return false;

            foreach (var callback in list)
                AddCallback(callback);

            return true;
        }

        public bool Detach(object interceptor)
        {
            if (!interceptors.TryRemove(interceptor, out List<DataCaptureCallback> callbacks))
                return false;

            foreach (var callback in callbacks)
                RemoveCallback(callback);

            return true;
        }
        #endregion

        #region - Data handling -
        public override void HandleIncoming(DataInterceptedEventArgs e) => HandleData(e);
        public override void HandleOutgoing(DataInterceptedEventArgs e) => HandleData(e);

        protected virtual void HandleData(DataInterceptedEventArgs e)
        {
            var map = e.IsOutgoing ? outDataCaptures : inDataCaptures;
            if (map.TryGetValue(e.Packet.Header, out List<DataCaptureCallback> callbacks))
            {
                lock (callbacks)
                {
                    for (int i = callbacks.Count - 1; i >= 0; i--)
                    {
                        var callback = callbacks[i];
                        if (callback.IsDisposed)
                        {
                            callbacks.RemoveAt(i);
                            continue;
                        }

                        e.Packet.Position = 0;
                        try { callback.Invoke(e); }
                        catch (Exception ex)
                        {
                            if (ex is TargetInvocationException)
                                ex = ex.InnerException;

                            string identifierName = (e.IsOutgoing ? Out : (Identifiers)In).GetName(e.Packet.Header);
                            OnUnhandledException(new DataInterceptorException(
                                "An unhandled exception occurred in data interceptor method "
                                + $"{callback.Attribute.Target.GetType().FullName}.{callback.Attribute.Method.Name} "
                                + $"for {(e.IsOutgoing ? "outgoing" : "incoming")} message "
                                + (identifierName != null ? $"{identifierName} ({e.Packet.Header})" : e.Packet.Header.ToString())
                                + $": {ex.Message}",
                                ex,
                                callback.Attribute.Target,
                                callback.Attribute.Method
                            ));
                        }
                    }
                }
            }

            e.Packet.Position = 0;
        }
        #endregion

        #region - Net -
        public async Task<bool> SendToClientAsync(ushort header, params object[] values)
        {
            if (!IsConnected) return false;
            if (header == 0 || header == ushort.MaxValue)
                return false;
            return await Connection?.SendToClientAsync(header, values) > 0;
        }

        public async Task<bool> SendToClientAsync(HMessage packet)
        {
            if (packet.Header == 0 || packet.Header == ushort.MaxValue)
                return false;
            return await Connection?.SendToClientAsync(packet) > 0;
        }

        public async Task<bool> SendToClientAsync(byte[] data) => await Connection?.SendToClientAsync(data) > 0;

        public async Task<bool> SendToServerAsync(ushort header, params object[] values)
        {
            if (header == 0 || header == ushort.MaxValue)
                return false;
            return await Connection?.SendToServerAsync(header, values) > 0;
        }

        public async Task<bool> SendToServerAsync(HMessage packet)
        {
            if (packet.Header == 0 || packet.Header == ushort.MaxValue)
                return false;
            return await Connection?.SendToServerAsync(packet) > 0;
        }

        public async Task<bool> SendToServerAsync(byte[] data) => await Connection?.SendToServerAsync(data) > 0;
        #endregion
    }
}
