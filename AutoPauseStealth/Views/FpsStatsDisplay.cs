using System;
using TMPro;
using UnityEngine;

namespace AutoPauseStealth.Views
{
    // FPS statistics since the settings page was shown. Runs only while the page is active.
    //  - Current : average over the last UpdateInterval (no outlier trimming)
    //  - Avg./Min.: per-frame samples since the page was shown, excluding the WarmupDuration right after
    //               showing and trimming the single highest and single lowest sample
    internal class FpsStatsDisplay : MonoBehaviour
    {
        private const float UpdateInterval = 0.5f;
        private const float WarmupDuration = 0.5f;
        private const string PlaceholderStats = "[FPS] -- / Avg. -- / Min. --";

        // Called every time the settings page is shown (set by SettingsView).
        internal Action Shown;

        private TextMeshProUGUI m_text;
        private string m_headsetText = "";
        private float m_warmupRemaining;

        // current window
        private int m_windowFrames;
        private float m_windowTime;
        private int m_currentFps = -1;

        // session (frame time based; lowest fps == longest frame)
        private int m_count;
        private double m_sumDt;
        private float m_maxDt1, m_maxDt2; // longest, 2nd longest
        private float m_minDt1, m_minDt2; // shortest, 2nd shortest

        private void OnEnable()
        {
            if (m_text == null)
                m_text = GetComponent<TextMeshProUGUI>();

            m_warmupRemaining = WarmupDuration;
            m_windowFrames = 0;
            m_windowTime = 0f;
            m_currentFps = -1;
            m_count = 0;
            m_sumDt = 0.0;
            m_maxDt1 = m_maxDt2 = 0f;
            m_minDt1 = m_minDt2 = float.MaxValue;

            m_headsetText = PluginSettings.TryGetHeadsetRefreshRate(out float hz)
                ? $" (Headset: {Mathf.RoundToInt(hz)} Hz)"
                : " (Headset: unknown)";

            if (m_text != null)
                m_text.text = PlaceholderStats + m_headsetText;

            Shown?.Invoke();
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (dt <= 0f)
                return;

            if (m_warmupRemaining > 0f)
            {
                m_warmupRemaining -= dt;
                return;
            }

            AddSample(dt);

            m_windowFrames++;
            m_windowTime += dt;
            if (m_windowTime >= UpdateInterval)
            {
                m_currentFps = Mathf.RoundToInt(m_windowFrames / m_windowTime);
                m_windowFrames = 0;
                m_windowTime = 0f;
                Refresh();
            }
        }

        private void AddSample(float dt)
        {
            m_count++;
            m_sumDt += dt;

            if (dt > m_maxDt1) { m_maxDt2 = m_maxDt1; m_maxDt1 = dt; }
            else if (dt > m_maxDt2) { m_maxDt2 = dt; }

            if (dt < m_minDt1) { m_minDt2 = m_minDt1; m_minDt1 = dt; }
            else if (dt < m_minDt2) { m_minDt2 = dt; }
        }

        private void Refresh()
        {
            if (m_text == null)
                return;

            string avg = "--";
            string min = "--";
            if (m_count >= 3) // need at least one sample left after trimming highest and lowest
            {
                double trimmedTime = m_sumDt - m_maxDt1 - m_minDt1;
                if (trimmedTime > 0.0)
                    avg = Mathf.RoundToInt((float)((m_count - 2) / trimmedTime)).ToString();
                if (m_maxDt2 > 0f)
                    min = Mathf.RoundToInt(1f / m_maxDt2).ToString();
            }

            m_text.text = $"[FPS] {(m_currentFps >= 0 ? m_currentFps.ToString() : "--")} / Avg. {avg} / Min. {min}{m_headsetText}";
        }
    }
}
