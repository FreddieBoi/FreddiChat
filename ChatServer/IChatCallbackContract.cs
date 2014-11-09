using System;
using System.ServiceModel;

namespace FreddieChatServer {

    public interface IChatCallbackContract {

        [OperationContract(IsOneWay = true)]
        void OnConnect(DateTime dateTime, bool result, string message, string[] users);

        [OperationContract(IsOneWay = true)]
        void OnDisconnect(DateTime dateTime, bool result, string message);

        [OperationContract(IsOneWay = true)]
        void OnUserConnect(DateTime dateTime, string user, string message);

        [OperationContract(IsOneWay = true)]
        void OnUserDisconnect(DateTime dateTime, string user, string message);

        [OperationContract(IsOneWay = true)]
        void OnBroadcast(DateTime dateTime, bool result, string resultMessage, string sentMessage);

        [OperationContract(IsOneWay = true)]
        void OnWhisper(DateTime dateTime, bool result, string resultMessage, string toUser, string sentMessage);

        [OperationContract(IsOneWay = true)]
        void OnUserBroadcast(DateTime dateTime, string fromUser, string message);

        [OperationContract(IsOneWay = true)]
        void OnUserWhisper(DateTime dateTime, string fromUser, string message);

        [OperationContract(IsOneWay = true)]
        void OnKeepAlive(DateTime dateTime, bool result, string message);

    }

}
