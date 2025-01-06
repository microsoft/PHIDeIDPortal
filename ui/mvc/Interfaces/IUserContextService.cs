namespace PhiDeidPortal.Ui.Interfaces
{
    using System.Security.Claims;

    public interface IUserContextService
    {
        ClaimsPrincipal User { get; set; }
        bool HasElevatedRights { get; set; }
        bool ViewFilter { get; set; }
    }
}
