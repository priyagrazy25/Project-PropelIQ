using System.Linq.Expressions;

namespace SharedKernel.Authorization;

/// <summary>
/// Extension methods for applying staff data scope filtering to IQueryable collections.
/// These methods work with the IStaffDataScopeService to restrict query results
/// based on the authenticated user's role and claims.
/// </summary>
public static class StaffDataScopeExtensions
{
    /// <summary>
    /// Applies patient-based data scoping to a queryable collection.
    /// Filters results to only include records the current user can access.
    /// </summary>
    /// <typeparam name="T">Entity type with a PatientId property.</typeparam>
    /// <param name="query">The queryable to filter.</param>
    /// <param name="scopeService">The staff data scope service.</param>
    /// <param name="patientIdSelector">Expression to select the PatientId from the entity.</param>
    /// <returns>Filtered queryable based on user's data scope.</returns>
    public static IQueryable<T> ApplyPatientScope<T>(
        this IQueryable<T> query,
        IStaffDataScopeService scopeService,
        Expression<Func<T, Guid>> patientIdSelector)
    {
        // Admin/ComplianceOfficer: no filtering
        if (scopeService.HasUnrestrictedAccess())
        {
            return query;
        }

        var role = scopeService.GetCurrentUserRole();

        // FrontDesk/Staff/Provider: can access all patient data for operations
        // Future: Add facility-based filtering here
        if (string.Equals(role, "Provider", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(role, "FrontDesk", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(role, "Staff", StringComparison.OrdinalIgnoreCase))
        {
            return query;
        }

        // Patient: filter to own data only
        var currentPatientId = scopeService.GetCurrentPatientId();
        if (!currentPatientId.HasValue)
        {
            // No patient ID claim - return empty
            return query.Where(_ => false);
        }

        // Build expression: entity => patientIdSelector(entity) == currentPatientId
        var parameter = patientIdSelector.Parameters[0];
        var patientIdAccess = patientIdSelector.Body;
        var constant = Expression.Constant(currentPatientId.Value);
        var equality = Expression.Equal(patientIdAccess, constant);
        var lambda = Expression.Lambda<Func<T, bool>>(equality, parameter);

        return query.Where(lambda);
    }

    /// <summary>
    /// Applies provider-based data scoping to a queryable collection.
    /// Providers can only see their own data; staff can see all.
    /// </summary>
    /// <typeparam name="T">Entity type with a ProviderId property.</typeparam>
    /// <param name="query">The queryable to filter.</param>
    /// <param name="scopeService">The staff data scope service.</param>
    /// <param name="providerIdSelector">Expression to select the ProviderId from the entity.</param>
    /// <returns>Filtered queryable based on user's data scope.</returns>
    public static IQueryable<T> ApplyProviderScope<T>(
        this IQueryable<T> query,
        IStaffDataScopeService scopeService,
        Expression<Func<T, Guid>> providerIdSelector)
    {
        // Admin/ComplianceOfficer: no filtering
        if (scopeService.HasUnrestrictedAccess())
        {
            return query;
        }

        var role = scopeService.GetCurrentUserRole();

        // FrontDesk/Staff: can access all provider data for scheduling
        if (string.Equals(role, "FrontDesk", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(role, "Staff", StringComparison.OrdinalIgnoreCase))
        {
            return query;
        }

        // Provider: filter to own data only
        if (string.Equals(role, "Provider", StringComparison.OrdinalIgnoreCase))
        {
            var currentProviderId = scopeService.GetCurrentProviderId();
            if (!currentProviderId.HasValue)
            {
                // Provider without ProviderId claim - return empty
                return query.Where(_ => false);
            }

            var parameter = providerIdSelector.Parameters[0];
            var providerIdAccess = providerIdSelector.Body;
            var constant = Expression.Constant(currentProviderId.Value);
            var equality = Expression.Equal(providerIdAccess, constant);
            var lambda = Expression.Lambda<Func<T, bool>>(equality, parameter);

            return query.Where(lambda);
        }

        // Patient: can see provider availability (no filtering on provider queries)
        // This allows patients to browse available slots across providers
        return query;
    }

    /// <summary>
    /// Applies combined patient and provider scoping for appointment-related queries.
    /// </summary>
    /// <typeparam name="T">Entity type with both PatientId and ProviderId properties.</typeparam>
    public static IQueryable<T> ApplyAppointmentScope<T>(
        this IQueryable<T> query,
        IStaffDataScopeService scopeService,
        Expression<Func<T, Guid>> patientIdSelector,
        Expression<Func<T, Guid>> providerIdSelector)
    {
        // Admin/ComplianceOfficer: no filtering
        if (scopeService.HasUnrestrictedAccess())
        {
            return query;
        }

        var role = scopeService.GetCurrentUserRole();

        // FrontDesk/Staff: can access all appointments
        if (string.Equals(role, "FrontDesk", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(role, "Staff", StringComparison.OrdinalIgnoreCase))
        {
            return query;
        }

        // Provider: filter to own appointments only
        if (string.Equals(role, "Provider", StringComparison.OrdinalIgnoreCase))
        {
            return query.ApplyProviderScope(scopeService, providerIdSelector);
        }

        // Patient: filter to own appointments only
        return query.ApplyPatientScope(scopeService, patientIdSelector);
    }
}
