using System.Data.Common;
using System.Threading;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CoreAlign.Integration.Tests.Infrastructure;

public sealed class DbCommandRoundTripInterceptor : DbCommandInterceptor
{
    private static readonly AsyncLocal<RoundTripCounter?> Current = new();

    public static RoundTripCounter BeginScope()
    {
        var counter = new RoundTripCounter();
        Current.Value = counter;
        return counter;
    }

    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        Current.Value?.IncrementReader();
        return base.ReaderExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        Current.Value?.IncrementReader();
        return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> NonQueryExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result)
    {
        Current.Value?.IncrementNonQuery();
        return base.NonQueryExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Current.Value?.IncrementNonQuery();
        return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override InterceptionResult<object> ScalarExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object> result)
    {
        Current.Value?.IncrementScalar();
        return base.ScalarExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object> result,
        CancellationToken cancellationToken = default)
    {
        Current.Value?.IncrementScalar();
        return base.ScalarExecutingAsync(command, eventData, result, cancellationToken);
    }
}

public sealed class RoundTripCounter : IDisposable
{
    private int _reader;
    private int _nonQuery;
    private int _scalar;

    public int Reader => Volatile.Read(ref _reader);
    public int NonQuery => Volatile.Read(ref _nonQuery);
    public int Scalar => Volatile.Read(ref _scalar);
    public int Total => Reader + NonQuery + Scalar;

    internal void IncrementReader() => Interlocked.Increment(ref _reader);
    internal void IncrementNonQuery() => Interlocked.Increment(ref _nonQuery);
    internal void IncrementScalar() => Interlocked.Increment(ref _scalar);

    public void Dispose()
    {
    }
}
