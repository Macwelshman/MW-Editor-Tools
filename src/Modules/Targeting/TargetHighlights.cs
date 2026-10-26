using System.Collections.Generic;
using Game.Common;
using Game.Tools;
using Game.Prefabs;
using Game.Rendering;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace MW.EditorTools.Modules.Targeting
{
    public sealed partial class EditorTargetingTool
    {
        private readonly HashSet<Entity> nativeHighlights = new HashSet<Entity>();
        private void ClearNativeHighlights()
        {
            foreach (var id in nativeHighlights) if (EntityManager.Exists(id) && EntityManager.HasComponent<Highlighted>(id)) {
                EntityManager.RemoveComponent<Highlighted>(id);
                if (!EntityManager.HasComponent<BatchesUpdated>(id)) EntityManager.AddComponent<BatchesUpdated>(id);
            }
            nativeHighlights.Clear();
        }
        private readonly List<Entity> expiredHighlights = new List<Entity>(2);
        private void UpdateNativeHighlights(JobHandle input)
        {
            input.Complete();
            var selected = selection.HasSelected && Stable(selection.Selected) ? selection.Selected : Entity.Null;
            var hovered = selection.HasHover && Stable(selection.Hover) ? selection.Hover : Entity.Null;
            expiredHighlights.Clear();
            foreach (var id in nativeHighlights) if (id != selected && id != hovered) expiredHighlights.Add(id);
            foreach (var id in expiredHighlights) {
                if (EntityManager.Exists(id) && EntityManager.HasComponent<Highlighted>(id)) {
                    EntityManager.RemoveComponent<Highlighted>(id);
                    if (!EntityManager.HasComponent<BatchesUpdated>(id)) EntityManager.AddComponent<BatchesUpdated>(id);
                }
                nativeHighlights.Remove(id);
            }
            AddNativeHighlight(selected);
            if (hovered != selected) AddNativeHighlight(hovered);
        }
        private void AddNativeHighlight(Entity id)
        {
            // Polygons have their own exact boundary. Never claim another tool's highlight.
            if (id == Entity.Null || EntityManager.HasBuffer<Game.Areas.Node>(id) || EntityManager.HasComponent<Highlighted>(id)) return;
            EntityManager.AddComponent<Highlighted>(id); nativeHighlights.Add(id);
            if (!EntityManager.HasComponent<BatchesUpdated>(id)) EntityManager.AddComponent<BatchesUpdated>(id);
        }
        private void DrawHighlights(JobHandle input)
        {
            UpdateNativeHighlights(input);
            if (!selection.HasHover && !selection.HasSelected) return;
            var overlay = World.GetExistingSystemManaged<OverlayRenderSystem>();
            if (overlay == null) return;
            var buffer = overlay.GetBuffer(out var writers);
            JobHandle.CombineDependencies(input, writers).Complete();
            if (selection.HasSelected) DrawTarget(buffer, selection.Selected, new Color(0.1f, 0.9f, 1f, 1f));
            if (selection.HasHover && (!selection.HasSelected || selection.Hover != selection.Selected))
                DrawTarget(buffer, selection.Hover, new Color(1f, 0.8f, 0.15f, 1f), true);
            overlay.AddBufferWriter(default(JobHandle));
        }
        private void DrawTarget(OverlayRenderSystem.Buffer buffer, Entity id, Color color, bool hoverOnly = false)
        {
            if (!Stable(id)) return;
            if (EntityManager.HasComponent<Game.Net.Curve>(id))
            {
                var curve=EntityManager.GetComponentData<Game.Net.Curve>(id).m_Bezier;
                if(math.all(math.isfinite(curve.a))&&math.all(math.isfinite(curve.b))&&math.all(math.isfinite(curve.c))&&math.all(math.isfinite(curve.d)))
                    buffer.DrawCurve(color,curve,.25f);
                return;
            }
            if (EntityManager.HasBuffer<Game.Areas.Node>(id))
            {
                var nodes = EntityManager.GetBuffer<Game.Areas.Node>(id, true);
                if (nodes.Length < 2 || nodes.Length > 4096) return;
                int stride=math.max(1,(nodes.Length+255)/256);
                for (int i = 0; i < nodes.Length; i+=stride)
                {
                    var a = nodes[i].m_Position + new float3(0, .08f, 0);
                    var b = nodes[i + stride < nodes.Length ? i + stride : 0].m_Position + new float3(0, .08f, 0);
                    if(math.all(math.isfinite(a))&&math.all(math.isfinite(b)))buffer.DrawLine(color, new Colossal.Mathematics.Line3.Segment(a, b), .15f, false);
                }
                return;
            }
            // Mesh objects and native gizmos use silhouettes; no circular overlays.
        }
    }
}
