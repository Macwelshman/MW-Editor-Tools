using System;
using System.Reflection;
using Game.Settings;
using Game.Simulation;
using UnityEngine;

namespace MW.EditorTools.Modules.EditorEnvironment
{
    // The public SnowDepth exposes only the current read buffer. Access the
    // installed game's paired buffers narrowly so the next flip retains coverage.
    internal sealed class SnowCoverage
    {
        private static readonly FieldInfo buffersField = typeof(SnowSystem).GetField("m_SnowHeights", BindingFlags.Instance | BindingFlags.NonPublic);
        private SnowSystem system;
        private readonly RenderTexture[] targets = new RenderTexture[2];
        private readonly RenderTexture[] originals = new RenderTexture[2];

        private static RenderTexture[] GetBuffers(SnowSystem snow)
        {
            if (buffersField == null || buffersField.FieldType != typeof(RenderTexture[])) throw new NotSupportedException("SnowSystem buffers changed; rebuild this mod for the installed game.");
            return (RenderTexture[])buffersField.GetValue(snow);
        }

        internal static bool IsReady(SnowSystem snow)
        {
            if (snow == null || !snow.Loaded) return false;
            var buffers = GetBuffers(snow);
            return buffers != null && buffers.Length == 2 &&
                buffers[0] != null && buffers[0].IsCreated() &&
                buffers[1] != null && buffers[1].IsCreated();
        }

        internal void Apply(SnowSystem snow)
        {
            var active = RenderTexture.active;
            try
            {
                if (system == null)
                {
                    system = snow;
                    var buffers = GetBuffers(snow);
                    // Capture both before writing either, including for failure cleanup.
                    for (int i = 0; i < 2; i++)
                    {
                        targets[i] = buffers[i];
                        originals[i] = new RenderTexture(targets[i].descriptor);
                        if (!originals[i].Create()) throw new InvalidOperationException("Could not snapshot editor snow coverage.");
                        Graphics.CopyTexture(targets[i], originals[i]);
                    }
                }
                for (int i = 0; i < 2; i++)
                {
                    RenderTexture.active = targets[i];
                    // Seed uniform snow depth at the game's scale of 8.
                    GL.Clear(false, true, new Color(0.125f, 0f, 0f, 0f));
                }
                Shader.SetGlobalTexture("_SnowMap", snow.SnowDepth);
                var gameplay = SharedSettings.instance == null ? null : SharedSettings.instance.gameplay;
                Shader.SetGlobalVector("colossal_SnowScale", new Vector4(gameplay == null || gameplay.snowVisual ? 8f : 0f, 1f, 1f, 1f));
            }
            finally { RenderTexture.active = active; }
        }

        internal void Release() { Release(true); }
        internal void Discard() { Release(false); }
        private void Release(bool restore)
        {
            if (system == null) return;
            try
            {
                if (!restore) return; // Free snapshots without touching the new world's GPU state.
                var buffers = GetBuffers(system);
                for (int i = 0; i < 2; i++)
                {
                    // Never restore into buffers belonging to a newly loaded world.
                    if (originals[i] != null && originals[i].IsCreated() &&
                        targets[i] != null && targets[i].IsCreated() &&
                        buffers != null && buffers.Length == 2 && buffers[i] == targets[i])
                        Graphics.CopyTexture(originals[i], targets[i]);
                }
                Shader.SetGlobalTexture("_SnowMap", system.SnowDepth);
            }
            finally
            {
                for (int i = 0; i < 2; i++)
                {
                    if (originals[i] != null)
                    {
                        originals[i].Release();
                        UnityEngine.Object.Destroy(originals[i]);
                    }
                    originals[i] = null;
                    targets[i] = null;
                }
                system = null;
            }
        }
    }
}
