namespace SyslogForwarder
{
    internal class ConfigModel
    {
        public ListenerConfig Listener { get; set; }
        public ConverterConfig Converter { get; set; }
        public ForwarderConfig Forwarder { get; set; }
    }

    internal class ListenerConfig
    {
        public int Port { get; set; }
    }

    internal class ConverterConfig
    {
        public string Type { get; set; }
    }

    internal class ForwarderConfig
    {
        public int IterationInterval { get; set; }

        public EventHubConfig EventHub { get; set; }

        public class EventHubConfig
        {
            public string ConnectionString { get; set; }

            public string EventHubName { get; set; }
        }
    }

}