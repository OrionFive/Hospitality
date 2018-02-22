using System;
using System.Collections.Generic;
using HugsLib;
using HugsLib.Settings;
using Verse;

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
        public static SettingHandle<bool> disableLimits;

        public Settings(ModSettingsPack settings)
        {
            disableGuests = settings.GetHandle("disableGuests", "DisableVisitors".Translate(), "DisableVisitorsDesc".Translate(), false);
            disableWork = settings.GetHandle("disableWork", "DisableGuestsHelping".Translate(), "DisableGuestsHelpingDesc".Translate(), false);
            disableGifts = settings.GetHandle("disableGifts", "DisableGifts".Translate(), "DisableGiftsDesc".Translate(), false);
            minGuestWorkSkill = settings.GetHandle("minGuestWorkSkill", "MinGuestWorkSkill".Translate(), "MinGuestWorkSkillDesc".Translate(), 7, WorkSkillLimits());
            maxGuestGroupSize = settings.GetHandle("maxGuestGroupSize", "MaxGuestGroupSize".Translate(), "MaxGuestGroupSizeDesc".Translate(), 16, GroupSizeLimits());
            disableLimits = settings.GetHandle("disableLimits", "DisableLimits".Translate(), "DisableLimitsDesc".Translate(), false);
        }

        private static SettingHandle.ValueIsValid WorkSkillLimits()
        {
            return AtLeast(() => disableLimits == null || disableLimits.Value ? 0 : 6);
        }

        private static SettingHandle.ValueIsValid GroupSizeLimits()
        {
            return AtLeast(() => disableLimits == null || disableLimits.Value ? 1 : 8);
        }

        private static SettingHandle.ValueIsValid AtLeast(Func<int> amount)
        {
            return delegate(string value) {
                int actual;
                return int.TryParse(value, out actual) && actual >= amount();
            };
        }
    }
}