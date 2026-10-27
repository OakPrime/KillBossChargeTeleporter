using BepInEx;
using RoR2;
using System;
using UnityEngine;
using UnityEngine.Networking;

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
        public const string PluginAuthor = "Phreel";
        public const string PluginName = "KillBossChargeTeleporterFork";
        public const string PluginVersion = "1.1.3";

        //The Awake() method is run at the very start when the game is initialized.
        public void Awake()
        {
            Log.Init(Logger);
            try
            {
                On.RoR2.TeleporterInteraction.UpdateMonstersClear += (On.RoR2.TeleporterInteraction.hook_UpdateMonstersClear)((orig, self) =>
                {
                    orig(self);
                    if (self)
                        TrySkipCharge(self, self.monstersCleared);
                });
                // The DLC4 sequence shrine swaps in its own ChargingState, which never calls UpdateMonstersClear
                On.RoR2.TeleporterInteraction.FixedUpdate += (On.RoR2.TeleporterInteraction.hook_FixedUpdate)((orig, self) =>
                {
                    orig(self);
                    if (self && self.mainStateMachine && self.mainStateMachine.state is EntityStates.TeleporterCatastrophe.DLC4.Teleporter.ChargingState && self.bossGroup)
                        TrySkipCharge(self, !self.bossGroup.enabled);
                });
            }
            catch (Exception e)
            {
                Logger.LogError(e.Message + " - " + e.StackTrace);
            };
        }

        private static void TrySkipCharge(TeleporterInteraction self, bool monstersCleared)
        {
            // UpdateMonstersClear now also runs on clients; everything below is server-only state
            if (!NetworkServer.active || !Run.instance)
                return;
            if (!monstersCleared || !self.holdoutZoneController || self.activationState != TeleporterInteraction.ActivationState.Charging || self.chargeFraction <= 0.02f || self.holdoutZoneController.charge >= 1f)
                return;
            HoldoutZoneController holdoutZone = self.holdoutZoneController;
            int displayChargePercent = holdoutZone.displayChargePercent;
            float runStopwatch = Run.instance.GetRunStopwatch();
            int num1 = Math.Min(Util.GetTotalItemCountFromTeam(new Util.GetTotalItemCountFromTeamArgs(RoR2Content.Items.FocusConvergence.itemIndex)
            {
                requiresAlive = true,
                requiredTeam = holdoutZone.chargingTeam,
                requiresConnected = true
            }), 3);
            float num2 = (float)Math.Round((100.0 - (double)displayChargePercent) / 100.0 * ((double)holdoutZone.baseChargeDuration / (1.0 + 0.300000011920929 * (double)num1)), 2);
            if (self.bonusDirector && self.bonusDirector.moneyWaves != null)
                GrantWouldBeRewards(num2, self.bonusDirector, self);
            Run.instance.SetRunStopwatch(runStopwatch + num2);
            holdoutZone.FullyChargeHoldoutZone();
        }

        private static void GrantWouldBeRewards(float timeRemaining, CombatDirector combatDirector, TeleporterInteraction tpInteraction)
        {
            float monsterCredit = combatDirector.monsterCredit;
            for (int index = 0; index < combatDirector.moneyWaves.Length; ++index)
            {
                monsterCredit += combatDirector.moneyWaves[index].Update(timeRemaining, Run.instance.compensatedDifficultyCoefficient);
                while (combatDirector.moneyWaves[index].timer > combatDirector.moneyWaves[index].interval)
                {
                    monsterCredit += combatDirector.moneyWaves[index].Update(0.0f, Run.instance.compensatedDifficultyCoefficient);
                }
            }
            float b = monsterCredit * combatDirector.expRewardCoefficient;
            if ((double)b > (double)Mathf.Epsilon)
            {
                uint expReward = (uint)Mathf.Max(1f, b * Run.instance.compensatedDifficultyCoefficient);

                float num = (float)(1.0 + ((double)Run.instance.ambientLevel - 1.0) * 0.300000011920929);
                CharacterBody playerBody = null;
                foreach (PlayerCharacterMasterController controller in PlayerCharacterMasterController._instancesReadOnly)
                {
                    if (controller.body != null)
                    {
                        playerBody = controller.body;
                        break;
                    }
                }
                if (playerBody == null)
                {
                    Log.LogError("No player bodies found!");
                    return;
                }
                ExperienceManager.instance.AwardExperience(tpInteraction.teleporterPositionIndicator.defaultPosition, playerBody, (ulong)(uint)((double)expReward * (double)num));

                uint goldReward = (uint)Mathf.Max(1f, (float)((double)b * (double)combatDirector.goldRewardCoefficient * 2.0) * Run.instance.compensatedDifficultyCoefficient);
                if (Run.instance.selectedDifficulty >= DifficultyIndex.Eclipse6)
                    goldReward = (uint)((double)goldReward * 0.800000011920929);
                TeamManager.instance.GiveTeamMoney(tpInteraction.holdoutZoneController.chargingTeam, goldReward, false);
            }

        }
    }
}
