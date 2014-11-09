using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;

namespace FreddieChatServer {

    #region User

    /// <summary>
    /// A service user. Consists of a service callback associated with a specific user name.
    /// </summary>
    public class User {

        /// <summary>
        /// The name of the user.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// The service callback of the user.
        /// </summary>
        public IChatCallbackContract Callback { get; private set; }

        /// <summary>
        /// Is the user registered?
        /// </summary>
        public bool IsRegistered { get; private set; }

        /// <summary>
        /// The communication state of the service callback.
        /// </summary>
        public CommunicationState? State {
            get {
                var communication = Callback as ICommunicationObject;
                if (communication == null) {
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
        public User(IChatCallbackContract callback, string name = null, bool registered = false) {
            Callback = callback;
            Name = name;
            IsRegistered = registered;
        }

    }

    #endregion

    #region Users

    /// <summary>
    /// A manager for registered users.
    /// </summary>
    public class Users {

        private static readonly Dictionary<IChatCallbackContract, User> registeredUsers = new Dictionary<IChatCallbackContract, User>();

        /// <summary>
        /// Get the number of currently registered users.
        /// </summary>
        public int Count {
            get { return registeredUsers.Count; }
        }

        /// <summary>
        /// Get the user associated with the current callback.
        /// </summary>
        /// <remarks>Invoke from within a service method</remarks>
        public static User Current {
            get {
                var callback = OperationContext.Current.GetCallbackChannel<IChatCallbackContract>();
                User registeredUser;
                return registeredUsers.TryGetValue(callback, out registeredUser) ? registeredUser : new User(callback);
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
            var user = new User(callback, name, true);
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
            return new User(user.Callback, user.Name);
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
            return registeredUsers.Values.Where(u => !u.State.HasValue || u.State.Value != CommunicationState.Opened);
        }

    #endregion

    }

}
