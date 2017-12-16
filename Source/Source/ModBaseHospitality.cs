using System;
using System.Collections.Generic;
using HugsLib;
using HugsLib.Settings;

namespace Hospitality
{
    internal class ModBaseHospitality : ModBase
    {
        private static List<Action> TickActions = new List<Action>();

        public static Settings settings;

        private static void Inject()
        {
            var injector = new Hospitality_SpecialInjector();
            injector.Inject();
        }

        public override string ModIdentifier { get { return "Hospitality"; } }

        public override void Initialize()
        {
            Inject();
        }

        public override void DefsLoaded()
        {
            settings = new Settings(Settings);
        }

        public override void Tick(int currentTick)
        {
            foreach (var action in TickActions)
            {
                action();
            }
            TickActions.Clear();
        }

        public static void RegisterTickAction(Action action)
        {
            TickActions.Add(action);
        }
    }

    internal class Settings
    {
        public static SettingHandle<int> minGuestWorkSkill;
        public static SettingHandle<int> maxGuestGroupSize;
        public static SettingHandle<bool> disableGuests;
        public static SettingHandle<bool> disableWork;
        public static SettingHandle<bool> disableGifts;
        
        public Settings(ModSettingsPack settings)
        {
            disableGuests = settings.GetHandle("disableGuests", "Disable visitors", "You actually hate visitors and wonder why you even have this mod.", false);
            disableWork = settings.GetHandle("disableWork", "Disable guests helping", "When checked, guests will not perform any work in your colony.", false);
            disableGifts = settings.GetHandle("disableGifts", "Disable guests leaving gifts", "When checked, guests will never leave items behind when satisfied.", false);
            minGuestWorkSkill = settings.GetHandle("minGuestWorkSkill", "Minimum skill for work", "The minimum skill a guest needs to have to perform a task when helping out.", 7, AtLeast(6));
            maxGuestGroupSize = settings.GetHandle("maxGuestGroupSize", "Maximum guest group size", "The maximum size a group of guests can be.", 16, AtLeast(1)); // TODO make 8
        }

        private static SettingHandle.ValueIsValid AtLeast(int amount)
        {
            return delegate(string value) {
                int actual;
                return int.TryParse(value, out actual) && actual >= amount;
            };
        }
    }
}