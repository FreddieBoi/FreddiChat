using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Timers;

namespace FreddieChatServer
{

    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    class ChatService : IChatService
    {

        private readonly Dictionary<string, IChatCallbackContract> _users = new Dictionary<string, IChatCallbackContract>();

        private readonly Timer _refreshUserListTimer = new Timer();

        public ChatService()
        {
            _refreshUserListTimer.Elapsed += OnRefreshUserListTimerElapsed;
            // Set the Interval to every 10 seconds.
            _refreshUserListTimer.Interval = 10000;
            _refreshUserListTimer.Enabled = true;
        }

        #region IChatService members

        public void Connect(string user)
        {
            Console.WriteLine("[{0}] Recieved Connect({1})", DateTime.Now, user);

            RefreshUserList();

            var callbackContract = OperationContext.Current.GetCallbackChannel<IChatCallbackContract>();

            string message;

            if (_users.ContainsKey(user))
            {
                message = String.Format("Could not connect as {0}. A user with that name already exists!", user);
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[{0}] Sending  OnConnect(false, {1}, null) to Unknown", DateTime.Now, message);
                Console.ResetColor();
                callbackContract.OnConnect(DateTime.Now, false, message, null);
                return;
            }

            var comObj = ((ICommunicationObject)callbackContract);
            if (comObj.State != CommunicationState.Opened)
            {
                message = String.Format("Could not connect. Connection not properly opened (state was {0}).", comObj.State.ToString().ToLower());
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[{0}] Sending  OnConnect(false, {1}, null) to Unknown", DateTime.Now, message);
                Console.ResetColor();
                callbackContract.OnConnect(DateTime.Now, false, message, null);
                return;
            }

            _users.Add(user, callbackContract);

            message = String.Format("Successfully connected as {0}.", user);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("[{0}] Sending  OnConnect(true, {1}, string[{2}]) to {3}", DateTime.Now, message, _users.Count, user);
            Console.ResetColor();

            callbackContract.OnConnect(DateTime.Now, true, message, _users.Keys.ToArray());

            foreach (var pair in _users.Where(pair => !pair.Value.Equals(callbackContract)))
            {
                message = String.Format("{0} connected.", user);
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.WriteLine("[{0}] Sending  OnUserConnect({1}, {2}) to {3}", DateTime.Now, user, message, pair.Key);
                Console.ResetColor();

                pair.Value.OnUserConnect(DateTime.Now, user, message);
            }
        }

        public void Disconnect()
        {
            Console.WriteLine("[{0}] Recieved Disconnect()", DateTime.Now);

            RefreshUserList();

            var callbackContract = OperationContext.Current.GetCallbackChannel<IChatCallbackContract>();

            string message;

            // Can this user disconnect?
            if (!_users.ContainsValue(callbackContract))
            {
                // Log it
                message = String.Format("Could not disconnect. Probably already disconnected.");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[{0}] Sending  OnDisconnect(false, {1}) to Unknown", DateTime.Now, message);
                Console.ResetColor();

                // Notify client
                callbackContract.OnDisconnect(DateTime.Now, false, message);
                return;
            }

            // Remove the user from the active users list
            string user = null;
            foreach (var pair in _users.Where(pair => pair.Value.Equals(callbackContract)))
            {
                user = pair.Key;
            }
            if (user != null) _users.Remove(user);

            // Log
            message = String.Format("Successfully disconnected.");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("[{0}] Sending  OnDisconnect(true, {1}) to {2}", DateTime.Now, message, user);
            Console.ResetColor();

            // Notify client
            callbackContract.OnDisconnect(DateTime.Now, true, message);

            // Notify all other clients
            foreach (var pair in _users.Where(pair => !pair.Value.Equals(callbackContract)))
            {
                // Log
                message = String.Format("{0} disconnected.", user);
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine("[{0}] Sending  OnUserDisconnect({1}, {2}) to {3}", DateTime.Now, user, message, pair.Key);
                Console.ResetColor();

                // Notify current client
                pair.Value.OnUserDisconnect(DateTime.Now, user, message);
            }
        }

        public void Broadcast(string fromUser, string message)
        {
            Console.WriteLine("[{0}] Recieved Broadcast({1}, {2})", DateTime.Now, fromUser, message);

            RefreshUserList();

            var callback = OperationContext.Current.GetCallbackChannel<IChatCallbackContract>();

            string resultMessage;

            if (!_users.ContainsValue(callback) || !_users.ContainsKey(fromUser))
            {
                // Something is wrong here... Log it
                resultMessage = "Please connect before broadcasting.";
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[{0}] Sending  OnBroadcast(false, {1}, null) to {2}", DateTime.Now, resultMessage, fromUser);
                Console.ResetColor();

                // Notify client
                callback.OnBroadcast(DateTime.Now, false, resultMessage, null);
                return;
            }

            // Trim the message (avoid trailing spaces and new lines)
            message = message.Trim();

            // Log
            resultMessage = "Successfully broadcasted the message!";
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("[{0}] Sending  OnBroadcast(true, {1}, {2}) to {3}", DateTime.Now, resultMessage, message, fromUser);
            Console.ResetColor();

            // Notify sending client
            callback.OnBroadcast(DateTime.Now, true, resultMessage, message);

            // Go through all users' callbacks, but skip the fromUser's callback.
            foreach (var pair in _users.Where(pair => !pair.Value.Equals(callback)))
            {
                // Log
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.WriteLine("[{0}] Sending  OnUserBroadcast({1}, {2}) to {3}", DateTime.Now, fromUser, message, pair.Key);
                Console.ResetColor();

                // Notify the current client
                pair.Value.OnUserBroadcast(DateTime.Now, fromUser, message);
            }
        }

        public void Whisper(string fromUser, string toUser, string message)
        {
            Console.WriteLine("[{0}] Recieved Whisper({1}, {2}, {3})", DateTime.Now, fromUser, toUser, message);

            RefreshUserList();

            var callback = OperationContext.Current.GetCallbackChannel<IChatCallbackContract>();

            string resultMessage;

            // Is the user allowed to whisper?
            if (!_users.ContainsValue(callback) || !_users.ContainsKey(fromUser))
            {
                // Something is wrong here... Log
                resultMessage = "Please connect before whispering.";
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[{0}] Sending  OnWhisper(false, {1}, null) to {2}", DateTime.Now, resultMessage, fromUser);
                Console.ResetColor();

                // Notify client
                callback.OnWhisper(DateTime.Now, false, resultMessage, toUser, null);
                return;
            }

            // Is the toUser available?
            if (!_users.ContainsKey(toUser))
            {
                // Something is wrong here... Log
                resultMessage = String.Format("There is no user named {0}.", toUser);
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[{0}] Sending  OnWhisper(false, {1}, {2}, null) to {3}", DateTime.Now, resultMessage, toUser, fromUser);
                Console.ResetColor();

                // Notify client
                callback.OnWhisper(DateTime.Now, false, resultMessage, toUser, null);
                return;
            }

            // Is the fromUser whispering to self?
            if (toUser.Equals(fromUser))
            {
                // Something is wrong here... Log
                resultMessage = String.Format("There is no need to whisper to yourself.");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[{0}] Sending  OnWhisper(false, {1}, {2}, null) to {3}", DateTime.Now, resultMessage, toUser, fromUser);
                Console.ResetColor();

                // Notify client
                callback.OnWhisper(DateTime.Now, false, resultMessage, toUser, null);
                return;
            }

            // Trim the message (avoid trailing spaces and new lines)
            message = message.Trim();

            // Log
            resultMessage = String.Format("Successfully whispered to {0}.", toUser);
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("[{0}] Sending  OnWhisper(true, {1}, {2}, {3}) to {4}", DateTime.Now, resultMessage, toUser, message, fromUser);
            Console.ResetColor();

            // Notify sending client
            callback.OnWhisper(DateTime.Now, true, resultMessage, toUser, message);

            // Log
            Console.ForegroundColor = ConsoleColor.DarkMagenta;
            Console.WriteLine("[{0}] Sending  OnUserWhisper({1}, {2}) to {3}", DateTime.Now, fromUser, message, toUser);
            Console.ResetColor();

            // Whisper through the toUser callback.
            _users[toUser].OnUserWhisper(DateTime.Now, fromUser, message);
        }

        public void KeepAlive(string user)
        {
            Console.WriteLine("[{0}] Recieved KeepAlive({1})", DateTime.Now, user);

            var callback = OperationContext.Current.GetCallbackChannel<IChatCallbackContract>();

            string message;

            // Is the user allowed to keep the connection alive?
            if (!_users.ContainsValue(callback) || !_users.ContainsKey(user))
            {
                // Something is wrong here... Log
                message = "Please connect before trying to keep connection alive.";
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[{0}] Sending  OnKeepAlive(false, {1}) to {2}", DateTime.Now, message, user);
                Console.ResetColor();

                // Notify client
                callback.OnKeepAlive(DateTime.Now, false, message);
                return;
            }

            // Log
            message = "Successfully kept connection alive!";
            Console.WriteLine("[{0}] Sending  OnKeepAlive(true, {1}) to {2}", DateTime.Now, message, user);

            // Notify client
            callback.OnKeepAlive(DateTime.Now, true, message);
        }

        #endregion

        #region Private helpers

        private void RefreshUserList()
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("[{0}] System   RefreshUserList()", DateTime.Now);
            Console.ResetColor();

            // Get all broken connections
            var usersToRemove = new Dictionary<string, CommunicationState>();
            foreach (var pair in _users)
            {
                var comObj = ((ICommunicationObject)pair.Value);
                if (comObj.State == CommunicationState.Opened) continue;
                usersToRemove.Add(pair.Key, comObj.State);
            }

            foreach (var pair in usersToRemove)
            {
                // Remove the user from the active users list
                var user = pair.Key;
                _users.Remove(user);

                // Log
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("[{0}] System   Removing user {1}, state was {2}.", DateTime.Now, user, pair.Value);
                Console.ResetColor();
            }

            foreach (var pair in _users)
            {
                foreach (var userToRemovePair in usersToRemove)
                {
                    var user = userToRemovePair.Key;
                    var state = userToRemovePair.Value;
                    // Log
                    var message = String.Format("{0} disconnected (connection {1}).", user, state.ToString().ToLower());
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine("[{0}] Sending  OnUserDisconnect({1}, {2}) to {3}", DateTime.Now, user, message, pair.Key);
                    Console.ResetColor();

                    // Notify current client
                    pair.Value.OnUserDisconnect(DateTime.Now, user, message);
                }
            }

        }

        private void OnRefreshUserListTimerElapsed(object sender, ElapsedEventArgs e)
        {
            RefreshUserList();
        }

        #endregion
    }

}
