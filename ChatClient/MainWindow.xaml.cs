using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using FreddiChatClient.ChatServiceReference;

namespace FreddiChatClient {

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    [CallbackBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, UseSynchronizationContext = false)]
    public partial class MainWindow : IChatServiceCallback {

        private ChatServiceClient _chatClient;

        private readonly Dispatcher _dispatcher;

        private readonly DispatcherTimer _keepAliveTimer;

        private readonly List<string> _messageHistory = new List<string> { String.Empty };

        private int _messageHistoryIndex;

        private string _respondToUser;

        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true, CallingConvention = CallingConvention.Winapi)]
        public static extern short GetKeyState(int keyCode);

        public MainWindow() {
            InitializeComponent();

            _dispatcher = Dispatcher.CurrentDispatcher;

            _keepAliveTimer = new DispatcherTimer(TimeSpan.FromSeconds(30), DispatcherPriority.Normal, KeepAlive, _dispatcher);
        }

        #region IChatServiceCallback members

        public void OnConnect(DateTime dateTime, bool result, string message, string[] users) {
            _dispatcher.Invoke(new Action(() => AppendText(dateTime, message, result ? Colors.OliveDrab : Colors.Red)));
            if (!result) {
                _dispatcher.Invoke(new Action(() => EnableConnect(true)));
                return;
            }
            _dispatcher.Invoke(new Action(() => AddUsers(users)));
            _dispatcher.Invoke(new Action(() => AppendText(String.Format("There are currently {0} user(s) connected.", users.Length), Colors.OliveDrab)));
            _dispatcher.Invoke(new Action(() => EnableDisconnect(true)));
            _dispatcher.Invoke(new Action(() => EnableChat(true)));
        }

        public void OnDisconnect(DateTime dateTime, bool result, string message) {
            _dispatcher.Invoke(new Action(() => AppendText(dateTime, message, result ? Colors.Orange : Colors.Red)));
            if (!result) return;
            _dispatcher.Invoke(new Action(() => RemoveUser(userNameTextBox.Text)));
            _dispatcher.Invoke(new Action(() => EnableConnect(true)));
        }

        public void OnUserConnect(DateTime dateTime, string user, string message) {
            _dispatcher.Invoke(new Action(() => AddUser(user)));
            _dispatcher.Invoke(new Action(() => AppendText(dateTime, message, Colors.OliveDrab)));
        }

        public void OnUserDisconnect(DateTime dateTime, string user, string message) {
            _dispatcher.Invoke(new Action(() => RemoveUser(user)));
            _dispatcher.Invoke(new Action(() => AppendText(dateTime, message, Colors.Orange)));
        }

        public void OnBroadcast(DateTime dateTime, bool result, string resultMessage, string sentMessage) {
            if (result) {
                // Ignore the result message (probably unnecessary information)
                // Just output what was sent
                _dispatcher.Invoke(new Action(() => AppendText(dateTime, "You", "say", sentMessage)));
                return;
            }

            // Display the error message
            _dispatcher.Invoke(new Action(() => AppendText(dateTime, resultMessage, Colors.Red)));
        }

        public void OnWhisper(DateTime dateTime, bool result, string resultMessage, string toUser, string sentMessage) {
            if (result) {
                // Ignore the result message (probably unnecessary information)
                // Just output what was sent
                _dispatcher.Invoke(new Action(() => AppendText(dateTime, "You", String.Format("whisper to {0}", toUser), sentMessage, Colors.BlueViolet)));
                return;
            }

            // Display the error message
            _dispatcher.Invoke(new Action(() => AppendText(dateTime, resultMessage, Colors.Red)));
        }

        public void OnUserBroadcast(DateTime dateTime, string fromUser, string message) {
            _dispatcher.Invoke(new Action(() => AppendText(dateTime, fromUser, "says", message)));
        }

        public void OnUserWhisper(DateTime dateTime, string fromUser, string message) {
            // Update the user name to quick respond to through "/r "
            _respondToUser = fromUser;
            _dispatcher.Invoke(new Action(() => AppendText(dateTime, fromUser, "whispers", message, Colors.BlueViolet)));
        }

        public void OnKeepAlive(DateTime dateTime, bool result, string message) {
            // Restart the keep alive timer
            _keepAliveTimer.Start();

            // No need to show the successful keep alive to the user.
            if (result) return;

            // Disable chat.
            _dispatcher.Invoke(new Action(() => EnableChat(false)));

            // Display the error message.
            _dispatcher.Invoke(new Action(() => AppendText(dateTime, message, Colors.Red)));

            // Enable the user to reconnect.
            _dispatcher.Invoke(new Action(() => EnableConnect(true)));
        }

        #endregion

        #region GUI event delegates

        private void WindowLoaded(object sender, RoutedEventArgs e) {
            EnableConnect(true);
        }

        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e) {
            if (disconnectButton.IsEnabled) {
                EnableDisconnect(false);
                EnableChat(false);
                Disconnect();
            }
            _chatClient = null;
        }

        private void ConnectButtonClick(object sender, RoutedEventArgs e) {
            var username = userNameTextBox.Text;
            if (string.IsNullOrWhiteSpace(username) || username.Contains(" ")) {
                AppendText("Please enter a valid username and try to connect again.", Colors.Red);
                EnableConnect(true);
                return;
            }

            var hostname = hostNameTextBox.Text;
            if (string.IsNullOrWhiteSpace(hostname) || hostname.Contains(" ")) {
                AppendText("Please enter a valid hostname or IP.", Colors.Red);
                EnableConnect(true);
                return;
            }

            var selectedItem = protocolComboBox.SelectedItem as ComboBoxItem;

            var protocol = selectedItem == null ? string.Empty : selectedItem.Content.ToString();

            var port = portTextBox.Text;

            EnableConnect(false);
            ThreadPool.QueueUserWorkItem(delegate {
                Connect(username, protocol, hostname, port);
            });
        }

        private void DisconnectButtonClick(object sender, RoutedEventArgs e) {
            EnableDisconnect(false);
            EnableChat(false);
            ThreadPool.QueueUserWorkItem(delegate {
                Disconnect();
            });
        }

        private void SendButtonClick(object sender, RoutedEventArgs e) {
            var message = messageTextBox.Text;
            _messageHistory.Insert(1, message);
            _messageHistoryIndex = 0;
            messageTextBox.Text = String.Empty;
            message = message.Trim();
            if (String.IsNullOrEmpty(message) || String.IsNullOrWhiteSpace(message)) {
                AppendText("Please enter a valid message.", Colors.Red);
                EnableChat(true);
                return;
            }
            var name = userNameTextBox.Text;
            ThreadPool.QueueUserWorkItem(delegate { Send(name, message); });
        }

        private void UserListBoxMouseDoubleClick(object sender, MouseButtonEventArgs e) {
            var listBoxItem = GetElementFromPoint(userListBox, e.GetPosition(userListBox)) as ListBoxItem;
            if (listBoxItem == null) return;

            if (listBoxItem.Content.Equals(userNameTextBox.Text)) {
                AppendText("There is no need to whisper to yourself.", Colors.Red);
                return;
            }

            messageTextBox.Text = String.Empty;
            messageTextBox.Text += String.Format("/w {0} ", listBoxItem.Content);
            messageTextBox.CaretIndex = messageTextBox.Text.Length;
            messageTextBox.Focus();
        }

        private void MessageTextBoxPreviewKeyDown(object sender, KeyEventArgs e) {
            switch (e.Key) {
                case Key.Up:
                    UpdateMessageTextFromHistory(1);
                    break;
                case Key.Down:
                    UpdateMessageTextFromHistory(-1);
                    break;
            }
        }

        private void ExitMenuItemClick(object sender, RoutedEventArgs e) {
            Close();
        }

        private void AboutMenuItemClick(object sender, RoutedEventArgs e) {
            MessageBox.Show("FreddiChat is a chat client by Fredrik Pettersson.",
                "About FreddiChat", MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void MessageTextBoxTextChanged(object sender, TextChangedEventArgs e) {
            var text = messageTextBox.Text.ToLower();
            // Do we have a user to quick respond to? And should we?
            if (_respondToUser != null && (text.Contains("/r ") || text.Contains("/respond "))) {
                var respondToString = String.Format("/w {0} ", _respondToUser);
                messageTextBox.Text = messageTextBox.Text.Replace("/r ", respondToString).Replace("/R ", respondToString).Replace("/respond ", respondToString);
                messageTextBox.CaretIndex = messageTextBox.Text.Length;
            }
        }

        #endregion

        #region Private helpers

        private void RemoveUser(string user) {
            var itemToRemove = userListBox.Items.Cast<ListBoxItem>().FirstOrDefault(item => item.Content.Equals(user));
            if (itemToRemove == null) return;
            userListBox.Items.Remove(itemToRemove);
        }

        /// <summary>
        /// Remove all users from the userListBox.
        /// </summary>
        private void RemoveUsers() {
            var itemsToRemove = userListBox.Items.Cast<ListBoxItem>().ToList();
            foreach (var listBoxItem in itemsToRemove) {
                userListBox.Items.Remove(listBoxItem);
            }
        }

        private void AddUser(string user) {
            userListBox.Items.Add(new ListBoxItem { Content = user });
        }

        private void AddUsers(IEnumerable<string> users) {
            foreach (var user in users.Where(user => !userListBox.Items.Contains(user))) {
                AddUser(user);
            }
        }

        private void EnableConnect(bool enabled) {
            if (enabled) {
                userNameTextBox.Focus();
                userNameTextBox.SelectAll();
                _keepAliveTimer.Stop();
            }
            connectButton.IsDefault = enabled;
            connectButton.IsEnabled = enabled;
            connectMenuItem.IsEnabled = enabled;
            userNameTextBox.IsEnabled = enabled;
            protocolComboBox.IsEnabled = enabled;
            hostNameTextBox.IsEnabled = enabled;
            portTextBox.IsEnabled = enabled;
        }

        private void EnableDisconnect(bool enabled) {
            if (enabled) {
                _keepAliveTimer.Start();
            }
            disconnectButton.IsEnabled = enabled;
            disconnectMenuItem.IsEnabled = enabled;
        }

        private void Connect(string username, string protocol, string hostname, string port) {
            try {
                Binding binding;
                switch (protocol) {
                    case "Named Pipe":
                        protocol = "net.pipe";
                        port = string.Empty;
                        binding = new NetNamedPipeBinding();
                        break;
                    case "HTTP":
                    default:
                        protocol = "http";
                        port = string.IsNullOrEmpty(port) ? string.Empty : string.Format(":{0}", port);
                        var httpBinding = new WSDualHttpBinding();
                        httpBinding.Security.Mode = WSDualHttpSecurityMode.None;
                        binding = httpBinding;
                        break;
                }

                // Create the endpoint address. 
                var serverUrl = string.Format("{0}://{1}{2}/FreddiChat", protocol, hostname, port);
                EndpointAddress endpointAddress = new EndpointAddress(serverUrl);
                _chatClient = new ChatServiceClient(new InstanceContext(this), binding, endpointAddress);

                _chatClient.Open();
                _chatClient.Connect(username);
            } catch (Exception e) {
                _dispatcher.Invoke(new Action(() => AppendText(String.Format("Couldn't establish connection to server. {0} {1}", e.Message, e.InnerException != null ? e.InnerException.Message : ""), Colors.Red)));
                _dispatcher.Invoke(new Action(() => EnableChat(false)));
                _dispatcher.Invoke(new Action(() => EnableConnect(true)));
                _dispatcher.Invoke(new Action(() => EnableDisconnect(false)));
            }
        }

        private void Disconnect() {
            try {
                _chatClient.Disconnect();
                _chatClient.Close();
            } catch (Exception e) {
                _dispatcher.Invoke(new Action(() => AppendText(String.Format("Couldn't establish connection to server. {0} {1}", e.Message, e.InnerException != null ? e.InnerException.Message : ""), Colors.Red)));
                _dispatcher.Invoke(new Action(() => EnableChat(false)));
                _dispatcher.Invoke(new Action(() => EnableConnect(true)));
                _dispatcher.Invoke(new Action(() => EnableDisconnect(false)));
            }
        }

        private void EnableChat(bool enabled) {
            sendButton.IsEnabled = enabled;
            sendButton.IsDefault = enabled;
            messageTextBox.IsEnabled = enabled;
            if (enabled) {
                messageTextBox.Focus();
                messageTextBox.SelectAll();
            } else {
                messageTextBox.Text = String.Empty;
                RemoveUsers();
            }
        }

        private void Send(string name, string message) {
            try {
                // Is this a whisper message?
                if (message.ToLower().StartsWith("/w ") || message.ToLower().StartsWith("/whisper ")) {
                    try {
                        // Split into 3 substrings on ' ' (space).
                        // eg. "/w User This is a message" splits to "/w", "User" and "This is a message".
                        var words = message.Split(new[] { ' ' }, 3);
                        var toUser = words[1];
                        var whisperMessage = words[2];

                        // Are the fields still valid?
                        if (String.IsNullOrEmpty(toUser) || String.IsNullOrEmpty(whisperMessage)) {
                            _dispatcher.Invoke(new Action(() => AppendText("Bad format on whisper command, please use \"/w user message\".", Colors.Red)));
                        }
                            // Is the user whispering himself?
                        else if (toUser.Equals(name)) {
                            _dispatcher.Invoke(new Action(() => AppendText("There is no need to whisper to yourself.", Colors.Red)));
                        }
                            // Send the whisper message.
                        else {
                            try {
                                _chatClient.Whisper(name, toUser, whisperMessage);
                            } catch (Exception e) {
                                _dispatcher.Invoke(new Action(() => AppendText(String.Format("Couldn't establish connection to server. {0} {1}", e.Message, e.InnerException != null ? e.InnerException.Message : ""), Colors.Red)));
                                _dispatcher.Invoke(new Action(() => EnableChat(false)));
                                _dispatcher.Invoke(new Action(() => EnableConnect(true)));
                                _dispatcher.Invoke(new Action(() => EnableDisconnect(false)));
                            }
                        }
                        return;
                    } catch (Exception) {
                        _dispatcher.Invoke(new Action(() => AppendText("Bad format on whisper command, please use \"/w user message\".", Colors.Red)));
                        return;
                    }
                }

                // Broadcast instead
                _chatClient.Broadcast(name, message);
            } catch (Exception e) {
                _dispatcher.Invoke(new Action(() => AppendText(String.Format("Couldn't establish connection to server. {0} {1}", e.Message, e.InnerException != null ? e.InnerException.Message : ""), Colors.Red)));
                _dispatcher.Invoke(new Action(() => EnableChat(false)));
                _dispatcher.Invoke(new Action(() => EnableConnect(true)));
                _dispatcher.Invoke(new Action(() => EnableDisconnect(false)));
            }
        }

        private void AppendText(string text, Color color) {
            AppendText(DateTime.Now, text, color);
        }

        private void AppendText(DateTime dateTime, string text, Color color) {
            AppendText(dateTime, "System", null, text, color);
        }

        private void AppendText(DateTime dateTime, string sender, string senderInfo, string text) {
            AppendText(dateTime, sender, senderInfo, text, Colors.Black);
        }

        private void AppendText(DateTime dateTime, string sender, string senderInfo, string text, Color color) {
            // Create a textrange at the very end of the chat text box, extend the range with the new text.
            var textRange = new TextRange(chatTextBox.Document.ContentEnd, chatTextBox.Document.ContentEnd) { Text = String.Format("[{0}] ", dateTime) };
            // Colorize the last added section.
            textRange.ApplyPropertyValue(TextElement.ForegroundProperty, new SolidColorBrush(color));

            textRange = new TextRange(chatTextBox.Document.ContentEnd, chatTextBox.Document.ContentEnd) { Text = String.Format("{0}", sender) };
            // Colorize the last added section.
            textRange.ApplyPropertyValue(TextElement.ForegroundProperty, new SolidColorBrush(color));
            // Make it bold.
            textRange.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Bold);

            textRange = new TextRange(chatTextBox.Document.ContentEnd, chatTextBox.Document.ContentEnd) { Text = String.IsNullOrEmpty(senderInfo) ? String.Format(": {0}", text) : String.Format(" {0}: {1}", senderInfo, text) };
            // Colorize the last added section.
            textRange.ApplyPropertyValue(TextElement.ForegroundProperty, new SolidColorBrush(color));
            // Make it normal
            textRange.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Normal);

            // Add a new line
            chatTextBox.AppendText(Environment.NewLine);

            // Scroll to end of chat text if ScrollLock isn't on
            var scrollLock = (((ushort)GetKeyState(0x91)) & 0xffff) != 0;
            if (!scrollLock) {
                chatTextBox.ScrollToEnd();
            }
        }

        private static object GetElementFromPoint(ItemsControl box, Point point) {
            var element = (UIElement)box.InputHitTest(point);
            while (true) {
                if (element == box) {
                    return null;
                }
                var item = box.ItemContainerGenerator.ItemFromContainer(element);
                var itemFound = !(item.Equals(DependencyProperty.UnsetValue));
                if (itemFound) {
                    return item;
                }
                element = (UIElement)VisualTreeHelper.GetParent(element);
            }
        }

        private void KeepAlive(object sender, EventArgs e) {
            try {
                _keepAliveTimer.Stop();
                _chatClient.KeepAlive(userNameTextBox.Text);
            } catch (Exception exception) {
                _dispatcher.Invoke(new Action(() => AppendText(String.Format("Couldn't establish connection to server. {0} {1}", exception.Message, exception.InnerException != null ? exception.InnerException.Message : ""), Colors.Red)));
                _dispatcher.Invoke(new Action(() => EnableChat(false)));
                _dispatcher.Invoke(new Action(() => EnableConnect(true)));
                _dispatcher.Invoke(new Action(() => EnableDisconnect(false)));
            }
        }

        private void UpdateMessageTextFromHistory(int direction) {
            _messageHistoryIndex += direction;

            // Wrap around to index 0, the String.Empty entry.
            if (Math.Abs(_messageHistoryIndex) > _messageHistory.Count - 1) _messageHistoryIndex = 0;

            // Set the message to the history message (the message at |index|)
            // Using absolute will make both Up and Down keys work.
            messageTextBox.Text = _messageHistory[Math.Abs(_messageHistoryIndex)];
            messageTextBox.CaretIndex = messageTextBox.Text.Length;
        }

        #endregion

    }

}
