using Game.Tools;
using Unity.Entities;
namespace MW.EditorTools.Modules.BulldozerRecovery {
    internal sealed partial class BulldozerTrace {
        // Caller preflights the complete after-image before entering this method.
        private void WriteAreaMoveGeometry(Entity id,StandaloneAreaEntry before) {
            var nodes=before.Data.Nodes;var local=before.Data.Local;var em=world.EntityManager;
            var buffer=em.GetBuffer<Game.Areas.Node>(id);buffer.Clear();foreach(var n in nodes)buffer.Add(n);
            if(local!=null){var cache=em.GetBuffer<LocalNodeCache>(id);cache.Clear();foreach(var n in local)cache.Add(n);}
            else if(em.HasBuffer<LocalNodeCache>(id))em.RemoveComponent<LocalNodeCache>(id);
            if(before.HasExpand){var expansion=em.GetBuffer<Game.Areas.Expand>(id);expansion.Clear();foreach(var value in before.Expand)expansion.Add(value);}
            if(before.HasSeed)em.SetComponentData(id,before.Seed);
            PutMain(id,new Game.Common.Updated());PutMain(id,new Game.Common.BatchesUpdated());
        }
    }
}
