using System;
using System.ServiceModel;
using System.Timers;
using FreddieChatServer.Contracts;
using FreddieChatServer.Managers;
using FreddieChatServer.Utils;

namespace FreddieChatServer {

    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class ChatService : IChatService {

        private readonly Users users = new Users();

        private readonly Timer refreshUserListTimer = new Timer();

        /// <summary>
        /// Constructor.
        /// </summary>
        public ChatService() {
            refreshUserListTimer.Elapsed += OnRefreshUserListTimerElapsed;
            // Set the Interval to every 10 seconds.
            refreshUserListTimer.Interval = 10000;
            refreshUserListTimer.Enabled = true;
        }

        #region IChatService members

        public void Connect(string user) {
            ConsoleUtils.TraceCall("Connect({0})", user);

            RefreshUserList();

            var currentUser = Users.Current;

            if (currentUser.IsRegistered) {
                var error = string.Format("Already connected as {0}!", currentUser.Name ?? "Unknown");
                ConsoleUtils.TraceCallFailure(currentUser, "OnConnect(false, {0}, null)", error);
                currentUser.Callback.OnConnect(DateTime.Now, false, error, null);
                return;
            }

            if (users.IsNameRegistered(user)) {
                var error = String.Format("Could not connect as {0}. A user with that name already exists!", user);
                ConsoleUtils.TraceCallFailure(currentUser, "OnConnect(false, {0}, null)", error);
                currentUser.Callback.OnConnect(DateTime.Now, false, error, null);
                return;
            }

            if (currentUser.State == null || currentUser.State != CommunicationState.Opened) {
                var state = currentUser.State == null ? "not initialized" : currentUser.State.ToString().ToLower();
                var error = String.Format("Could not connect. Connection not properly opened (connection {0}).", state);
                ConsoleUtils.TraceCallFailure(currentUser, "OnConnect(false, {0}, null)", error);
                currentUser.Callback.OnConnect(DateTime.Now, false, error, null);
                return;
            }

            currentUser = users.Register(user, currentUser.Callback);

            if (!currentUser.IsRegistered) {
                const string error = "Registration failed!";
                ConsoleUtils.TraceCallFailure(currentUser, "OnConnect(false, {0}, null)", error);
                currentUser.Callback.OnConnect(DateTime.Now, false, error, null);
                return;
            }

            var message = String.Format("Successfully connected as {0}.", user);
            ConsoleUtils.TraceCallSuccess(currentUser, "OnConnect(true, {0}, string[{1}])", message, users.Count);
            currentUser.Callback.OnConnect(DateTime.Now, true, message, users.GetRegisteredNames());

            foreach (var other in users.GetUsersToNotify(currentUser)) {
                var notification = String.Format("{0} connected.", user);
                ConsoleUtils.TraceNotificationSuccess(currentUser, "OnUserConnect({0}, {1}) to {2}", user, notification, other.Name);

                other.Callback.OnUserConnect(DateTime.Now, user, notification);
            }
        }

        public void Disconnect() {
            ConsoleUtils.TraceCall("Disconnect()");

            RefreshUserList();

            var currentUser = Users.Current;

            // Can this user disconnect?
            if (!currentUser.IsRegistered) {
                // Log it
                var error = String.Format("Could not disconnect. Probably already disconnected.");
                ConsoleUtils.TraceCallFailure(currentUser, "OnDisconnect(false, {0})", error);

                // Notify client
                currentUser.Callback.OnDisconnect(DateTime.Now, false, error);
                return;
            }

            // Remove the user from the active users list
            currentUser = users.Unregister(currentUser);

            // Log
            var message = String.Format("Successfully disconnected.");
            ConsoleUtils.TraceCallWarning(currentUser, "OnDisconnect(true, {0})", message);

            // Notify client
            currentUser.Callback.OnDisconnect(DateTime.Now, true, message);

            // Notify all other clients
            foreach (var otherUser in users.GetUsersToNotify(currentUser)) {
                // Log
                var notification = String.Format("{0} disconnected.", currentUser.Name);
                ConsoleUtils.TraceNotificationWarning(otherUser, "OnUserDisconnect({0}, {1})", currentUser.Name, notification);

                // Notify the other user
                otherUser.Callback.OnUserDisconnect(DateTime.Now, currentUser.Name, notification);
            }
        }

        public void Broadcast(string fromUser, string message) {
            ConsoleUtils.TraceCall("Broadcast({0}, {1})", fromUser, message);

            RefreshUserList();

            var currentUser = Users.Current;

            if (!currentUser.IsRegistered) {
                // Something is wrong here... Log it
                const string errorMessage = "Please connect before broadcasting.";
                ConsoleUtils.TraceCallFailure(currentUser, "OnBroadcast(false, {0}, null)", errorMessage);

                // Notify client
                currentUser.Callback.OnBroadcast(DateTime.Now, false, errorMessage, null);
                return;
            }

            // Trim the message (avoid trailing spaces and new lines)
            message = message.Trim();

            // Log
            const string successMessage = "Successfully broadcasted the message!";
            ConsoleUtils.TraceCallBroadcast(currentUser, "OnBroadcast(true, {0}, {1})", successMessage, message);

            // Notify sending client
            currentUser.Callback.OnBroadcast(DateTime.Now, true, successMessage, message);

            // Go through all users' callbacks, but skip the fromUser's callback.
            foreach (var otherUser in users.GetUsersToNotify(currentUser)) {
                // Log
                ConsoleUtils.TraceNotificationBroadcast(otherUser, "OnUserBroadcast({0}, {1})", fromUser, message);

                // Notify the other user
                otherUser.Callback.OnUserBroadcast(DateTime.Now, fromUser, message);
            }
        }

        public void Whisper(string fromUser, string toUser, string message) {
            ConsoleUtils.TraceCall("Whisper({0}, {1}, {2})", fromUser, toUser, message);

            RefreshUserList();

            var currentUser = Users.Current;

            // Is the user allowed to whisper?
            if (!currentUser.IsRegistered) {
                // Something is wrong here... Log
                const string errorMessage = "Please connect before whispering.";
                ConsoleUtils.TraceCallFailure(currentUser, "OnWhisper(false, {0}, null)", errorMessage);

                // Notify client
                currentUser.Callback.OnWhisper(DateTime.Now, false, errorMessage, toUser, null);
                return;
            }

            // Is the toUser available?
            var otherUser = users.GetUser(toUser);
            if (otherUser == null) {
                // Something is wrong here... Log
                var errorMessage = String.Format("There is no user named {0}.", toUser);
                ConsoleUtils.TraceCallFailure(currentUser, "OnWhisper(false, {0}, {1}, null)", errorMessage, toUser);

                // Notify client
                currentUser.Callback.OnWhisper(DateTime.Now, false, errorMessage, toUser, null);
                return;
            }

            // Is the fromUser whispering to self?
            if (toUser.Equals(fromUser)) {
                // Something is wrong here... Log
                var errorMessage = String.Format("There is no need to whisper to yourself.");
                ConsoleUtils.TraceCallFailure(currentUser, "OnWhisper(false, {0}, {1}, null)", errorMessage, toUser);

                // Notify client
                currentUser.Callback.OnWhisper(DateTime.Now, false, errorMessage, toUser, null);
                return;
            }

            // Trim the message (avoid trailing spaces and new lines)
            message = message.Trim();

            // Log
            var resultMessage = String.Format("Successfully whispered to {0}.", toUser);
            ConsoleUtils.TraceCallWhisper(currentUser, "OnWhisper(true, {0}, {1}, {2})", resultMessage, toUser, message);

            // Notify sending client
            currentUser.Callback.OnWhisper(DateTime.Now, true, resultMessage, toUser, message);

            // Log
            ConsoleUtils.TraceNotificationWhipser(otherUser, "OnUserWhisper({0}, {1})", fromUser, message);

            // Whisper through the toUser callback.
            otherUser.Callback.OnUserWhisper(DateTime.Now, fromUser, message);
        }

        public void KeepAlive(string user) {
            ConsoleUtils.TraceCall("KeepAlive({0})", user);

            var currentUser = Users.Current;

            // Is the user allowed to keep the connection alive?
            if (!currentUser.IsRegistered) {
                // Something is wrong here... Log
                const string error = "Please connect before trying to keep connection alive.";
                ConsoleUtils.TraceCallFailure(currentUser, "OnKeepAlive(false, {0})", error);

                // Notify client
                currentUser.Callback.OnKeepAlive(DateTime.Now, false, error);
                return;
            }

            // Keep alive...
            currentUser.AliveAt = DateTime.Now;

            // Log
            const string message = "Successfully kept connection alive!";
            ConsoleUtils.TraceCallSuccess(currentUser, "OnKeepAlive(true, {0})", message);

            // Notify client
            currentUser.Callback.OnKeepAlive(DateTime.Now, true, message);
        }

        #endregion

        #region Private helpers

        private void RefreshUserList() {
            ConsoleUtils.TraceSystemWork("Refreshing list of users...");

            // Remove all broken connections
            foreach (var user in users.GetUsersToRemove()) {
                // Log
                var reason = !user.State.HasValue || user.State.Value != CommunicationState.Opened ? "connection state" : "timeout";
                var state = user.State == null ? "not initialized" : user.State.ToString().ToLower();
                ConsoleUtils.TraceSystemWarning("Removing user {0} due to {1} (connection {2}).", user, reason, state);

                // Remove the user from the active users list
                users.Unregister(user);

                try {
                    // Try to notify client...
                    user.Callback.OnDisconnect(DateTime.Now, true, string.Format("Disconnected due to {0} (connection {1}).", reason, state));
                } catch {
                    // Ignore any error
                }

                // Notify other users
                foreach (var otherUser in users.GetUsersToNotify(user)) {
                    var message = String.Format("{0} disconnected due to {1} (connection {2}).", user.Name, reason, state);

                    // Log
                    ConsoleUtils.TraceNotificationWarning(otherUser, "OnUserDisconnect({0}, {1})", user.Name, message);

                    // Notify client
                    otherUser.Callback.OnUserDisconnect(DateTime.Now, user.Name, message);
                }
            }

            ConsoleUtils.TraceSystemWork("Refresh completed.");
        }

        private void OnRefreshUserListTimerElapsed(object sender, ElapsedEventArgs e) {
            RefreshUserList();
        }

        #endregion

    }

}
