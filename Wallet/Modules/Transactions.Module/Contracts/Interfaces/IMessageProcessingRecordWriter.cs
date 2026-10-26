using Transactions.Contracts.Entities;

public interface IMessageProcessingRecordWriter
{
    Task RecordAsync(MessageProcessingRecord messageProcessingRecord, CancellationToken cancellationToken);
}



public class MessageProcessingRecordWriter : IMessageProcessingRecordWriter
{
    public Task RecordAsync(MessageProcessingRecord messageProcessingRecord, CancellationToken cancellationToken)
    {
        // throw new NotImplementedException();
      System.Console.WriteLine($"{messageProcessingRecord.MessageId} is wriiten");
      return  Task.CompletedTask;
    }
}