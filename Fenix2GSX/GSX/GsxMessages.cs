using CFIT.AppFramework.MessageService;

namespace Fenix2GSX.GSX
{
    public class MessageDataGsx(GsxController controller, object value = null) : AppMessageData(controller, value)
    {
        public virtual GsxController Controller { get { return Sender as GsxController; } }
    }

    public class MessageGsx(MessageDataGsx value) : AppMessage(value)
    {
        public virtual MessageDataGsx Data { get { return Value as MessageDataGsx; } }

        public static TMessage Create<TMessage>(GsxController controller, object value = null) where TMessage : MessageGsx
        {
            return Create<TMessage, MessageDataGsx, GsxController, object>(controller, value);
        }
    }

    public class MsgGsxMenuReady(MessageDataGsx value) : MessageGsx(value) { }

    public class MsgGsxMenuReceived(MessageDataGsx value) : MessageGsx(value) { }

    public class MsgGsxCouatlStarted(MessageDataGsx value) : MessageGsx(value) { }

    public class MsgGsxCouatlStopped(MessageDataGsx value) : MessageGsx(value) { }
}
