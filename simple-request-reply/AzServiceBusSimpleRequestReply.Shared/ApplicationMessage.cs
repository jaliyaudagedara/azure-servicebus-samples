namespace AzServiceBusSimpleRequestReply.Shared;

public record ApplicationMessage(string Input)
{
    public string? Output { get; set; }
}