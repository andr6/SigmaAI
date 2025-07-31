namespace Sigma.Core.Domain.Model
{
    public class MessageInfo
    {
        public string ID { get; set; } = "";
        public string Context { get; set; } = "";
        public string HtmlAnswers { get; set; } = "";

        /// <summary>
        /// True when sent, false when received
        /// </summary>
        public bool IsSend { get; set; } = false;
        public DateTime CreateTime { get; set; }

    }
}
