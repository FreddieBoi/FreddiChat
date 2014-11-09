using System.ServiceModel;

namespace FreddieChatServer {

    [ServiceContract(SessionMode = SessionMode.Required, CallbackContract = typeof(IChatCallbackContract))]
    public interface IChatService {

        [OperationContract(IsOneWay = true)]
        void Connect(string user);

        [OperationContract(IsOneWay = true)]
        void Disconnect();

        [OperationContract(IsOneWay = true)]
        void Broadcast(string fromUser, string message);

        [OperationContract(IsOneWay = true)]
        void Whisper(string fromUser, string toUser, string message);

        [OperationContract(IsOneWay = true)]
        void KeepAlive(string user);

    }

}
