using System;
using System.Reflection;
using Game.Common;
using Game.Prefabs;
using Game.Tools;
using MW.EditorTools.Modules.BulldozerRecovery;
using MW.EditorTools.Modules.DeletionProtection;
using MW.EditorTools.Modules.PropReplacer;
using MW.EditorTools.Modules.Targeting;
using MW.EditorTools.Services;
using Unity.Entities;

internal static class Program
{
    private static int checks;
    private static void Check(bool value, string name) { if (!value) throw new Exception(name); checks++; }
    private static Entity Id(int value) => new Entity { Index = value };
    private static void Set(object value, string name, object field) => value.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(value, field);
    private static void Main()
    {
        var model = new TargetSelection<int>(); var asset = new object(); var undo = new object(); var oldUndo = undo;
        model.SetContext(asset);
        foreach (TargetKind kind in Enum.GetValues(typeof(TargetKind)))
            Check(TargetSelection<int>.Matches("all", kind) == (kind != TargetKind.None), "all filter excludes unsupported elements");
        Check(!model.SetFilter("unknown") && model.Filter == "all", "unknown filter rejected");
        model.SetFilter("effects"); model.SetHover(1, TargetKind.Prop); Check(!model.SelectHover(), "excluded prop cannot be selected as effect");
        model.SetHover(2, TargetKind.Effect); Check(model.SelectHover() && model.Selected == 2, "allowed effect selected");
        model.SetHover(3, TargetKind.Effect); Check(model.TryTarget(_ => true, out var chosen) && chosen == 2, "explicit selection wins over later hover");
        Check(!model.TryTarget(id => id != 2, out chosen) && !model.HasHover && !model.HasSelected, "stale selection cannot silently fall back to hover");
        model.SetHover(2, TargetKind.Effect); model.SelectHover(); model.SetFilter("props"); Check(!model.HasSelected && !model.HasHover, "filter changes clear both selections");
        model.SetHover(4, TargetKind.Prop); model.SelectHover(); Check(!model.SetContext(asset) && model.Selected == 4, "same asset context preserves selection");
        model.SetContext(new object()); Check(!model.HasSelected && !model.HasHover, "asset switch expires selection");
        Check(ReferenceEquals(undo, oldUndo), "selection owns no Undo entry");

        var world = new World(); var em = world.EntityManager; var tools = new ToolSystem(); var ray = new ToolRaycastSystem(); var prefabs = new PrefabSystem();
        var normal = new DefaultToolSystem { World = world }; var tool = new EditorTargetingTool { World = world };
        world.Register(tools); world.Register(ray); world.Register(prefabs); world.Register(normal); world.Register(new Game.Rendering.OverlayRenderSystem());
        tools.activeTool = normal;
        var root = new PrefabBase { name = "Editable house" }; var builtIn = new PrefabBase { name = "Bench", isReadOnly = true };
        prefabs.Prefabs[Id(100)] = root; prefabs.Prefabs[Id(101)] = builtIn;
        em.Put(Id(100), new PrefabRef { m_Prefab = Id(100) }); em.Put(Id(101), new PrefabRef { m_Prefab = Id(101) });
        em.Put(Id(10), new PrefabRef { m_Prefab = Id(100) });
        foreach (var id in new[] { Id(1), Id(2) })
        {
            em.Put(id, new PrefabRef { m_Prefab = Id(101) }); em.Put(id, new Game.Objects.Object());
            em.Put(id, new Game.Objects.Transform()); em.Put(id, new Owner { m_Owner = Id(10) });
        }
        em.SetBuffer(Id(10), new Game.Objects.SubObject { m_SubObject = Id(1) }, new Game.Objects.SubObject { m_SubObject = Id(2) });
        var context = new EditorContextService { Asset = root }; var recovery = new ListRecovery { World=world }; var recoverySlot = recovery.UndoSlot; bool allowed = true;
        bool rejectedBuild = false;
        try { tool.Initialize(context, recovery, () => allowed); } catch (NotSupportedException) { rejectedBuild = true; }
        Check(rejectedBuild, "unverified game assembly refuses initialization");
        Set(tool, "context", context); Set(tool, "recovery", recovery); Set(tool, "allowed", (Func<bool>)(() => allowed));
        Check(tool.GetState().available && !tool.GetState().active, "targeting available without activation");
        var wire = Newtonsoft.Json.Linq.JObject.Parse(Newtonsoft.Json.JsonConvert.SerializeObject(new { targeting = tool.GetState() }));
        Check((bool)wire["targeting"]["available"] && !(bool)wire["targeting"]["active"] && ((string)wire["targeting"]["status"]).Length > 0, "wire snapshot retains nested targeting availability and reason");
        var other = new ToolBaseSystem { World = world }; tools.activeTool = other; tool.Command("targetToggle");
        Check(ReferenceEquals(tools.activeTool, other), "does not steal another mod's active tool");
        tools.activeTool = normal; int writes = em.Writes;
        tool.Command("targetToggle"); Check(ReferenceEquals(tools.activeTool, tool) && tool.applyAction.shouldBeEnabled && tool.cancelAction.shouldBeEnabled, "explicit activation owns only targeting inputs");
        tool.InitializeRaycast(); Check((ray.typeMask & TypeMask.Lanes) != 0 && (ray.areaTypeMask & Game.Areas.AreaTypeMask.Spaces) != 0, "all filter includes lanes and spaces");
        context.Asset=new BuildingPrefab();tool.Command("targetFilter|props");tool.InitializeRaycast();Check((ray.raycastFlags&RaycastFlags.NoMainElements)!=0,"static-only building targeting requests subelements");
        tool.Command("targetFilter|surfaces");tool.InitializeRaycast();Check((ray.typeMask&TypeMask.Terrain)!=0&&(ray.raycastFlags&RaycastFlags.NoMainElements)==0,"surface raycast includes required ground seed without main-element refusal");
        tool.Command("targetFilter|all");context.Asset=root;tool.InitializeRaycast();
        Check((ray.raycastFlags & RaycastFlags.SubElements) != 0 && (ray.raycastFlags & RaycastFlags.EditorContainers) != 0, "raycast includes exact subelements and effects");
        ray.Hit = true; ray.Result = new RaycastResult { m_Owner = Id(10), m_Hit = new RaycastHit { m_HitEntity = Id(1) } };
        var hoverCircles = Game.Rendering.OverlayRenderSystem.Circles;
        tool.applyAction.Pressed = true; tool.Tick(); Check(tool.GetState().selected == "" && recovery.DeepProbes == 0, "fresh unconfirmed ray cannot select or run expensive preflight");
        Check(Game.Rendering.OverlayRenderSystem.Circles == hoverCircles && em.HasComponent<Highlighted>(Id(1)), "prop hover uses native outline without a circular overlay");
        tool.Tick(); Check(tool.GetState().selected.Contains("Bench") && recovery.DeepProbes == 1, "stable click selects read-only referenced prop inside editable owner and preflights once");
        tool.applyAction.Pressed = false; tool.Tick(); tool.Tick();
        Check(recovery.DeepProbes == 1, "stable selected target does not repeat full preflight each frame");
        Check(em.HasComponent<Highlighted>(Id(1)), "native silhouette is requested for targeted object");
        Check(Game.Rendering.OverlayRenderSystem.Circles == 0, "hover and selection have no circular overlays");
        ray.Result = new RaycastResult { m_Owner = Id(10), m_Hit = new RaycastHit { m_HitEntity = Id(2) } }; tool.Tick();
        Check(!tool.GetState().hoverIsSelected && tool.GetState().hover == tool.GetState().selected, "identical names do not merge distinct hover/selection identities");
        var source = (IViewportTargetSource<Entity>)tool;
        Check(source.TryGetTarget(out var target, out _) && target == Id(1), "clipboard uses explicitly selected entity rather than another hovered instance");
        Check(!em.HasComponent<Highlighted>(Id(1))&&!em.HasComponent<Highlighted>(Id(2)),"clipboard dispatch removes owned render-only tags before capture");
        tool.Tick();var hoverSource=(IViewportHoverSource<Entity>)tool;
        Check(hoverSource.TryGetHoverTarget(out var hoverTarget,out _)&&hoverTarget==Id(2),"viewport shortcut chooses verified hover instead of pinned selection");
        Check(source.TryGetTarget(out target,out _)&&target==Id(1),"panel buttons retain explicit selection independently of hover shortcut");
        ray.Result=new RaycastResult{m_Owner=Id(10),m_Hit=new RaycastHit{m_HitEntity=Id(1)}};
        Check(!hoverSource.TryGetHoverTarget(out _,out _),"changed current ray cannot reuse stale hover or fall back to pinned selection");
        ray.Result=new RaycastResult{m_Owner=Id(10),m_Hit=new RaycastHit{m_HitEntity=Id(2)}};

        ray.raycastFlags |= RaycastFlags.UIDisable; tool.InitializeRaycast();
        Check((ray.raycastFlags&RaycastFlags.UIDisable)!=0,"category masks retain native UI raycast blocking");
        tool.applyAction.Pressed = true; tool.Tick();
        Check(source.TryGetTarget(out target, out _) && target == Id(1), "clicking UI cannot select the world behind it");
        Check(!hoverSource.TryGetHoverTarget(out _,out _),"viewport shortcuts refuse while UI blocks the ray instead of copying a pinned surface");
        ray.raycastFlags &= ~RaycastFlags.UIDisable; tool.applyAction.Pressed = false;
        em.AddComponent<Highlighted>(Id(2)); tool.Tick();
        tool.Command("targetFilter|surfaces"); tool.InitializeRaycast();
        Check(!em.HasComponent<Highlighted>(Id(1))&&em.HasComponent<Highlighted>(Id(2)),"filter cleanup removes only MW-owned highlights and preserves foreign tags"); em.RemoveComponent<Highlighted>(Id(2));
        Check(ray.typeMask == (TypeMask.Areas|TypeMask.Terrain) && ray.areaTypeMask == Game.Areas.AreaTypeMask.Surfaces && tool.GetState().selected == "", "surface filter changes native mask and clears selected prop");
        tool.Tick(); Check(!source.TryGetTarget(out _, out _) && tool.GetState().status.Contains("excluded"), "fine category mismatch is not selected or sent to clipboard");
        tool.Command("targetFilter|all"); tool.InitializeRaycast();
        em.Put(Id(3), new Game.Areas.Area()); em.Put(Id(3), new Game.Areas.Surface()); em.Put(Id(3), new PrefabRef { m_Prefab = Id(101) }); em.Put(Id(3), new Owner { m_Owner = Id(10) });
        em.SetBuffer(Id(10), new Game.Areas.SubArea { m_Area = Id(3) }); em.SetBuffer(Id(3), new Game.Areas.Node{m_Position=new Unity.Mathematics.float3(0,0,0)}, new Game.Areas.Node{m_Position=new Unity.Mathematics.float3(2,0,0)}, new Game.Areas.Node{m_Position=new Unity.Mathematics.float3(0,0,2)});
        tool.Command("targetFilter|surfaces");tool.InitializeRaycast();
        ray.Result = new RaycastResult { m_Owner = Id(3), m_Hit = new RaycastHit { m_HitEntity = Id(200) } }; tool.Tick();
        Check(tool.GetState().hover.StartsWith("Surface:"), "surface polygon owner wins over retained terrain hit");
        Check(Game.Rendering.OverlayRenderSystem.Lines == 3, "surface highlight follows node geometry without editing it");
        ray.Result=new RaycastResult{m_Owner=Id(3),m_Hit=new RaycastHit{m_HitEntity=Id(200),m_HitPosition=new Unity.Mathematics.float3(9,0,9)}};tool.Tick();
        Check(tool.GetState().hover==""&&tool.GetState().status.Contains("outside"),"surface candidate outside its actual polygon is refused before hover/highlight");
        ray.Result=new RaycastResult{m_Owner=Id(3),m_Hit=new RaycastHit{m_HitEntity=Id(200)}};tool.Tick();tool.Tick();
        Check(hoverSource.TryGetHoverTarget(out hoverTarget,out _)&&hoverTarget==Id(3),"ground surface shortcut validates actual boundary point");

        writes = em.Writes; tool.Tick(); Check(em.Writes == writes && ReferenceEquals(recoverySlot, recovery.UndoSlot), "targeting/highlighting/preflight do not write persistent data or replace Undo slot");
        prefabs.Prefabs[Id(102)]=new PrefabBase{name="Tiles Different",isReadOnly=true};em.Put(Id(102),new PrefabRef{m_Prefab=Id(102)});
        em.Put(Id(6),new Game.Areas.Area());em.Put(Id(6),new Game.Areas.Surface());em.Put(Id(6),new PrefabRef{m_Prefab=Id(102)});em.Put(Id(6),new Owner{m_Owner=Id(10)});em.SetBuffer(Id(6),new Game.Areas.Node{m_Position=new Unity.Mathematics.float3(0,0,0)},new Game.Areas.Node{m_Position=new Unity.Mathematics.float3(2,0,0)},new Game.Areas.Node{m_Position=new Unity.Mathematics.float3(0,0,2)});
        em.SetBuffer(Id(10),new Game.Areas.SubArea{m_Area=Id(3)},new Game.Areas.SubArea{m_Area=Id(6)});
        em.Put(Id(3),new EditorContainer{m_Prefab=Id(102)});tool.Tick();Check(tool.GetState().hover.Contains("Bench")&&!tool.GetState().hover.Contains("Tiles Different"),"surface name uses live PrefabRef instead of unrelated/stale editor container reference");em.Drop<EditorContainer>(Id(3));
        ray.Result=new RaycastResult{m_Owner=Id(3),m_Hit=new RaycastHit{m_HitEntity=Id(6)}};tool.Tick();Check(tool.GetState().hover==""&&tool.GetState().status.Contains("ownership"),"conflicting surface owner and specific surface hit refuse instead of naming the owner");
        ray.Result=new RaycastResult{m_Owner=Id(6),m_Hit=new RaycastHit{m_HitEntity=Id(200)}};tool.applyAction.Pressed=true;tool.Tick();Check(!source.TryGetTarget(out _,out var pendingReason)&&pendingReason.Contains("confirmed"),"fresh surface click blocks clipboard while its exact hit is pending confirmation");
        tool.applyAction.Pressed=false;tool.Tick();Check(source.TryGetTarget(out var surfaceTarget,out _)&&surfaceTarget==Id(6)&&tool.GetState().selected.Contains("Tiles Different"),"one-frame click selects the new surface after stable confirmation without requiring a second click");
        ray.Result=new RaycastResult{m_Owner=Id(3),m_Hit=new RaycastHit{m_HitEntity=Id(200)}};tool.applyAction.Pressed=true;tool.Tick();
        ray.Result=new RaycastResult{m_Owner=Id(6),m_Hit=new RaycastHit{m_HitEntity=Id(200)}};tool.applyAction.Pressed=false;tool.Tick();Check(!source.TryGetTarget(out surfaceTarget,out _)&&tool.GetState().selected=="","moving away before confirmation cancels the click and leaves panel buttons without an implicit hover fallback");
        tool.Tick();tool.applyAction.Pressed=true;tool.Tick();tool.applyAction.Pressed=false;
        ray.Result=new RaycastResult{m_Owner=Id(3),m_Hit=new RaycastHit{m_HitEntity=Id(200)}};tool.applyAction.Pressed=true;tool.Tick();Check(tool.GetState().selected=="","a fresh click clears the old pinned surface instead of leaving it available for clipboard use");
        ray.raycastFlags|=RaycastFlags.UIDisable;tool.applyAction.Pressed=false;tool.Tick();Check(!source.TryGetTarget(out _,out _),"moving into the UI before click confirmation cannot copy the previous surface");ray.raycastFlags&=~RaycastFlags.UIDisable;
        tool.Command("targetClear");ray.Result=new RaycastResult{m_Owner=Id(3),m_Hit=new RaycastHit{m_HitEntity=Id(200)}};tool.Tick();tool.Tick();
        context.Asset = new PrefabBase { name = "Different house" }; Check(!source.TryGetTarget(out _, out _), "clipboard context switch expires target before dispatch");
        context.Asset = root; tool.Tick(); tool.Tick(); tool.applyAction.Pressed = true; tool.Tick(); tool.applyAction.Pressed = false;
        em.Put(Id(3), new Updated()); Check(!source.TryGetTarget(out _, out _), "updating selected entity is refused without retargeting"); em.Drop<Updated>(Id(3));
        tool.Command("targetClear"); Check(tool.GetState().selected == "" && tool.GetState().feedback == "", "clear selection clears eligibility text");
        em.Put(Id(20),new PrefabRef{m_Prefab=Id(100)});ray.Result=new RaycastResult{m_Owner=Id(20),m_Hit=new RaycastHit{m_HitEntity=Id(1)}};
        tool.Tick();Check(!source.TryGetTarget(out _,out _)&&tool.GetState().status.Contains("ownership"),"a different ray owner with the same prefab cannot authorise the hit");
        ray.Result=new RaycastResult{m_Owner=Id(10),m_Hit=new RaycastHit{m_HitEntity=Id(1)}};tool.Tick();tool.Tick();
        em.SetBuffer(Id(10),new Game.Objects.SubObject{m_SubObject=Id(1)},new Game.Objects.SubObject{m_SubObject=Id(1)});
        Check(!source.TryGetTarget(out _,out _),"duplicate owner links cannot become a selected clipboard target");
        em.SetBuffer(Id(10),new Game.Objects.SubObject{m_SubObject=Id(1)},new Game.Objects.SubObject{m_SubObject=Id(2)});
        tool.Tick();tool.Tick();em.Put(Id(10),new Owner{m_Owner=Id(10)});
        Check(!source.TryGetTarget(out _,out _),"matching asset identity cannot bypass a cyclic or broken ancestor chain");em.Drop<Owner>(Id(10));
        tool.cancelAction.Pressed = true; tool.Tick(); Check(ReferenceEquals(tools.activeTool, normal) && !tool.applyAction.shouldBeEnabled && !tool.cancelAction.shouldBeEnabled, "Escape returns to original native selection tool and releases actions");
        tool.cancelAction.Pressed = false; tool.Command("targetToggle"); allowed = false; tool.Tick();
        Check(!source.TryGetTarget(out _, out _) && tool.GetState().selected == "", "busy/dialog/save gate pauses targeting and clears action target");
        allowed = true; tool.Release();
        Check(!em.HasComponent<Highlighted>(Id(1))&&!em.HasComponent<Highlighted>(Id(2)),"owned silhouette tags removed on tool exit"); tool.Release(); Check(ReferenceEquals(tools.activeTool, normal), "release is idempotent and preserves native tool");
        Check(TargetKinds.Label(TargetKind.Space)=="Area", "native space category is presented as Area");
        var versioned=new TargetSelection<Entity>();versioned.SetContext(root);versioned.SetHover(Id(1),TargetKind.Prop);versioned.SelectHover();
        em.Destroy(Id(1));var regenerated=new Entity{Index=1,Version=1};em.Put(regenerated,new Game.Objects.Object());
        versioned.SetHover(regenerated,TargetKind.Prop);Check(!versioned.TryTarget(em.Exists,out _),"recycled entity index cannot retarget a selected entity with an old version");
        em.Put(Id(1),new PrefabRef{m_Prefab=Id(101)});em.Put(Id(1),new Game.Objects.Object());em.Put(Id(1),new Owner{m_Owner=Id(10)});
        FeedbackChecks(world, context, Id(1), Id(3));
        var polygon=new[]{new Unity.Mathematics.float3(0,0,0),new Unity.Mathematics.float3(2,0,0),new Unity.Mathematics.float3(2,0,2),new Unity.Mathematics.float3(1,0,1),new Unity.Mathematics.float3(0,0,2)};
        Check(SurfacePointPolicy.Contains(new Unity.Mathematics.float3(.5f,100,.5f),polygon.Length,i=>polygon[i]),"ground surface point uses XZ projection independently of terrain height");
        Check(!SurfacePointPolicy.Contains(new Unity.Mathematics.float3(1,0,1.5f),polygon.Length,i=>polygon[i]),"concave polygon gap is not treated as its bounding rectangle");
        Check(SurfacePointPolicy.Contains(new Unity.Mathematics.float3(2,0,1),polygon.Length,i=>polygon[i]),"surface boundary edge is included");
        Check(!SurfacePointPolicy.Contains(new Unity.Mathematics.float3(float.NaN,0,0),polygon.Length,i=>polygon[i]),"nonfinite ray point refused");
        Check(!SurfacePointPolicy.Contains(new Unity.Mathematics.float3(),3,i=>new Unity.Mathematics.float3()),"degenerate polygon refused");
        Set(tool,"context",context);Set(tool,"recovery",recovery);Set(tool,"allowed",(Func<bool>)(()=>allowed));
        context.Asset=root;allowed=true;tools.activeTool=normal;tool.Command("targetToggle");
        bool restored=false;tool.RestoreOutlineBeforeEdit=()=>{restored=true;Check(ReferenceEquals(tools.activeTool,tool),"native outline restoration happens before targeting stops");};
        tool.PauseForInspectorEdit();Check(restored&&ReferenceEquals(tools.activeTool,normal)&&!tool.GetState().active,"vanilla inspector edit releases targeting before native refresh");
        Check(ReferenceEquals(recovery.UndoSlot,recoverySlot),"inspector targeting cleanup does not mutate working Undo slot");
        restored=false;tool.PauseForInspectorEdit();Check(!restored,"native inspector edits leave unrelated/default tools alone");
        tool.Command("targetToggle");tool.RestoreOutlineBeforeEdit=()=>throw new Exception("render cleanup fault");bool blockedCleanup=false;try{tool.PauseForInspectorEdit();}catch(Exception){blockedCleanup=true;}Check(blockedCleanup&&ReferenceEquals(tools.activeTool,tool),"cleanup failure propagates so edit guard can refuse native rebuild");tool.RestoreOutlineBeforeEdit=null;tool.PauseForInspectorEdit();
        Console.WriteLine("PASS: " + checks + " actual-source targeting, overlay, selection and read-only Undo feedback assertions. Native tool acceptance pending.");
    }
    private static void FeedbackChecks(World world, EditorContextService context, Entity prop, Entity surface)
    {
        var trace = new BulldozerTrace { world = world, context = context }; var slot = trace.UndoSlot; int writes = world.EntityManager.Writes;
        Check(trace.TargetUndoFeedback(prop, false).Contains("candidate") && trace.Captures == 0, "hover reports provisional eligibility without full snapshot allocation");
        Check(trace.TargetUndoFeedback(prop, true).Contains("preflight passed") && trace.Captures == 2, "selected bound prop runs detached group and companion preflight");
        trace.Failure = "Unhandled nested component: Anarchy.Components.TransformRecord";
        Check(trace.TargetUndoFeedback(prop, true).Contains("Anarchy"), "Anarchy protection refusal is explained clearly");
        trace.Failure = "Unhandled nested component: Other.CustomData"; Check(trace.TargetUndoFeedback(prop, true).Contains("additional component data"), "unknown component data is not advertised recoverable");
        trace.Failure = null; trace.Busy = true; int count = trace.Captures;
        Check(trace.TargetUndoFeedback(prop, true).Contains("Wait") && trace.Captures == count, "in-flight recovery is never probed");
        trace.Busy = false; trace.Compatible = false; Check(trace.TargetUndoFeedback(prop, true).Contains("conflict") && trace.Captures == count, "patch conflict blocks eligibility preflight"); trace.Compatible = true;
        trace.moveDisabled = true; Check(trace.TargetUndoFeedback(prop, true).Contains("unavailable"), "disabled move hook cannot report successful eligibility"); trace.moveDisabled = false;
        Check(trace.TargetUndoFeedback(surface, true).Contains("preflight passed"), "area preflight uses area and companion capture");
        world.EntityManager.Put(Id(4), new Game.Net.Edge()); world.EntityManager.Put(Id(4), new PrefabRef());
        Check(trace.TargetUndoFeedback(Id(4), true).Contains("Standalone"), "standalone network not advertised recoverable");
        world.EntityManager.Put(Id(4), new Owner { m_Owner = Id(10) });
        Check(trace.TargetUndoFeedback(Id(4), true).Contains("move Undo is not supported"), "bound network deletion remains conditional and network move explicitly unsupported");
        world.EntityManager.Put(Id(5), new Game.Buildings.Building());
        Check(trace.TargetUndoFeedback(Id(5), true).Contains("not supported"), "whole buildings not advertised recoverable");
        Check(ReferenceEquals(trace.UndoSlot, slot), "all feedback outcomes preserve exact existing Undo entry");
        Check(world.EntityManager.Writes == writes + 4, "feedback itself performs no ECS writes, including failure paths");
    }
}
