using P2PFileSharing.Common.Models.Messages;

namespace P2PFileSharing.Common.Models.Messages
{
    public class RegisterNackMessage : Message
    {
        public override MessageType Type => MessageType.RegisterNack;

        public string Reason { get; set; } = string.Empty;
    }
}
