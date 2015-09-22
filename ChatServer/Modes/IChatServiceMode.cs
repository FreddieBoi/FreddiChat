using System.ServiceModel.Channels;
using System.ServiceModel.Description;

namespace FreddieChatServer.Modes {

    /// <summary>
    /// Interface for service modes.
    /// </summary>
    public interface IChatServiceMode {

        string Protocol {
            get;
        }

        Binding ServiceEndpointBinding {
            get;
        }

        Binding ServiceMetadataBinding {
            get;
        }

        bool IsPortRequired {
            get;
        }

        IServiceBehavior ServiceBehavior {
            get;
        }

    }

}
