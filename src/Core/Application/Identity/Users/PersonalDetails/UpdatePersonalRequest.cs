using FL_CRMS_ERP_WEBAPI.Application.LeadData.Account;
using FL_CRMS_ERP_WEBAPI.Domain.Identity;
using FL_CRMS_ERP_WEBAPI.Domain.LeadData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FL_CRMS_ERP_WEBAPI.Application.Identity.Users.PersonalDetails;
public class UpdatePersonalRequest : IRequest<Guid>
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? AliasName { get; set; }
    public string? Role { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
    public string? Phone { get; set; }
    public string? Mobile { get; set; }
    public string? Fax { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Street { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Zipcode { get; set; }
    public string? Country { get; set; }
    public string? Language { get; set; }
    public string? CountryLocale { get; set; }
    public string? TimeZone { get; set; }
    public int IsActive { get; set; }
    //public string CompanyId { get; set; }
    public string? SecurityQuestion { get; set; }
    public string? SecurityAnswer { get; set; }
    public string? Profile { get; set; }
    public string? Designation { get; set; }

    public class UpdatePersonalRequestHandler : IRequestHandler<UpdatePersonalRequest, Guid>
    {
        // Add Domain Events automatically by using IRepositoryWithEvents
        private readonly IRepositoryWithEvents<PersonalDetailsModel> _repository;
        private readonly IStringLocalizer _t;

        public UpdatePersonalRequestHandler(IRepositoryWithEvents<PersonalDetailsModel> repository, IStringLocalizer<UpdatePersonalRequestHandler> localizer) =>
            (_repository, _t) = (repository, localizer);

        public async Task<Guid> Handle(UpdatePersonalRequest request, CancellationToken cancellationToken)
        {
            var personal = await _repository.GetByIdAsync(request.Id, cancellationToken);

            _ = personal
            ?? throw new NotFoundException(_t["User {0} Not Found.", request.Id]);

            personal.Update(request.UserId, request.FirstName, request.LastName, request.AliasName, request.Role, request.Email, request.Website, request.Phone, request.Mobile,
                request.Fax, request.DateOfBirth, request.Street, request.City, request.State, request.Zipcode, request.Country, request.Language, request.CountryLocale, request.TimeZone,
                request.IsActive, request.SecurityQuestion, request.SecurityAnswer, request.Profile, request.Designation);

            await _repository.UpdateAsync(personal, cancellationToken);

            return request.Id;
        }
    }
}
