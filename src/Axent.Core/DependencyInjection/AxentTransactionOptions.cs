using System.Transactions;

namespace Axent.Core.DependencyInjection;

/// <summary>
/// Configures transaction behavior for <c>ICommand</c> handlers within the Axent pipeline.
/// Transactions are created using <see cref="System.Transactions.TransactionScope"/>.
/// </summary>
public sealed class AxentTransactionOptions
{
    /// <summary>
    /// When <see langword="true"/>, handlers implementing <c>ICommand</c> are executed within a <see cref="System.Transactions.TransactionScope"/>.
    /// When <see langword="false"/>, commands are executed without a transaction.
    /// Defaults to <see langword="true"/>.
    /// </summary>
    public bool UseTransactions { get; set; } = true;

    /// <summary>
    /// The <see cref="System.Transactions.TransactionOptions"/> applied to the transaction scope.
    /// Defaults to <see cref="IsolationLevel.ReadCommitted"/> with a 180-second timeout.
    /// </summary>
    public TransactionOptions TransactionOptions { get; set; } = new()
    {
        IsolationLevel = IsolationLevel.ReadCommitted,
        Timeout = TimeSpan.FromSeconds(180)
    };

    /// <summary>
    /// Controls how the <see cref="System.Transactions.TransactionScope"/> interacts with ambient transactions.
    /// Use <see cref="TransactionScopeOption.Required"/> to enlist in an existing transaction or create a new one,
    /// or <see cref="TransactionScopeOption.RequiresNew"/> to always create an independent transaction.
    /// Defaults to <see cref="TransactionScopeOption.Required"/>.
    /// </summary>
    public TransactionScopeOption TransactionScopeOption { get; set; } = TransactionScopeOption.Required;

    /// <summary>
    /// Controls whether the ambient transaction is flowed across <see langword="async"/> continuations.
    /// Should be set to <see cref="TransactionScopeAsyncFlowOption.Enabled"/> when using <see langword="async"/>/<see langword="await"/>
    /// to ensure the transaction context is preserved across thread switches.
    /// Defaults to <see cref="TransactionScopeAsyncFlowOption.Enabled"/>.
    /// </summary>
    public TransactionScopeAsyncFlowOption TransactionScopeAsyncFlowOption { get; set; } = TransactionScopeAsyncFlowOption.Enabled;
}
