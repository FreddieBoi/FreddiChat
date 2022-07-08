using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using FreddieChatServer.Communications;

namespace FreddieChatServer.Managers {

    #region User

    /// <summary>
    /// A service user. Consists of a service callback associated with a specific user name.
    /// </summary>
    public class User {

        /// <summary>
        /// The name of the user.
        /// </summary>
        public string Name {
            get;
            private set;
        }

        /// <summary>
        /// The service callback of the user.
        /// </summary>
        public IChatCallbackContract Callback {
            get;
            private set;
        }

        /// <summary>
        /// Is the user registered?
        /// </summary>
        public bool IsRegistered {
            get;
            private set;
        }

        public DateTime? AliveAt {
            get;
            set;
        }

        /// <summary>
        /// The communication state of the service callback.
        /// </summary>
        public CommunicationState? State {
            get {
                if (!(Callback is ICommunicationObject communication))
                {
                    return null;
                }
                return communication.State;
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="callback">The service callback of the user.</param>
        /// <param name="name">The name of the user.</param>
        /// <param name="registered">Is the user registered?</param>
        public User(IChatCallbackContract callback, string name, bool registered, DateTime? aliveAt) {
            Callback = callback;
            Name = name;
            IsRegistered = registered;
            AliveAt = aliveAt;
        }

        public override string ToString() {
            return Name;
        }

    }

    #endregion

    #region Users

    /// <summary>
    /// A manager for registered users.
    /// </summary>
    public class Users {

        private const int timeoutMinutes = 2;

        private static readonly Dictionary<IChatCallbackContract, User> registeredUsers = new Dictionary<IChatCallbackContract, User>();

        /// <summary>
        /// Get the number of currently registered users.
        /// </summary>
        public int Count {
            get {
                return registeredUsers.Count;
            }
        }

        /// <summary>
        /// Get the user associated with the current callback.
        /// </summary>
        /// <remarks>Invoke from within a service method</remarks>
        public static User Current {
            get {
                var callback = OperationContext.Current.GetCallbackChannel<IChatCallbackContract>();
                return registeredUsers.TryGetValue(callback, out User registeredUser) ? registeredUser : new User(callback, null, false, null);
            }
        }

        /// <summary>
        /// Is the specified user name occupied?
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool IsNameRegistered(string name) {
            return GetUser(name) != null;
        }

        /// <summary>
        /// Register user with the specified name and callback.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public User Register(string name, IChatCallbackContract callback) {
            if (registeredUsers.ContainsKey(callback)) {
                return registeredUsers[callback];
            }
            var user = new User(callback, name, true, DateTime.Now);
            registeredUsers.Add(callback, user);
            return user;
        }

        /// <summary>
        /// Unregister the specified user.
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public User Unregister(User user) {
            if (registeredUsers.ContainsKey(user.Callback)) {
                registeredUsers.Remove(user.Callback);
            }
            return new User(user.Callback, user.Name, false, DateTime.Now);
        }

        public IEnumerable<User> GetUsersToNotify(User user) {
            return registeredUsers.Values.Where(u => u.Callback != user.Callback && u.State.HasValue && u.State.Value == CommunicationState.Opened);
        }

        public string[] GetRegisteredNames() {
            var names = registeredUsers.Values.Select(u => u.Name).ToList();
            names.Sort();
            return names.ToArray();
        }

        public User GetUser(string name) {
            return registeredUsers.Values.FirstOrDefault(u => u.Name.Equals(name));
        }

        public IEnumerable<User> GetUsersToRemove() {
            var now = DateTime.Now;
            return registeredUsers.Values.Where(u =>
                // No connection
                !u.State.HasValue || 
                // Bad connection
                u.State.Value != CommunicationState.Opened ||
                // Timeout
                (u.AliveAt.HasValue && u.AliveAt.Value.AddMinutes(timeoutMinutes) < now));
        }

    }

    #endregion

}
