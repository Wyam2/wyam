namespace Wyam.Hosting.LiveReload.Messages
{
    internal class BasicMessage : ILiveReloadMessage
    {
        public string Command { get; set; }
    }
}