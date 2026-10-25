using System.Collections.Generic;
using System.Linq;
using PavonisInteractive.TerraInvicta;
using PavonisInteractive.TerraInvicta.Actions;

namespace NotificationPresets
{
    public enum Preset { Early, Mid, Late }

    // Each preset only decides which notifications raise an alert (the popup that pauses the game).
    // News feed, timer and summary settings are left alone, so everything is still logged.
    public static class Presets
    {
        // Never worth a pause: missions that can't fail, e.g. Defend Interests
        static readonly HashSet<string> AlwaysQuiet = new HashSet<string>
        {
            "LogMissionOutcome_Uncontested",
        };

        // Routine once the opening is over
        static readonly HashSet<string> MidQuiet = new HashSet<string>
        {
            "LogCouncilorGainsTrait",
            "LogEnemyMissionFailure",
            "LogOrgPoolOverfull",
            "LogArmyLaunchesTowardEnemyRegion",
            "LogArmyArrivesInRegion",
            "LogArmyTeleportedToLegalRegion",
            "LogArmyCompletesOperation",
            "LogArmyAssignedToFaction",
            "LogArmyBeginsAnnexation",
            "LogArmyAnnexationCancelled",
            "LogNewArmyBuilt",
            "LogNewNavyBuilt",
            "LogSpaceDefensesComplete",
            "LogNationGainsSpaceProgram",
            "LogNationGainsCoreEcoRegion",
            "LogNationGainsCoreMineralRegion",
            "LogNationGainsCoreOilRegion",
            "LogDecolonizeComplete",
            "LogDecontaminateComplete",
            "LogMilitaryFounded",
            "LogNationsGainClaims",
            "LogLegitimizeClaimComplete",
            "LogPolicyDeclined",
            "LogPolicyAdopted",
            "LogScanningPlanet",
            "LogScannedPlanet",
            "LogProbeArrived",
            "LogOurFleetRefueled",
            "LogFleetUndocked",
            "LogHabFounded",
            "LogDecommissionHabComplete",
        };

        // Late game: only losing ground on Earth, and fleets and habs
        static readonly HashSet<string> LateAlerts = new HashSet<string>
        {
            // Control points
            "LogMyControlPointCrackedDown",
            "LogMyControlPointPurged",
            "LogControlPointReduction",
            "LogCPDominated",
            "LogLoyaltySwitch",
            "LogCoup",
            "LogRevolution",
            "LogTechWinnerWarning",
            // Fleets
            "LogAssaultCarrierLaunchesTowardEarth",
            "LogFleetLaunchesTowardMyAsset",
            "LogFleetArrival",
            "LogTrajectoryAborted",
            "LogFleetCrashes",
            "LogShipDestroyedInStrat",
            "LogOurShipChangedSides",
            "LogEnemyShipChangedSidesToUs",
            "LogFleetStoleOurFuel",
            "LogFleetEjectedFromStation",
            "LogFleetAvailableForOperations",
            "LogOurFleetRepaired",
            "LogShipComplete",
            "LogBombardingFleetEntersDangerZone",
            "LogAutoBombardementCancelled",
            "LogOrbitalBombardmentComplete",
            "LogAlienHabDetected",
            // Habs
            "LogHabAcquired",
            "LogHabDefected",
            "LogHabDestroyed",
            "LogHabAssaultFailed",
            "LogMyHabAssaultInitiated",
            "LogOurHabControlled",
            "LogOurHabDestroyed",
            "LogOurCriticalHabModuleDestroyed",
        };

        public static bool Alerts(TINotificationTemplate template, Preset preset)
        {
            string name = template.dataName;
            if (preset == Preset.Late)
                return LateAlerts.Contains(name);
            bool byDefault = template.alertAudience == NotificationAudience.RelevantFactions
                || template.alertAudience == NotificationAudience.PrimaryFactions;
            if (!byDefault || AlwaysQuiet.Contains(name))
                return false;
            return preset == Preset.Early || !MidQuiet.Contains(name);
        }

        public static void Apply(TIFactionState faction, Preset preset)
        {
            var templates = TemplateManager.IterateByClass<TINotificationTemplate>()
                .Where(t => t.allowAnyChanges && t.allowAlertChanges)
                .ToList();
            int on = 0;
            foreach (var template in templates)
            {
                bool alert = Alerts(template, preset);
                if (alert)
                    on++;
                faction.playerControl.StartAction(new SetNotificationOptions(faction, template.dataName, 0,
                    alert ? NotificationOverrideBehavior.Add : NotificationOverrideBehavior.Remove));
            }
            Main.Log($"Applied {preset} preset: {on} of {templates.Count} notifications raise alerts");
        }
    }
}
