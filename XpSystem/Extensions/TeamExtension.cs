using PlayerRoles;

namespace XpSystem.Extensions;

public static class TeamExtension
{
    public static RoundSummary.LeadingTeam GetLeadingTeam(this Team team)
    {
        return team switch
        {
            Team.ClassD or Team.ChaosInsurgency => RoundSummary.LeadingTeam.ChaosInsurgency,
            Team.FoundationForces or Team.Scientists => RoundSummary.LeadingTeam.FacilityForces,
            Team.SCPs => RoundSummary.LeadingTeam.Anomalies,
            _ => RoundSummary.LeadingTeam.Draw
        };
    }
}