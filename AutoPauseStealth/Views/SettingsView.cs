using System;
using System.Collections.Generic;
using System.Reflection;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components.Settings;
using HMUI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AutoPauseStealth.Views
{
    // BSML host for Views/settings.bsml. Forwards values to PluginSettings.Instance
    // (the BSIPA generated config type is not used as BSML host).
    internal class SettingsView
    {
        private const string ManualLabel = "Manual";
        private const int ManualMin = 55;
        private const int ManualMax = 150;
        private const int ManualButtonStep = 5;
        private static readonly string[] ThresholdLabels = { "Strict (97%)", "Normal (95%)", "Loose (90%)" };

        private static PluginSettings Settings => PluginSettings.Instance;

        // UI state (re-loaded from PluginSettings.FpsThresold every time the page is shown)
        private bool m_manual;
        private int m_targetIndex = PluginSettings.DefaultTargetIndex;
        private int m_ratioIndex = PluginSettings.NormalRatioIndex;

        [UIValue("targetOptions")]
        public List<object> TargetOptions { get; } = BuildTargetOptions();

        [UIValue("thresholdOptions")]
        public List<object> ThresholdOptions { get; } = new List<object>(ThresholdLabels);

        [UIValue("targetFps")]
        public object TargetFps
        {
            get => m_manual ? ManualLabel : PluginSettings.TargetFpsOptions[m_targetIndex].ToString();
            set
            {
                string label = value as string;
                if (label == ManualLabel)
                {
                    if (m_manual)
                        return;
                    // Switching to Manual alone does not change the stored value.
                    m_manual = true;
                    m_manualSlider?.ReceiveValue();
                    UpdateRows();
                    return;
                }

                int index = Array.IndexOf(PluginSettings.TargetFpsOptions, ParseInt(label));
                if (index < 0)
                    return;

                if (m_manual)
                {
                    m_manual = false;
                    m_ratioIndex = PluginSettings.NormalRatioIndex;
                    m_thresholdDropdown?.ReceiveValue();
                    UpdateRows();
                }
                m_targetIndex = index;
                ApplyPreset();
            }
        }

        [UIValue("threshold")]
        public object Threshold
        {
            get => ThresholdLabels[m_ratioIndex];
            set
            {
                int index = Array.IndexOf(ThresholdLabels, value as string);
                if (index < 0 || m_manual)
                    return;
                m_ratioIndex = index;
                ApplyPreset();
            }
        }

        [UIValue("manualFps")]
        public int ManualFps
        {
            get
            {
                float current = Settings.GetEffectiveFpsThresold();
                if (current <= 0)
                    current = ManualMin;
                return Mathf.Clamp(Mathf.RoundToInt(current), ManualMin, ManualMax);
            }
            set => Settings.FpsThresold = Mathf.Clamp(value, ManualMin, ManualMax);
        }

        [UIValue("stabilityDurationCheck")]
        public float StabilityDurationCheck
        {
            get => Settings.StabilityDurationCheck;
            set => Settings.StabilityDurationCheck = value;
        }

        [UIValue("maxWaitingTime")]
        public float MaxWaitingTime
        {
            get => Settings.MaxWaitingTime;
            set => Settings.MaxWaitingTime = value;
        }

        [UIValue("reloadOnFailStab")]
        public bool ReloadOnFailStab
        {
            get => Settings.ReloadOnFailStab;
            set => Settings.ReloadOnFailStab = value;
        }

        [UIComponent("fps-stats")]
        private TextMeshProUGUI m_fpsStatsText = null;

        [UIComponent("target-dropdown")]
        private DropDownListSetting m_targetDropdown = null;

        [UIComponent("threshold-dropdown")]
        private DropDownListSetting m_thresholdDropdown = null;

        [UIComponent("manual-fps")]
        private SliderSetting m_manualSlider = null;

        [UIObject("threshold-row")]
        private GameObject m_thresholdRow = null;

        [UIObject("manual-row")]
        private GameObject m_manualRow = null;

        [UIAction("#post-parse")]
        private void PostParse()
        {
            if (m_fpsStatsText != null)
            {
                var display = m_fpsStatsText.GetComponent<FpsStatsDisplay>();
                if (display == null)
                    display = m_fpsStatsText.gameObject.AddComponent<FpsStatsDisplay>();
                display.Shown = OnPageShown;
            }
            RebindManualButtons();
            OnPageShown();
        }

        private bool m_buttonsRebound;

        // The slider's built-in arrows (show-buttons) step one slider step (= 1). Rebind them to +-5.
        // Uses HMUI private fields _decButton/_incButton (present in 1.44.1); if they cannot be found,
        // the arrows keep the default +-1 behaviour.
        private void RebindManualButtons()
        {
            if (m_buttonsRebound || m_manualSlider == null)
                return;
            m_buttonsRebound = true;

            var slider = m_manualSlider.GetComponentInChildren<RangeValuesTextSlider>(true);
            Button dec = GetSliderButton(slider, "_decButton");
            Button inc = GetSliderButton(slider, "_incButton");
            if (dec == null || inc == null)
            {
                Logger.log?.Warn("Couldn't access Min FPS slider buttons, they keep stepping by 1");
                return;
            }

            dec.onClick.RemoveAllListeners();
            dec.onClick.AddListener(() => StepManual(-ManualButtonStep));
            inc.onClick.RemoveAllListeners();
            inc.onClick.AddListener(() => StepManual(ManualButtonStep));
        }

        private static Button GetSliderButton(RangeValuesTextSlider slider, string fieldName)
        {
            if (slider == null)
                return null;
            FieldInfo field = typeof(RangeValuesTextSlider).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            return field?.GetValue(slider) as Button;
        }

        private void StepManual(int delta)
        {
            ManualFps = ManualFps + delta;
            m_manualSlider?.ReceiveValue();
        }

        private void OnPageShown()
        {
            LoadStateFromSettings();
            m_targetDropdown?.ReceiveValue();
            m_thresholdDropdown?.ReceiveValue();
            m_manualSlider?.ReceiveValue();
            UpdateRows();
        }

        private void LoadStateFromSettings()
        {
            float stored = Settings.FpsThresold;
            if (stored > 0)
            {
                // a value matching a preset is shown as that preset, anything else as Manual (kept as is)
                m_manual = !PluginSettings.TryMatchPreset(stored, out m_targetIndex, out m_ratioIndex);
                if (m_manual)
                    m_ratioIndex = PluginSettings.NormalRatioIndex;
            }
            else
            {
                // not set: show the automatic value (nearest target x Normal)
                m_manual = false;
                m_ratioIndex = PluginSettings.NormalRatioIndex;
                m_targetIndex = PluginSettings.TryGetHeadsetRefreshRate(out float hz)
                    ? PluginSettings.NearestTargetIndex(hz)
                    : PluginSettings.DefaultTargetIndex;
            }
        }

        private void ApplyPreset()
        {
            Settings.FpsThresold = PluginSettings.ComputeThreshold(m_targetIndex, m_ratioIndex);
        }

        private void UpdateRows()
        {
            if (m_thresholdRow != null)
                m_thresholdRow.SetActive(!m_manual);
            if (m_manualRow != null)
                m_manualRow.SetActive(m_manual);
        }

        private static List<object> BuildTargetOptions()
        {
            var list = new List<object>();
            foreach (int fps in PluginSettings.TargetFpsOptions)
                list.Add(fps.ToString());
            list.Add(ManualLabel);
            return list;
        }

        private static int ParseInt(string s)
        {
            return int.TryParse(s, out int v) ? v : -1;
        }
    }
}
