using Game.Prefabs;
using Unity.Entities;

namespace MW.EditorTools.Modules.Targeting
{
    internal static class TargetKinds
    {
        internal static TargetKind Read(EntityManager em, Entity entity)
        {
            if (entity == Entity.Null || !em.Exists(entity)) return TargetKind.None;
            if (em.HasComponent<Game.Buildings.Building>(entity) || em.HasComponent<Game.Buildings.ServiceUpgrade>(entity)) return TargetKind.None;
            if (em.HasComponent<Game.Tools.EditorContainer>(entity))
            {
                var container = em.GetComponentData<Game.Tools.EditorContainer>(entity).m_Prefab;
                if (em.Exists(container) && em.HasComponent<EffectData>(container)) return TargetKind.Effect;
            }
            if (em.HasComponent<Game.Objects.SpawnLocation>(entity) || em.HasComponent<Game.Routes.TakeoffLocation>(entity) ||
                em.HasComponent<Game.Objects.Marker>(entity)) return TargetKind.Marker;
            if (em.HasComponent<Game.Areas.Area>(entity)) return em.HasComponent<Game.Areas.Surface>(entity) ? TargetKind.Surface :
                em.HasComponent<Game.Areas.Space>(entity) ? TargetKind.Space : TargetKind.None;
            if (em.HasComponent<Game.Net.Lane>(entity)) return TargetKind.Lane;
            if (em.HasComponent<Game.Net.Edge>(entity) || em.HasComponent<Game.Net.Node>(entity)) return TargetKind.Network;
            if (em.HasComponent<Game.Objects.Tree>(entity) || em.HasComponent<Game.Objects.Plant>(entity)) return TargetKind.Foliage;
            return em.HasComponent<Game.Objects.Object>(entity) ? TargetKind.Prop : TargetKind.None;
        }
        internal static string Label(TargetKind kind) => kind == TargetKind.Foliage ? "Tree/plant" : kind == TargetKind.Space ? "Area" : kind.ToString();
    }
}
