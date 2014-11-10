﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.18444
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace FreddiChatClient.ChatServiceReference {
    
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ServiceModel.ServiceContractAttribute(ConfigurationName="ChatServiceReference.IChatService", CallbackContract=typeof(FreddiChatClient.ChatServiceReference.IChatServiceCallback), SessionMode=System.ServiceModel.SessionMode.Required)]
    public interface IChatService {
        
        [System.ServiceModel.OperationContractAttribute(IsOneWay=true, Action="http://tempuri.org/IChatService/Connect")]
        void Connect(string user);
        
        [System.ServiceModel.OperationContractAttribute(IsOneWay=true, Action="http://tempuri.org/IChatService/Disconnect")]
        void Disconnect();
        
        [System.ServiceModel.OperationContractAttribute(IsOneWay=true, Action="http://tempuri.org/IChatService/Broadcast")]
        void Broadcast(string fromUser, string message);
        
        [System.ServiceModel.OperationContractAttribute(IsOneWay=true, Action="http://tempuri.org/IChatService/Whisper")]
        void Whisper(string fromUser, string toUser, string message);
        
        [System.ServiceModel.OperationContractAttribute(IsOneWay=true, Action="http://tempuri.org/IChatService/KeepAlive")]
        void KeepAlive(string user);
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public interface IChatServiceCallback {
        
        [System.ServiceModel.OperationContractAttribute(IsOneWay=true, Action="http://tempuri.org/IChatService/OnConnect")]
        void OnConnect(System.DateTime dateTime, bool result, string message, string[] users);
        
        [System.ServiceModel.OperationContractAttribute(IsOneWay=true, Action="http://tempuri.org/IChatService/OnDisconnect")]
        void OnDisconnect(System.DateTime dateTime, bool result, string message);
        
        [System.ServiceModel.OperationContractAttribute(IsOneWay=true, Action="http://tempuri.org/IChatService/OnUserConnect")]
        void OnUserConnect(System.DateTime dateTime, string user, string message);
        
        [System.ServiceModel.OperationContractAttribute(IsOneWay=true, Action="http://tempuri.org/IChatService/OnUserDisconnect")]
        void OnUserDisconnect(System.DateTime dateTime, string user, string message);
        
        [System.ServiceModel.OperationContractAttribute(IsOneWay=true, Action="http://tempuri.org/IChatService/OnBroadcast")]
        void OnBroadcast(System.DateTime dateTime, bool result, string resultMessage, string sentMessage);
        
        [System.ServiceModel.OperationContractAttribute(IsOneWay=true, Action="http://tempuri.org/IChatService/OnWhisper")]
        void OnWhisper(System.DateTime dateTime, bool result, string resultMessage, string toUser, string sentMessage);
        
        [System.ServiceModel.OperationContractAttribute(IsOneWay=true, Action="http://tempuri.org/IChatService/OnUserBroadcast")]
        void OnUserBroadcast(System.DateTime dateTime, string fromUser, string message);
        
        [System.ServiceModel.OperationContractAttribute(IsOneWay=true, Action="http://tempuri.org/IChatService/OnUserWhisper")]
        void OnUserWhisper(System.DateTime dateTime, string fromUser, string message);
        
        [System.ServiceModel.OperationContractAttribute(IsOneWay=true, Action="http://tempuri.org/IChatService/OnKeepAlive")]
        void OnKeepAlive(System.DateTime dateTime, bool result, string message);
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public interface IChatServiceChannel : FreddiChatClient.ChatServiceReference.IChatService, System.ServiceModel.IClientChannel {
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public partial class ChatServiceClient : System.ServiceModel.DuplexClientBase<FreddiChatClient.ChatServiceReference.IChatService>, FreddiChatClient.ChatServiceReference.IChatService {
        
        public ChatServiceClient(System.ServiceModel.InstanceContext callbackInstance) : 
                base(callbackInstance) {
        }
        
        public ChatServiceClient(System.ServiceModel.InstanceContext callbackInstance, string endpointConfigurationName) : 
                base(callbackInstance, endpointConfigurationName) {
        }
        
        public ChatServiceClient(System.ServiceModel.InstanceContext callbackInstance, string endpointConfigurationName, string remoteAddress) : 
                base(callbackInstance, endpointConfigurationName, remoteAddress) {
        }
        
        public ChatServiceClient(System.ServiceModel.InstanceContext callbackInstance, string endpointConfigurationName, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(callbackInstance, endpointConfigurationName, remoteAddress) {
        }
        
        public ChatServiceClient(System.ServiceModel.InstanceContext callbackInstance, System.ServiceModel.Channels.Binding binding, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(callbackInstance, binding, remoteAddress) {
        }
        
        public void Connect(string user) {
            base.Channel.Connect(user);
        }
        
        public void Disconnect() {
            base.Channel.Disconnect();
        }
        
        public void Broadcast(string fromUser, string message) {
            base.Channel.Broadcast(fromUser, message);
        }
        
        public void Whisper(string fromUser, string toUser, string message) {
            base.Channel.Whisper(fromUser, toUser, message);
        }
        
        public void KeepAlive(string user) {
            base.Channel.KeepAlive(user);
        }
    }
}
