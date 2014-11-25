using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using FreddiChatClient.ChatServiceReference;
using System.Windows.Threading;
using System.ServiceModel.Channels;

namespace FreddiChatClient {

    /// <summary>
    /// Manager of all outgoing and incoming connections.
    /// </summary>
    [CallbackBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, UseSynchronizationContext = false)]
    public class Chats : IChatServiceCallback {

        #region Fields

        private static readonly List<string> validHostUriSchemes = new List<string> { Uri.UriSchemeHttp, Uri.UriSchemeNetPipe, Uri.UriSchemeNetTcp };
        private const string serviceName = "FreddiChat";

        private readonly DispatcherTimer keepAliveTimer;

        private ChatServiceClient client;

        #endregion

        #region Events

        public delegate void ConnectionFailedHandler(object sender, DateTime dateTime, string message);
        public delegate void ConnectedEventHandler(object sender, bool result, DateTime dateTime, string message, string[] users);
        public delegate void DisconnectedEventHandler(object sender, bool result, DateTime dateTime, string message);
        public delegate void UserConnectionEventHandler(object sender, DateTime dateTime, string user, string message);
        public delegate void MessageEventHandler(object sender, DateTime dateTime, string message);
        public delegate void UserMessageEventHandler(object sender, DateTime dateTime, string user, string message);

        public event ConnectionFailedHandler ConnectionFailed;
        public event ConnectedEventHandler Connected;
        public event UserConnectionEventHandler UserConnected;
        public event DisconnectedEventHandler Disconnected;
        public event UserConnectionEventHandler UserDisconnected;
        public event MessageEventHandler Broadcasted;
        public event UserMessageEventHandler UserBroadcasted;
        public event UserMessageEventHandler Whispered;
        public event UserMessageEventHandler UserWhispered;

        #endregion

        #region Properties

        public string User {
            get;
            private set;
        }

        public string Host {
            get;
            private set;
        }

        public bool IsConnected {
            get {
                return this.client != null && client.State == CommunicationState.Opened;
            }
        }

        #endregion

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="dispatcher"></param>
        public Chats(Dispatcher dispatcher) {
            keepAliveTimer = new DispatcherTimer(TimeSpan.FromSeconds(30), DispatcherPriority.Normal, KeepAlive, dispatcher);
        }

        #region Chat actions

        public void Connect(string user, string host) {
            User = user;

            if (!host.Contains("://")) {
                host = string.Format("http://{0}", host);
            }
            Host = host;

            // Validate host
            Uri uri;
            UrlExtractor.TryCreate(Host, out uri, validHostUriSchemes);
            if (uri == null) {
                ConnectionFailed(this, DateTime.Now, string.Format("Invalid format for host: {0}", Host));
                return;
            }
            Host = uri.ToString();

            try {
                Binding binding;
                if (uri.Scheme.Equals(Uri.UriSchemeNetPipe)) {
                    var namedPipeBinding = new NetNamedPipeBinding();
                    namedPipeBinding.Security.Mode = NetNamedPipeSecurityMode.None;
                    binding = namedPipeBinding;
                } else if (uri.Scheme.Equals(Uri.UriSchemeNetTcp)) {
                    var tcpBinding = new NetTcpBinding();
                    tcpBinding.Security.Mode = SecurityMode.None;
                    binding = tcpBinding;
                } else {
                    var httpBinding = new WSDualHttpBinding();
                    httpBinding.Security.Mode = WSDualHttpSecurityMode.None;
                    binding = httpBinding;
                }

                // Append service name
                // Note: URI creation appends any missing "/" to the host, so it's safe to just append
                if (!Host.EndsWith(serviceName) && !Host.EndsWith(string.Format("{0}/", serviceName))) {
                    Host = string.Format("{0}{1}", Host, serviceName);
                }

                // Create the endpoint address
                EndpointAddress endpointAddress = new EndpointAddress(Host);
                client = new ChatServiceClient(new InstanceContext(this), binding, endpointAddress);

                client.Open();
                client.Connect(user);
            } catch (Exception exception) {
                TriggerConnectionFailed(exception);
            }
        }

        public void Disconnect() {
            try {
                client.Disconnect();
            } catch (Exception exception) {
                // Ignore any error
                client.Abort();
                TriggerConnectionFailed(exception);
            }
        }

        public void Broadcast(string message) {
            try {
                client.Broadcast(User, message);
            } catch (Exception exception) {
                TriggerConnectionFailed(exception);
            }
        }

        public void Whisper(string user, string message) {
            try {
                client.Whisper(User, user, message);
            } catch (Exception exception) {
                TriggerConnectionFailed(exception);
            }
        }

        #endregion

        #region IChatServiceCallback members

        public void OnConnect(DateTime dateTime, bool result, string message, string[] users) {
            Connected(this, result, dateTime, message, users);
        }

        public void OnDisconnect(DateTime dateTime, bool result, string message) {
            try {
                client.Close();
            } catch {
                // Ignore any error
                client.Abort();
            } finally {
                client = null;
                Disconnected(this, result, dateTime, message);
            }
        }

        public void OnUserConnect(DateTime dateTime, string user, string message) {
            UserConnected(this, dateTime, user, message);
        }

        public void OnUserDisconnect(DateTime dateTime, string user, string message) {
            UserDisconnected(this, dateTime, user, message);
        }

        public void OnBroadcast(DateTime dateTime, bool result, string resultMessage, string sentMessage) {
            if (result) {
                // Ignore the result message (probably unnecessary information)
                Broadcasted(this, dateTime, sentMessage);
                return;
            }

            // Display the error message
            ConnectionFailed(this, dateTime, resultMessage);
        }

        public void OnWhisper(DateTime dateTime, bool result, string resultMessage, string toUser, string sentMessage) {
            if (result) {
                // Ignore the result message (probably unnecessary information)
                Whispered(this, dateTime, toUser, sentMessage);
                return;
            }

            // Display the error message
            ConnectionFailed(this, dateTime, resultMessage);
        }

        public void OnUserBroadcast(DateTime dateTime, string fromUser, string message) {
            UserBroadcasted(this, dateTime, fromUser, message);
        }

        public void OnUserWhisper(DateTime dateTime, string fromUser, string message) {
            UserWhispered(this, dateTime, fromUser, message);
        }

        public void OnKeepAlive(DateTime dateTime, bool result, string message) {
            // No need to show the successful keep alive to the user.
            if (result) {
                return;
            }

            // Display the error message.
            ConnectionFailed(this, dateTime, message);
        }

        #endregion

        #region Helpers

        private void KeepAlive(object sender, EventArgs e) {
            // Restart the keep alive timer
            keepAliveTimer.Start();

            // Only try to keep alive if connected
            if (IsConnected) {
                try {
                    client.KeepAlive(User);
                } catch (Exception exception) {
                    TriggerConnectionFailed(exception);
                }
            }
        }

        private void TriggerConnectionFailed(Exception exception) {
            ConnectionFailed(this, DateTime.Now, string.Format("Couldn't establish connection to server. {0} {1}", exception.Message, exception.InnerException != null ? exception.InnerException.Message : string.Empty));
        }

        #endregion

    }

}
