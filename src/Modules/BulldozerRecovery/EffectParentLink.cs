using System;
using Unity.Entities;
namespace MW.EditorTools.Modules.BulldozerRecovery {
    internal sealed partial class BulldozerTrace {
        private void LinkIsolatedEffect(Entity owner,Entity effect) {
            var em=world.EntityManager;
            if(!em.Exists(owner)||!em.Exists(effect)||!em.HasComponent<Game.Common.Owner>(effect)||em.GetComponentData<Game.Common.Owner>(effect).m_Owner!=owner)throw new NotSupportedException("Fresh effect ownership changed before linking.");
            var links=em.GetBuffer<Game.Objects.SubObject>(owner);
            // DynamicBuffer's explicit IEnumerable enumerators throw in this
            // game build. Use indexed access rather than LINQ/Enumerable here.
            for(int i=0;i<links.Length;i++)if(links[i].m_SubObject==effect)throw new NotSupportedException("Fresh effect already linked to parent.");
            links.Add(new Game.Objects.SubObject{m_SubObject=effect});
        }
    }
}
