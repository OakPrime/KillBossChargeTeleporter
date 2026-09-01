using BepInEx;
using RoR2;
using System;
using System.Collections.Generic;
using UnityEngine.TextCore.Text;

namespace KillBossChargeTeleporter
{

    //This is an example plugin that can be put in BepInEx/plugins/ExamplePlugin/ExamplePlugin.dll to test out.
    //It's a small plugin that adds a relatively simple item to the game, and gives you that item whenever you press F2.

    //This attribute is required, and lists metadata for your plugin.
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]

    //This is the main declaration of our plugin class. BepInEx searches for all classes inheriting from BaseUnityPlugin to initialize on startup.
    //BaseUnityPlugin itself inherits from MonoBehaviour, so you can use this as a reference for what you can declare and use in your plugin class: https://docs.unity3d.com/ScriptReference/MonoBehaviour.html
    public class KillBossChargeTeleporter : BaseUnityPlugin
    {
        //The Plugin GUID should be a unique ID for this plugin, which is human readable (as it is used in places like the config).
        //If we see this PluginGUID as it is on thunderstore, we will deprecate this mod. Change the PluginAuthor and the PluginName !
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "OakPrime";
        public const string PluginName = "KillBossChargeTeleporter";
        public const string PluginVersion = "1.0.3";

        //The Awake() method is run at the very start when the game is initialized.
        public void Awake()
        {
            Log.Init(Logger);
            try
            {
                On.RoR2.TeleporterInteraction.UpdateMonstersClear += (On.RoR2.TeleporterInteraction.hook_UpdateMonstersClear)((orig, self) =>
                {
                    orig(self);
                    if (!((UnityEngine.Object)self != (UnityEngine.Object)null) || !self.monstersCleared || !(bool)(UnityEngine.Object)self.holdoutZoneController || self.activationState != TeleporterInteraction.ActivationState.Charging || (double)self.chargeFraction <= 0.0199999995529652)
                        return;
                    int displayChargePercent = TeleporterInteraction.instance.holdoutZoneController.displayChargePercent;
                    float runStopwatch = Run.instance.GetRunStopwatch();
                    int num1 = Math.Min(Util.GetItemCountForTeam(self.holdoutZoneController.chargingTeam, RoR2Content.Items.FocusConvergence.itemIndex, true), 3);
                    float num2 = (float)Math.Round((100.0 - (double)displayChargePercent) / 100.0 * ((double)TeleporterInteraction.instance.holdoutZoneController.baseChargeDuration / (1.0 + 0.300000011920929 * (double)num1)), 2);
                    Run.instance.SetRunStopwatch(runStopwatch + (float)Math.Round((double)num2, 2));
                    TeleporterInteraction.instance.holdoutZoneController.FullyChargeHoldoutZone();
                });
                /*On.RoR2.TeleporterInteraction.UpdateMonstersClear += (orig, self) =>
                {
                    orig(self);
                    if (self != null && self.monstersCleared && self.holdoutZoneController && self.activationState == TeleporterInteraction.ActivationState.Charging && self.chargeFraction > 0.02f)
                    {
                        int displayChargePercent = TeleporterInteraction.instance.holdoutZoneController.displayChargePercent;
                        float runStopwatch = Run.instance.GetRunStopwatch();
                        int num = Math.Min(Util.GetItemCountForTeam(self.holdoutZoneController.chargingTeam, RoR2Content.Items.FocusConvergence.itemIndex, true, true), 3);
                        float num2 = (100f - (float)displayChargePercent) / 100f * (TeleporterInteraction.instance.holdoutZoneController.baseChargeDuration / (1f + 0.3f * (float)num));
                        num2 = (float)Math.Round((double)num2, 2);
                        float runStopwatch2 = runStopwatch + (float)Math.Round((double)num2, 2);
                        Run.instance.SetRunStopwatch(runStopwatch2);
                        TeleporterInteraction.instance.holdoutZoneController.FullyChargeHoldoutZone();
                        //charMaster.GiveMoney(Run.instance.GetDifficultyScaledCost((int)((100 - displayChargePercent) * 0.70f)));
                        //Chat.AddMessage("Added " + num2.ToString() + " seconds to the game timer.");
                    }
                };*/
                On.RoR2.CombatSquad.AddMember += (orig, self, charMaster) =>
                {
                    orig(self, charMaster);
                    
                };
            }
            catch (Exception e)
            {
                Logger.LogError(e.Message + " - " + e.StackTrace);
            };
        }
    }
}
