﻿using Ship;
using Ship.TIEStriker;
using Upgrade;
using System.Collections.Generic;
using System;
using UpgradesList;
using SubPhases;
using ActionsList;
using Abilities;
using Movement;
using GameModes;

namespace UpgradesList
{
    public class AdaptiveAilerons : GenericUpgrade
    {
        public AdaptiveAilerons() : base()
        {
            Types.Add(UpgradeType.Title);
            Name = "Adaptive Ailerons";
            Cost = 0;

            UpgradeAbilities.Add(new AdaptiveAileronsAbility());
        }

        public override bool IsAllowedForShip(GenericShip ship)
        {
            return ship is TIEStriker;
        }
    }
}

namespace Abilities
{
    public class AdaptiveAileronsAbility : GenericAbility
    {
        private GenericMovement SavedManeuver;

        private static readonly List<string> ChangedManeuversCodes = new List<string>() { "1.L.B", "1.F.S", "1.R.B" };
        private Dictionary<string, ManeuverColor> SavedManeuverColors;

        public override void ActivateAbility()
        {
            HostShip.OnManeuverIsReadyToBeRevealed += RegisterAdaptiveAileronsAbility;
        }

        public override void DeactivateAbility()
        {
            HostShip.OnManeuverIsReadyToBeRevealed -= RegisterAdaptiveAileronsAbility;
        }

        private void RegisterAdaptiveAileronsAbility(GenericShip ship)
        {
            RegisterAbilityTrigger(TriggerTypes.OnManeuverIsReadyToBeRevealed, DoAdaptiveAileronsAbility);
        }

        private void DoAdaptiveAileronsAbility(object sender, EventArgs e)
        {
            SavedManeuver = HostShip.AssignedManeuver;

            SavedManeuverColors = new Dictionary<string, ManeuverColor>();
            foreach (var changedManeuver in ChangedManeuversCodes)
            {
                SavedManeuverColors.Add(changedManeuver, HostShip.Maneuvers[changedManeuver]);
                HostShip.Maneuvers[changedManeuver] = ManeuverColor.White;
            }

            Triggers.RegisterTrigger(
                new Trigger()
                {
                    Name = "SLAM Planning",
                    TriggerType = TriggerTypes.OnAbilityDirect,
                    TriggerOwner = Selection.ThisShip.Owner.PlayerNo,
                    EventHandler = SelectAdaptiveAileronsManeuver
                }
            );

            Triggers.ResolveTriggers(TriggerTypes.OnAbilityDirect, ExecuteSelectedManeuver);
        }

        private void SelectAdaptiveAileronsManeuver(object sender, EventArgs e)
        {
            HostShip.Owner.ChangeManeuver(
                (maneuverCode) => {
                    GameMode.CurrentGameMode.AssignManeuver(maneuverCode);
                    HostShip.OnMovementFinish += RestoreManuverColors;
                },
                AdaptiveAileronsFilter
            );
        }

        private void RestoreManuverColors(GenericShip ship)
        {
            foreach (var changedManeuver in ChangedManeuversCodes)
            {
                HostShip.Maneuvers[changedManeuver] = SavedManeuverColors[changedManeuver];
            }
        }

        private void ExecuteSelectedManeuver()
        {
            GameMode.CurrentGameMode.LaunchExtraMovement(FinishAdaptiveAileronsAbility);
        }

        private void FinishAdaptiveAileronsAbility()
        {
            GameMode.CurrentGameMode.AssignManeuver(SavedManeuver.ToString());
            // It calls Triggers.FinishTrigger
        }

        private bool AdaptiveAileronsFilter(string maneuverString)
        {
            GenericMovement movement = ShipMovementScript.MovementFromString(maneuverString, HostShip);
            if (movement.ManeuverSpeed != ManeuverSpeed.Speed1) return false;
            if (movement.Bearing == ManeuverBearing.Straight || movement.Bearing == ManeuverBearing.Bank) return true;

            return false;
        }
    }
}