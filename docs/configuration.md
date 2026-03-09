# ⚙️ Configuration
Customize behavior via AxentOptions.
```csharp
builder.Services.AddAxent(options =>
{
    options.ErrorHandling = new AxentErrorHandlingOptions
    {
        EnableDetailedExceptionResponse = false
    };

    options.Logging = new AxentLoggingOptions
    {
        EnableRequestLogging = true
    };

    options.Transactions = new AxentTransactionOptions
    {
        UseTransactions = true
    };
});
```

| Option                                         | Description                                                                                                                                  | Default               |
|------------------------------------------------|----------------------------------------------------------------------------------------------------------------------------------------------|-----------------------|
| `ErrorHandling`                                | Enables pipeline exception handling. EnableDetailedExceptionResponse includes exception details in responses. If null, exceptions propagate. | `null`                |
| `Logging.EnableRequestLogging`                 | Logs the full request object via `ILogger` before processing. Avoid in production if requests contain sensitive data.                        | `false`               |
| `Transactions.UseTransactions`                 | Wraps `ICommand` handlers in a TransactionScope. Has no effect on `IQuery` handlers.                                                         | `true`                |
| `Transactions.TransactionOptions`              | Isolation level and timeout settings.                                                                                                        | `ReadCommitted`, 180s |
| `Transactions.TransactionScopeOption`          | Interaction with ambient transactions.                                                                                                       | `Required`            |
| `Transactions.TransactionScopeAsyncFlowOption` | Controls async transaction flow.                                                                                                             | `Enabled`             |

