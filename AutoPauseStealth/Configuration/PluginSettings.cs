using System;
using IPA.Config.Stores;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo(GeneratedStore.AssemblyVisibilityTarget)]
namespace AutoPauseStealth
{
    // Serialized by BSIPA. Properties are virtual so that the generated store tracks changes and auto-saves.
    // UI bindings live in Views/SettingsView.cs.
    class PluginSettings
    {
        public static PluginSettings Instance { get; set; }

        // Minimum per-frame FPS required before the song resumes.
        // <= 0 means "not set": derived from the headset refresh rate (see GetEffectiveFpsThresold).
        // The settings UI writes TargetFps x Threshold (or a manual value) here; the file format is unchanged.
        public virtual float FpsThresold { get; set; } = -1;

        public virtual float StabilityDurationCheck { get; set; } = 0.3f;

        public virtual float MaxWaitingTime { get; set; } = 5.0f;

        public virtual bool ReloadOnFailStab { get; set; } = false;

        // ---- presets used by the settings UI ----
        internal static readonly int[] TargetFpsOptions = { 72, 80, 90, 120, 144 };
        internal static readonly float[] ThresholdRatios = { 0.97f, 0.95f, 0.90f }; // Strict, Normal, Loose
        internal const int NormalRatioIndex = 1;
        internal const int DefaultTargetIndex = 2; // 90
        private const float MatchTolerance = 0.01f;

        internal static float ComputeThreshold(int targetIndex, int ratioIndex)
        {
            return (float)Math.Round(TargetFpsOptions[targetIndex] * (double)ThresholdRatios[ratioIndex], 2);
        }

        internal static bool TryMatchPreset(float value, out int targetIndex, out int ratioIndex)
        {
            for (int t = 0; t < TargetFpsOptions.Length; t++)
            {
                for (int r = 0; r < ThresholdRatios.Length; r++)
                {
                    if (Math.Abs(ComputeThreshold(t, r) - value) < MatchTolerance)
                    {
                        targetIndex = t;
                        ratioIndex = r;
                        return true;
                    }
                }
            }
            targetIndex = DefaultTargetIndex;
            ratioIndex = NormalRatioIndex;
            return false;
        }

        internal static int NearestTargetIndex(float fps)
        {
            int best = 0;
            for (int t = 1; t < TargetFpsOptions.Length; t++)
            {
                if (Math.Abs(TargetFpsOptions[t] - fps) < Math.Abs(TargetFpsOptions[best] - fps)) // tie -> lower target
                    best = t;
            }
            return best;
        }

        // ---- headset refresh rate (session cache, only successful reads are cached) ----
        private static float s_headsetRefreshRate = -1;
        private static bool s_loggedUnknown;

        internal static bool TryGetHeadsetRefreshRate(out float hz)
        {
            if (s_headsetRefreshRate <= 0)
            {
                float rate = UnityEngine.XR.XRDevice.refreshRate; // may return 0 when no XR device / not reported (e.g. FPFC)
                if (rate > 0)
                {
                    s_headsetRefreshRate = rate;
                    Logger.log?.Debug("Detected headset framerate: " + rate);
                }
                else if (!s_loggedUnknown)
                {
                    s_loggedUnknown = true;
                    Logger.log?.Info("Couldn't get headset refresh rate (" + rate + ")");
                }
            }
            hz = s_headsetRefreshRate;
            return hz > 0;
        }

        // Value actually used by the stability check. Returns <= 0 when it cannot be determined
        // (not set and headset refresh rate unknown) => no pause.
        internal float GetEffectiveFpsThresold()
        {
            float stored = FpsThresold;
            if (stored > 0)
                return stored;
            return GetAutoFpsThresold();
        }

        // Nearest TargetFps x Normal, or -1 if the headset refresh rate is unknown.
        internal static float GetAutoFpsThresold()
        {
            if (!TryGetHeadsetRefreshRate(out float hz))
                return -1;
            return ComputeThreshold(NearestTargetIndex(hz), NormalRatioIndex);
        }
    }
}
