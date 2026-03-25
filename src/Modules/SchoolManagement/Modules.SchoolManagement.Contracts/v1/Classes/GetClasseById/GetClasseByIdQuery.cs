using FSH.Modules.SchoolManagement.Contracts.DTOs;
using Mediator;

namespace FSH.Modules.SchoolManagement.Contracts.v1.Classes.GetClasseById;

public sealed record GetClasseByIdQuery(Guid Id) : IQuery<ClasseDto>;
