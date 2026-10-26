using System;
using Game;
using Game.Common;
using Game.Prefabs;
using Game.Tools;
using MW.EditorTools.Modules.DeletionProtection;
using MW.EditorTools.Modules.PropReplacer;
using MW.EditorTools.Services;
using Unity.Entities;
using Unity.Jobs;
using EditorContainer = Game.Tools.EditorContainer;

namespace MW.EditorTools.Modules.Targeting
{
    [Serializable]
    public sealed class TargetingState
    {
        public bool active, available, hoverIsSelected;
        public string outlineStatus="";
        public string filter = "all", hover = "", selected = "", owner = "", feedback = "", status = "";
    }

    public sealed partial class EditorTargetingTool : ToolBaseSystem, IViewportTargetSource<Entity>, IViewportHoverSource<Entity>
    {
        private EditorContextService context;
        private ListRecovery recovery;
        private Func<bool> allowed;
        private readonly TargetSelection<Entity> selection = new TargetSelection<Entity>();
        private readonly TargetingState state = new TargetingState();
        private static readonly bool detailedTargetDiagnostics=string.Equals(Environment.GetEnvironmentVariable("MW_EDITOR_TOOLS_DIAGNOSTICS"),"1",StringComparison.Ordinal);
        private Entity previousHover;
        private Entity pendingClick;
        private int hoverFrames;
        private Entity feedbackTarget;
        private bool feedbackSelected;
        private string lastAvailability;
        public override string toolID => "MW.EditorTools.Targeting";
        public override PrefabBase GetPrefab() => null;
        public override bool TrySetPrefab(PrefabBase prefab) => false;

        internal void Initialize(EditorContextService value, ListRecovery history, Func<bool> canUse)
        {
            if (typeof(PrefabBase).Module.ModuleVersionId.ToString() != "e15551e9-3dea-4855-8dde-1da3cea7d7ef")
                throw new NotSupportedException("Unverified targeting API build.");
            context = value; recovery = history; allowed = canUse; Enabled = false;
        }
        private bool Active => ReferenceEquals(m_ToolSystem.activeTool, this);
        private bool Ready => context?.Available == true && context.Asset != null && !context.Asset.isReadOnly &&
            !context.DialogBusy && allowed?.Invoke() == true;

        internal TargetingState GetState()
        {
            state.active = Active;
            state.available = Ready && (Active || m_ToolSystem.activeTool is DefaultToolSystem);
            state.filter = selection.Filter;
            if (!Active) state.status = context?.EditorAvailable != true ? "Targeting is available only in the asset editor." :
                context?.Asset == null ? "Select an editable asset in Object Info first." :
                context.Asset.isReadOnly ? "The selected asset is read-only. Choose an editable duplicate." :
                !(m_ToolSystem.activeTool is DefaultToolSystem) ? "Return to the native Select tool before targeting." :
                !Ready ? "Finish the current edit, save or dialog before targeting." : "Target elements without editing their data.";
            if(!Active&&inspectorEditStopped&&Ready)state.status="Replacing stopped for the Object Info edit. Click Start replacing to resume.";
            var diagnostic = state.active + "|" + state.available + "|" + (Active ? "Targeting active." : state.status);
            if (diagnostic != lastAvailability) { lastAvailability = diagnostic; MW.EditorTools.Mod.Log.Info("MW targeting availability: " + diagnostic); }
            return state;
        }
        internal void Command(string command)
        {
            if (command == "targetToggle")
            {
                if (Active) Stop();
                else if (GetState().available) { inspectorEditStopped=false; selection.SetContext(context.Asset); m_ToolSystem.activeTool = this; }
                return;
            }
            if (command == "targetClear") { selection.Clear(); ClearFeedback(); return; }
            if (command != null && command.StartsWith("targetFilter|", StringComparison.Ordinal))
            {
                if (selection.SetFilter(command.Substring(13))) { ClearFeedback(); hoverFrames = 0; }
            }
        }
        private void Stop() { if (Active) m_ToolSystem.activeTool = World.GetExistingSystemManaged<DefaultToolSystem>(); }
        internal System.Action RestoreOutlineBeforeEdit;
        private bool inspectorEditStopped;
        internal void PauseForInspectorEdit() {
            if(!Active)return;
            m_ToolRaycastSystem.GetRaycastResult(out _);
            RestoreOutlineBeforeEdit?.Invoke();
            selection.Clear(); ClearFeedback(); Stop();
            inspectorEditStopped=true;
            state.status="Replacing stopped for the Object Info edit. Click Start replacing to resume.";
            MW.EditorTools.Mod.Log.Info("MW targeting stopped before native inspector refresh; selection and owned outlines released.");
        }
        internal void Release()
        {
            Stop(); selection.Clear(); selection.SetContext(null); ClearFeedback(); context = null; recovery = null; allowed = null; RestoreOutlineBeforeEdit=null; Enabled = false;
        }
        protected override void OnStartRunning()
        {
            base.OnStartRunning(); applyAction.shouldBeEnabled = true; cancelAction.shouldBeEnabled = true;
            selection.Clear(); ClearFeedback();
        }
        protected override void OnStopRunning()
        {
            applyAction.shouldBeEnabled = false; cancelAction.shouldBeEnabled = false;
            selection.Clear(); ClearFeedback(); base.OnStopRunning();
        }
        public override void InitializeRaycast()
        {
            base.InitializeRaycast();
            bool all = selection.Filter == "all";
            m_ToolRaycastSystem.typeMask = all ? TypeMask.StaticObjects | TypeMask.Areas | TypeMask.Net | TypeMask.Lanes :
                selection.Filter == "surfaces" || selection.Filter == "spaces" ? TypeMask.Areas :
                selection.Filter == "networks" ? TypeMask.Net : selection.Filter == "lanes" ? TypeMask.Lanes : TypeMask.StaticObjects;
            m_ToolRaycastSystem.areaTypeMask = all ? Game.Areas.AreaTypeMask.Surfaces | Game.Areas.AreaTypeMask.Spaces :
                selection.Filter == "surfaces" ? Game.Areas.AreaTypeMask.Surfaces :
                selection.Filter == "spaces" ? Game.Areas.AreaTypeMask.Spaces : Game.Areas.AreaTypeMask.None;
            if ((m_ToolRaycastSystem.typeMask & TypeMask.Areas)!=0) m_ToolRaycastSystem.typeMask |= TypeMask.Terrain;
            m_ToolRaycastSystem.netLayerMask = Game.Net.Layer.All;
            m_ToolRaycastSystem.raycastFlags |= RaycastFlags.SubElements | RaycastFlags.EditorContainers | RaycastFlags.Markers | RaycastFlags.Decals | RaycastFlags.Placeholders;
            if(context?.Asset is BuildingPrefab && (m_ToolRaycastSystem.typeMask & TypeMask.Areas)==0)m_ToolRaycastSystem.raycastFlags|=RaycastFlags.NoMainElements;
        }
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            if (context?.EditorAvailable != true) { Stop(); return inputDeps; }
            if (cancelAction.WasPressedThisFrame()) { Stop(); return inputDeps; }
            if (selection.SetContext(context.Asset)) { ClearFeedback(); hoverFrames = 0; }
            if (!Ready) { selection.Clear(); ClearFeedback(); state.status = "Targeting paused while editing, saving or a dialog is open."; return inputDeps; }
            try
            {
                var em = EntityManager;
                if (selection.HasSelected && !Valid(selection.Selected, out _)) { selection.Clear(); ClearFeedback(); }
                Entity hit = Entity.Null, rayOwner=Entity.Null, rayHit=Entity.Null;
                Unity.Mathematics.float3 rayPosition=default;
                string reason = "Hover an element, then click to select it for MW copy/replace.";
                var blocked = (m_ToolRaycastSystem.raycastFlags & (RaycastFlags.UIDisable | RaycastFlags.ToolDisable | RaycastFlags.FreeCameraDisable | RaycastFlags.DebugDisable)) != 0;
                if (!blocked && m_ToolRaycastSystem.GetRaycastResult(out RaycastResult ray))
                {
                    rayOwner=ray.m_Owner;rayHit=ray.m_Hit.m_HitEntity;rayPosition=ray.m_Hit.m_HitPosition;
                    // Ground-area results retain the terrain hit but identify the polygon in m_Owner.
                    hit=ResolveRayHit(ray);
                    if(!ViewportHitPolicy.MatchesPath(hit,ray.m_Owner,Stable,
                        id=>EntityManager.HasComponent<Owner>(id)?Tuple.Create(true,EntityManager.GetComponentData<Owner>(id).m_Owner):Tuple.Create(false,Entity.Null),Links))
                    {reason="The raycast hit's ownership could not be verified. Hover the intended element again.";hit=Entity.Null;}
                    else if (!Valid(hit, out reason)) hit = Entity.Null;
                    else if(!SurfaceHitValid(hit,ray,out reason))hit=Entity.Null;
                }
                var kind = TargetKinds.Read(em, hit);
                if (hit != Entity.Null && !TargetSelection<Entity>.Matches(selection.Filter, kind))
                { reason = TargetKinds.Label(kind) + " is excluded by this filter. Move the cursor to an allowed element."; hit = Entity.Null; }
                selection.SetHover(hit, hit == Entity.Null ? TargetKind.None : kind);
                hoverFrames = hit != Entity.Null && hit == previousHover ? hoverFrames + 1 : 0;
                if(detailedTargetDiagnostics&&hit!=previousHover&&(kind==TargetKind.Surface||kind==TargetKind.Space))
                    MW.EditorTools.Mod.Log.Info("MW surface hover: entity="+hit+" name="+Describe(hit)+" rayOwner="+rayOwner+" rayHit="+rayHit+" position="+rayPosition+" boundary="+SurfaceBounds(hit)+" selected="+(selection.HasSelected?selection.Selected:Entity.Null));
                previousHover = hit;
                if(blocked||hit==Entity.Null||pendingClick!=Entity.Null&&pendingClick!=hit)pendingClick=Entity.Null;
                if(!blocked&&hit!=Entity.Null&&applyAction.WasPressedThisFrame()) {
                    if(selection.HasSelected&&selection.Selected!=hit){selection.ClearSelected();feedbackTarget=Entity.Null;}
                    pendingClick=hit;
                }
                if (hit != Entity.Null && hoverFrames > 0 && pendingClick==hit)
                {
                    selection.SelectHover(); feedbackTarget = Entity.Null;pendingClick=Entity.Null;
                }
                state.hover = selection.HasHover ? Describe(selection.Hover) : "";
                state.selected = selection.HasSelected ? Describe(selection.Selected) : "";
                state.hoverIsSelected = selection.HasHover && selection.HasSelected && selection.Hover == selection.Selected;
                state.owner = context.Asset?.name ?? "";
                state.status = pendingClick!=Entity.Null ? "Confirming the clicked element. Copy/Replace will use it once the same hit is stable." : hit == Entity.Null ? reason : selection.HasSelected ? "Panel buttons use cyan selection; viewport shortcuts use gold hover. Return to native Select for moving or deleting." :
                    "Gold hover is the shortcut target. Click for panel selection and Undo eligibility.";
                var target = selection.HasSelected ? selection.Selected : hit;
                if (target != Entity.Null && (target != feedbackTarget || feedbackSelected != selection.HasSelected))
                {
                    ClearNativeHighlights();
                    feedbackTarget = target; feedbackSelected = selection.HasSelected;
                    state.feedback = recovery?.TargetUndoFeedback(target, selection.HasSelected) ?? "MW Undo is unavailable in this session.";
                }
                if (target == Entity.Null) { feedbackTarget = Entity.Null; state.feedback = ""; }
                DrawHighlights(inputDeps);
            }
            catch (Exception error)
            {
                selection.Clear(); ClearFeedback(); state.status = "Targeting unavailable: " + error.Message;
            }
            return inputDeps;
        }
        private void ClearFeedback()
        {
            ClearNativeHighlights();
            feedbackTarget = previousHover = pendingClick = Entity.Null; hoverFrames = 0;
            state.hover = state.selected = state.feedback = state.owner = "";
            state.hoverIsSelected = false;
        }
        internal void SetOutlineStatus(string value) { state.outlineStatus=value; }
        internal bool OwnsNativeHighlight(Entity id) => nativeHighlights.Contains(id) && EntityManager.HasComponent<Highlighted>(id);
        internal void GetOutlineTargets(out Entity hover, out Entity selected)
        {
            hover=selected=Entity.Null;
            if(!Active || !Ready || selection.SetContext(context.Asset)) return;
            if(selection.HasSelected && Valid(selection.Selected,out _)) selected=selection.Selected;
            if(selection.HasHover && Valid(selection.Hover,out _)) hover=selection.Hover;
        }
        private bool Stable(Entity id) => id != Entity.Null && EntityManager.Exists(id) &&
            !EntityManager.HasComponent<Deleted>(id) && !EntityManager.HasComponent<Created>(id) &&
            !EntityManager.HasComponent<Updated>(id) && !EntityManager.HasComponent<Temp>(id);
        private bool Valid(Entity id, out string reason)
        {
            reason = "The element or its owning asset is no longer available or is still updating.";
            if (!Stable(id) || !EntityManager.HasComponent<PrefabRef>(id)) return false;
            if (TargetKinds.Read(EntityManager, id) == TargetKind.None) { reason = "Target a prop, plant, effect, marker, surface, space, network segment or lane."; return false; }
            var prefabs = World.GetExistingSystemManaged<PrefabSystem>();
            var current = id; var seen = new System.Collections.Generic.HashSet<Entity>();
            bool matchesContext=false;
            for (int depth = 0; depth < 9; depth++)
            {
                if (!Stable(current) || !seen.Add(current) || !EntityManager.HasComponent<PrefabRef>(current)) return false;
                var prefab = prefabs?.GetPrefab<PrefabBase>(EntityManager.GetComponentData<PrefabRef>(current));
                if (prefab == null) { reason = "The element's prefab is unavailable."; return false; }
                // Built-in prop/effect prefabs are commonly read-only references.
                // Only the inspected owning asset is edited, never those references.
                if (ReferenceEquals(prefab, context.Asset)) matchesContext=!prefab.isReadOnly;
                if (!EntityManager.HasComponent<Owner>(current)) {
                    if(matchesContext)return true;
                    break;
                }
                var parent = EntityManager.GetComponentData<Owner>(current).m_Owner;
                if (!Stable(parent) || Links(parent, current) != 1) { reason = "The element has no unique link to its owner."; return false; }
                current = parent;
            }
            reason = "This element belongs to a different asset. Inspect that asset first."; return false;
        }
        private int Links(Entity parent, Entity child)
        {
            int count = 0;
            if (EntityManager.HasBuffer<Game.Objects.SubObject>(parent)) foreach (var value in EntityManager.GetBuffer<Game.Objects.SubObject>(parent, true)) if (value.m_SubObject == child) count++;
            if (EntityManager.HasBuffer<Game.Areas.SubArea>(parent)) foreach (var value in EntityManager.GetBuffer<Game.Areas.SubArea>(parent, true)) if (value.m_Area == child) count++;
            if (EntityManager.HasBuffer<Game.Net.SubNet>(parent)) foreach (var value in EntityManager.GetBuffer<Game.Net.SubNet>(parent, true)) if (value.m_SubNet == child) count++;
            if (EntityManager.HasBuffer<Game.Net.SubLane>(parent)) foreach (var value in EntityManager.GetBuffer<Game.Net.SubLane>(parent, true)) if (value.m_SubLane == child) count++;
            return count;
        }
        private string Describe(Entity id)
        {
            var prefabs = World.GetExistingSystemManaged<PrefabSystem>();
            var kind=TargetKinds.Read(EntityManager,id);
            var reference = (kind==TargetKind.Effect||kind==TargetKind.Lane)&&EntityManager.HasComponent<EditorContainer>(id) ? EntityManager.GetComponentData<EditorContainer>(id).m_Prefab : Entity.Null;
            var prefab = reference != Entity.Null ? prefabs?.GetPrefab<PrefabBase>(reference) : prefabs?.GetPrefab<PrefabBase>(EntityManager.GetComponentData<PrefabRef>(id));
            return TargetKinds.Label(TargetKinds.Read(EntityManager, id)) + ": " + (prefab?.name ?? "Unnamed element");
        }
        bool IViewportTargetSource<Entity>.TryGetTarget(out Entity target, out string reason)
        {
            ClearNativeHighlights();
            target = Entity.Null; reason = "Click an allowed element before using panel Copy/Replace.";
            if (!Active || !Ready || selection.SetContext(context.Asset)) { selection.Clear(); ClearFeedback(); return false; }
            if(pendingClick!=Entity.Null){reason="Wait for the clicked element to be confirmed before Copy/Replace.";return false;}
            if(!selection.HasSelected){reason="Click an element before using the panel buttons. Viewport shortcuts use the hovered element.";return false;}
            string refusal=null;
            if (!selection.TryTarget(id => Valid(id, out refusal) && TargetSelection<Entity>.Matches(selection.Filter, TargetKinds.Read(EntityManager, id)), out target)) {
                ClearFeedback();
                if(refusal!=null){reason=refusal;state.status=refusal;}
                return false;
            }
            reason = null; return true;
        }
        private Entity ResolveRayHit(RaycastResult ray) {
            var actual=ray.m_Hit.m_HitEntity;var ownerKind=TargetKinds.Read(EntityManager,ray.m_Owner);
            return TargetKinds.Read(EntityManager,actual)!=TargetKind.None?actual:
                ownerKind==TargetKind.Surface||ownerKind==TargetKind.Space?ray.m_Owner:actual==Entity.Null?ray.m_Owner:actual;
        }
        private string SurfaceBounds(Entity id) {
            if(!EntityManager.HasBuffer<Game.Areas.Node>(id))return "unavailable";
            var nodes=EntityManager.GetBuffer<Game.Areas.Node>(id,true);
            if(nodes.Length==0)return "empty";
            if(nodes.Length>4096)return "over budget";
            var min=nodes[0].m_Position;var max=min;
            for(int i=1;i<nodes.Length;i++){min=Unity.Mathematics.math.min(min,nodes[i].m_Position);max=Unity.Mathematics.math.max(max,nodes[i].m_Position);}
            return min+".."+max;
        }
        private bool SurfaceHitValid(Entity hit,RaycastResult ray,out string reason) {
            reason=null;
            if(TargetKinds.Read(EntityManager,hit)!=TargetKind.Surface)return true;
            if(!EntityManager.HasBuffer<Game.Areas.Node>(hit)){reason="Surface boundary is unavailable.";return false;}
            var nodes=EntityManager.GetBuffer<Game.Areas.Node>(hit,true);
            if(!SurfacePointPolicy.Contains(ray.m_Hit.m_HitPosition,nodes.Length,i=>nodes[i].m_Position)){
                reason="The surface ray hit is outside its boundary. Move the cursor over the intended surface.";return false;
            }
            return true;
        }
        bool IViewportHoverSource<Entity>.TryGetHoverTarget(out Entity target,out string reason) {
            ClearNativeHighlights();target=Entity.Null;reason="Hover an allowed element in the viewport before using the shortcut.";
            if(!Active||!Ready||selection.SetContext(context.Asset)){selection.Clear();ClearFeedback();return false;}
            if(pendingClick!=Entity.Null){reason="Wait for the clicked element to be confirmed.";return false;}
            var blocked=(m_ToolRaycastSystem.raycastFlags&(RaycastFlags.UIDisable|RaycastFlags.ToolDisable|RaycastFlags.FreeCameraDisable|RaycastFlags.DebugDisable))!=0;
            if(blocked||!selection.HasHover||hoverFrames<1||!m_ToolRaycastSystem.GetRaycastResult(out RaycastResult ray))return false;
            var current=ResolveRayHit(ray);
            if(current!=selection.Hover){reason="The cursor target changed. Wait for its hover outline before using the shortcut.";return false;}
            if(!ViewportHitPolicy.MatchesPath(current,ray.m_Owner,Stable,
                id=>EntityManager.HasComponent<Owner>(id)?Tuple.Create(true,EntityManager.GetComponentData<Owner>(id).m_Owner):Tuple.Create(false,Entity.Null),Links)||
                !Valid(current,out reason)||!TargetSelection<Entity>.Matches(selection.Filter,TargetKinds.Read(EntityManager,current))||!SurfaceHitValid(current,ray,out reason))return false;
            target=current;reason=null;
            if(TargetKinds.Read(EntityManager,current)==TargetKind.Surface)MW.EditorTools.Mod.Log.Info("MW surface shortcut target: hover="+current+" selected="+(selection.HasSelected?selection.Selected:Entity.Null)+" position="+ray.m_Hit.m_HitPosition);
            return true;
        }
    }
}
