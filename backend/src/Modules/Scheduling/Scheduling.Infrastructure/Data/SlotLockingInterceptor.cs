using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Scheduling.Infrastructure.Data;

/// <summary>
/// EF Core interceptor that applies UPDLOCK/ROWLOCK hints to slot reservation queries
/// tagged with "SlotReservation_Lock" per DR-009.
/// Usage: context.AppointmentSlots.TagWith("SlotReservation_Lock").Where(...)
/// </summary>
public sealed class SlotLockingInterceptor : DbCommandInterceptor
{
    private const string LockTag = "-- SlotReservation_Lock";
    private const string SlotTable = "[scheduling].[AppointmentSlots]";
    private const string LockedSlotTable = "[scheduling].[AppointmentSlots] WITH (UPDLOCK, ROWLOCK)";

    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        ApplyLockingHints(command);
        return base.ReaderExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        ApplyLockingHints(command);
        return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }

    private static void ApplyLockingHints(DbCommand command)
    {
        if (command.CommandText.Contains(LockTag, StringComparison.Ordinal))
        {
            command.CommandText = command.CommandText.Replace(SlotTable, LockedSlotTable, StringComparison.Ordinal);
        }
    }
}
