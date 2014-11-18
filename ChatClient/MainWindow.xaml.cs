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
using System.Windows.Navigation;
using System.Windows.Threading;
using FreddiChatClient.ChatServiceReference;
﻿using System.Diagnostics;

namespace FreddiChatClient {

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    [CallbackBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, UseSynchronizationContext = false)]
    public partial class MainWindow : IChatServiceCallback {

        #region Private fields

        private ChatServiceClient chatClient;

        private readonly Dispatcher dispatcher;

        private readonly DispatcherTimer keepAliveTimer;

        private readonly List<string> messageHistory = new List<string> { string.Empty };

        private int messageHistoryIndex;

        private string respondToUser;

        #endregion

        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true, CallingConvention = CallingConvention.Winapi)]
        public static extern short GetKeyState(int keyCode);

        /// <summary>
        /// Constructor.
        /// </summary>
        public MainWindow() {
            InitializeComponent();

            dispatcher = Dispatcher.CurrentDispatcher;
            dispatcher.UnhandledException += DispatcherUnhandledException;

            keepAliveTimer = new DispatcherTimer(TimeSpan.FromSeconds(30), DispatcherPriority.Normal, KeepAlive, dispatcher);
        }

        #region IChatServiceCallback members

        public void OnConnect(DateTime dateTime, bool result, string message, string[] users) {
            dispatcher.Invoke(new Action(() => AppendText(dateTime, message, result ? Colors.OliveDrab : Colors.Red)));
            if (!result) {
                dispatcher.Invoke(new Action(() => EnableConnect(true)));
                return;
            }
            dispatcher.Invoke(new Action(() => AddUsers(users)));
            dispatcher.Invoke(new Action(() => AppendText(string.Format("There are currently {0} user(s) connected.", users.Length), Colors.OliveDrab)));
            dispatcher.Invoke(new Action(() => EnableDisconnect(true)));
            dispatcher.Invoke(new Action(() => EnableChat(true)));
        }

        public void OnDisconnect(DateTime dateTime, bool result, string message) {
            try {
                chatClient.Close();
                chatClient = null;
            } catch (Exception) {
                // Ignore any error
                chatClient.Abort();
            } finally {
                if (!dispatcher.HasShutdownStarted) {
                    dispatcher.Invoke(() => AppendText(dateTime, message, result ? Colors.Orange : Colors.Red));
                    dispatcher.Invoke(() => RemoveUser(userNameTextBox.Text));
                    dispatcher.Invoke(() => EnableConnect(true));
                }
            }
        }

        public void OnUserConnect(DateTime dateTime, string user, string message) {
            dispatcher.Invoke(new Action(() => AddUser(user)));
            dispatcher.Invoke(new Action(() => AppendText(dateTime, message, Colors.OliveDrab)));
        }

        public void OnUserDisconnect(DateTime dateTime, string user, string message) {
            dispatcher.Invoke(new Action(() => RemoveUser(user)));
            dispatcher.Invoke(new Action(() => AppendText(dateTime, message, Colors.Orange)));
        }

        public void OnBroadcast(DateTime dateTime, bool result, string resultMessage, string sentMessage) {
            if (result) {
                // Ignore the result message (probably unnecessary information)
                // Just output what was sent
                dispatcher.Invoke(new Action(() => AppendText(dateTime, "You", "say", sentMessage)));
                return;
            }

            // Display the error message
            dispatcher.Invoke(new Action(() => AppendText(dateTime, resultMessage, Colors.Red)));
        }

        public void OnWhisper(DateTime dateTime, bool result, string resultMessage, string toUser, string sentMessage) {
            if (result) {
                // Ignore the result message (probably unnecessary information)
                // Just output what was sent
                dispatcher.Invoke(new Action(() => AppendText(dateTime, "You", string.Format("whisper to {0}", toUser), sentMessage, Colors.BlueViolet)));
                return;
            }

            // Display the error message
            dispatcher.Invoke(new Action(() => AppendText(dateTime, resultMessage, Colors.Red)));
        }

        public void OnUserBroadcast(DateTime dateTime, string fromUser, string message) {
            dispatcher.Invoke(new Action(() => AppendText(dateTime, fromUser, "says", message)));
        }

        public void OnUserWhisper(DateTime dateTime, string fromUser, string message) {
            // Update the user name to quick respond to through "/r "
            respondToUser = fromUser;
            dispatcher.Invoke(new Action(() => AppendText(dateTime, fromUser, "whispers", message, Colors.BlueViolet)));
        }

        public void OnKeepAlive(DateTime dateTime, bool result, string message) {
            // Restart the keep alive timer
            keepAliveTimer.Start();

            // No need to show the successful keep alive to the user.
            if (result) {
                return;
            }

            // Disable chat.
            dispatcher.Invoke(new Action(() => EnableChat(false)));

            // Display the error message.
            dispatcher.Invoke(new Action(() => AppendText(dateTime, message, Colors.Red)));

            // Enable the user to reconnect.
            dispatcher.Invoke(new Action(() => EnableConnect(true)));
        }

        #endregion

        #region GUI event delegates

        private void DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e) {
            // Do nothing.
        }

        private void WindowLoaded(object sender, RoutedEventArgs e) {
            EnableConnect(true);
        }

        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e) {
            if (disconnectButton.IsEnabled) {
                Disconnect();
                EnableDisconnect(false);
                EnableChat(false);
            }

            // Abort pending work in the queue
            dispatcher.InvokeShutdown();
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
            messageHistory.Insert(1, message);
            messageHistoryIndex = 0;
            messageTextBox.Text = string.Empty;
            message = message.Trim();
            if (string.IsNullOrEmpty(message) || string.IsNullOrWhiteSpace(message)) {
                AppendText("Please enter a valid message.", Colors.Red);
                EnableChat(true);
                return;
            }
            var name = userNameTextBox.Text;
            ThreadPool.QueueUserWorkItem(delegate {
                HandleCommand(name, message);
            });
        }

        private void UserListBoxMouseDoubleClick(object sender, MouseButtonEventArgs e) {
            var listBoxItem = GetElementFromPoint(userListBox, e.GetPosition(userListBox)) as ListBoxItem;
            if (listBoxItem == null) {
                return;
            }

            if (listBoxItem.Content.Equals(userNameTextBox.Text)) {
                AppendText("There is no need to whisper to yourself.", Colors.Red);
                return;
            }

            messageTextBox.Text = string.Empty;
            messageTextBox.Text += string.Format("/w {0} ", listBoxItem.Content);
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
            if (respondToUser != null && (text.Contains("/r ") || text.Contains("/respond "))) {
                var respondToString = string.Format("/w {0} ", respondToUser);
                messageTextBox.Text = messageTextBox.Text.Replace("/r ", respondToString).Replace("/R ", respondToString).Replace("/respond ", respondToString);
                messageTextBox.CaretIndex = messageTextBox.Text.Length;
            }
        }

        private void LinkRequestNavigate(object sender, RequestNavigateEventArgs e) {
            if (e.Uri != null) {
                Process.Start(new ProcessStartInfo(e.Uri.ToString()));
            }
        }

        #endregion

        #region Private helpers

        private void RemoveUser(string user) {
            var itemToRemove = userListBox.Items.Cast<ListBoxItem>().FirstOrDefault(item => item.Content.Equals(user));
            if (itemToRemove == null) {
                return;
            }
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
            userListBox.Items.Add(new ListBoxItem {
                Content = user
            });
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
                keepAliveTimer.Stop();
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
                keepAliveTimer.Start();
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
                chatClient = new ChatServiceClient(new InstanceContext(this), binding, endpointAddress);

                chatClient.Open();
                chatClient.Connect(username);
            } catch (Exception e) {
                dispatcher.Invoke(new Action(() => AppendText(string.Format("Couldn't establish connection to server. {0} {1}", e.Message, e.InnerException != null ? e.InnerException.Message : ""), Colors.Red)));
                dispatcher.Invoke(new Action(() => EnableChat(false)));
                dispatcher.Invoke(new Action(() => EnableConnect(true)));
                dispatcher.Invoke(new Action(() => EnableDisconnect(false)));
            }
        }

        private void Disconnect() {
            try {
                chatClient.Disconnect();
            } catch (Exception) {
                // Ignore any error
                chatClient.Abort();
            } finally {
                dispatcher.Invoke(() => EnableChat(false));
                dispatcher.Invoke(() => EnableConnect(true));
                dispatcher.Invoke(() => EnableDisconnect(false));
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
                messageTextBox.Text = string.Empty;
                RemoveUsers();
            }
        }

        private void HandleCommand(string name, string message) {
            try {
                // Is it a command?
                if (message.StartsWith("/")) {
                    var command = message.Remove(0, 1).ToLower();

                    // Special handling of commands with arguments
                    if (command.StartsWith("w") || command.StartsWith("whipser")) {
                        Send(name, message);
                        return;
                    }

                    // Handle all commands without arguments
                    switch (command) {
                        case "clear":
                            dispatcher.Invoke(ClearText);
                            return;
                        case "disconnect":
                            dispatcher.Invoke(() => EnableDisconnect(false));
                            dispatcher.Invoke(() => EnableChat(false));
                            Disconnect();
                            return;
                        case "?":
                        case "h":
                        case "help":
                            dispatcher.Invoke(() => AppendText("/clear, /disconnect, /h[elp], /q[uit], /w[hisper] user message, /r[eply]", Colors.CadetBlue));
                            return;
                        case "q":
                        case "quit":
                        case "exit":
                            dispatcher.Invoke(Close);
                            return;
                        default:
                            dispatcher.Invoke(() => AppendText("Invalid command.", Colors.Red));
                            goto case "help";
                    }
                }
            } catch {
                dispatcher.Invoke(() => AppendText("Bad format on command.", Colors.Red));
            }

            // Just regular broadcast chat...
            Send(name, message);
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
                        if (string.IsNullOrEmpty(toUser) || string.IsNullOrEmpty(whisperMessage)) {
                            dispatcher.Invoke(() => AppendText("Bad format on whisper command, please use \"/w user message\".", Colors.Red));
                        }
                            // Is the user whispering himself?
                        else if (toUser.Equals(name)) {
                            dispatcher.Invoke(() => AppendText("There is no need to whisper to yourself.", Colors.Red));
                        }
                            // Send the whisper message.
                        else {
                            try {
                                chatClient.Whisper(name, toUser, whisperMessage);
                            } catch (Exception e) {
                                dispatcher.Invoke(() => AppendText(string.Format("Couldn't establish connection to server. {0} {1}", e.Message, e.InnerException != null ? e.InnerException.Message : ""), Colors.Red));
                                dispatcher.Invoke(() => EnableChat(false));
                                dispatcher.Invoke(() => EnableConnect(true));
                                dispatcher.Invoke(() => EnableDisconnect(false));
                            }
                        }
                        return;
                    } catch (Exception) {
                        dispatcher.Invoke(() => AppendText("Bad format on whisper command, please use \"/w user message\".", Colors.Red));
                        return;
                    }
                }

                // Broadcast instead
                chatClient.Broadcast(name, message);
            } catch (Exception e) {
                dispatcher.Invoke(() => AppendText(string.Format("Couldn't establish connection to server. {0} {1}", e.Message, e.InnerException != null ? e.InnerException.Message : ""), Colors.Red));
                dispatcher.Invoke(() => EnableChat(false));
                dispatcher.Invoke(() => EnableConnect(true));
                dispatcher.Invoke(() => EnableDisconnect(false));
            }
        }

        private void ClearText() {
            chatTextBox.Document.Blocks.Clear();
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
            // Create a textrange at the very end of the chat text box, extend the range with the new text
            var timestampTextRange = new TextRange(chatTextBox.Document.ContentEnd, chatTextBox.Document.ContentEnd) {
                Text = string.Format("[{0}] ", dateTime)
            };
            // Colorize the timestamp
            timestampTextRange.ApplyPropertyValue(TextElement.ForegroundProperty, new SolidColorBrush(color));
            timestampTextRange.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Normal);

            var senderTextRange = new TextRange(chatTextBox.Document.ContentEnd, chatTextBox.Document.ContentEnd) {
                Text = string.Format("{0}", sender)
            };
            // Colorize the sender and make it bold
            senderTextRange.ApplyPropertyValue(TextElement.ForegroundProperty, new SolidColorBrush(color));
            senderTextRange.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Bold);

            var senderInfoTextRange = new TextRange(chatTextBox.Document.ContentEnd, chatTextBox.Document.ContentEnd) {
                Text = string.IsNullOrWhiteSpace(senderInfo) ? ": " : string.Format(" {0}: ", senderInfo)
            };
            // Colorize the sender info
            senderInfoTextRange.ApplyPropertyValue(TextElement.ForegroundProperty, new SolidColorBrush(color));
            senderInfoTextRange.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Normal);

            // Try to extract URLs and make them clickable
            foreach (var partialText in UrlExtractor.Extract(text)) {
                if (partialText.Value != null) {
                    var linkTextRange = new TextRange(chatTextBox.Document.ContentEnd, chatTextBox.Document.ContentEnd) {
                        Text = partialText.Key
                    };
                    var link = new Hyperlink(linkTextRange.Start, linkTextRange.End);
                    link.NavigateUri = partialText.Value;
                    link.RequestNavigate += LinkRequestNavigate;
                } else {
                    var regularTextRange = new TextRange(chatTextBox.Document.ContentEnd, chatTextBox.Document.ContentEnd) {
                        Text = partialText.Key
                    };
                    // Colorize the text
                    regularTextRange.ApplyPropertyValue(TextElement.ForegroundProperty, new SolidColorBrush(color));
                    regularTextRange.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Normal);
                }
            }

            // Add a new line and reset styles
            var resetTextRange = new TextRange(chatTextBox.Document.ContentEnd, chatTextBox.Document.ContentEnd) {
                Text = Environment.NewLine
            };
            resetTextRange.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Normal);

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
                keepAliveTimer.Stop();
                chatClient.KeepAlive(userNameTextBox.Text);
            } catch (Exception exception) {
                dispatcher.Invoke(new Action(() => AppendText(string.Format("Couldn't establish connection to server. {0} {1}", exception.Message, exception.InnerException != null ? exception.InnerException.Message : ""), Colors.Red)));
                dispatcher.Invoke(new Action(() => EnableChat(false)));
                dispatcher.Invoke(new Action(() => EnableConnect(true)));
                dispatcher.Invoke(new Action(() => EnableDisconnect(false)));
            }
        }

        private void UpdateMessageTextFromHistory(int direction) {
            messageHistoryIndex += direction;

            // Wrap around to index 0, the string.Empty entry.
            if (Math.Abs(messageHistoryIndex) > messageHistory.Count - 1) {
                messageHistoryIndex = 0;
            }

            // Set the message to the history message (the message at |index|)
            // Using absolute will make both Up and Down keys work.
            messageTextBox.Text = messageHistory[Math.Abs(messageHistoryIndex)];
            messageTextBox.CaretIndex = messageTextBox.Text.Length;
        }

        #endregion

    }

}
