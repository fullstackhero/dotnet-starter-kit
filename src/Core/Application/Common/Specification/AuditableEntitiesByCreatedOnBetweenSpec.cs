namespace FL_CRMS_ERP_WEBAPI.Application.Common.Specification;

public class AuditableEntitiesByCreatedOnBetweenSpec<T> : Specification<T>
    where T : AuditableEntity
{
    public AuditableEntitiesByCreatedOnBetweenSpec(DateTime from, DateTime until) =>
        Query.Where(e => e.CreatedOn >= from && e.CreatedOn <= until);
}