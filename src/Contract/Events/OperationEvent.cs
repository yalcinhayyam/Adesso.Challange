namespace Contract.Events;

public record OperationEvent(string OperationName, string Status, string[] Args);
