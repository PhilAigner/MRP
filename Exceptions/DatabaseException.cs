using System;

namespace MRP;

/// <summary>
/// Exception thrown when a database operation fails
/// </summary>
public class DatabaseException : Exception
{
 public string? Operation { get; }
    public string? TableName { get; }

    public DatabaseException(string message) : base(message)
  {
    }

    public DatabaseException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }

    public DatabaseException(string message, string operation, string tableName, Exception innerException)
        : base(message, innerException)
  {
     Operation = operation;
        TableName = tableName;
    }
}
