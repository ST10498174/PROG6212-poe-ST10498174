using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Controllers;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Services;

namespace RaceDay.Web.Areas.Participant.Controllers;

/// <summary>
/// Base class for every Participant page. Only logged-in Participants pass the [RoleAuthorize] filter;
/// Organisers are sent to the access-denied page and visitors to the login page.
/// </summary>
[Area("Participant")]
[RoleAuthorize(Roles.Participant)]
public abstract class ParticipantControllerBase : RaceDayControllerBase
{
    protected ParticipantControllerBase(IRaceDayApiClient api) : base(api)
    {
    }
}
