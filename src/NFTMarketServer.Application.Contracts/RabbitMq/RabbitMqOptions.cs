namespace NFTMarketServer.RabbitMq;

public class RabbitMqOptions
{
    public string Host { get; set; }
    public ushort Port { get; set; }
    public string VirtualHost { get; set; }
    public string UserName { get; set; }
    public string Password { get; set; }
    public string ClientQueueName { get; set; }
}