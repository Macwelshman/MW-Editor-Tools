using Game.UI.Widgets;
using Game.UI.Localization;
using Game.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using Game;
using Game.Modding;
using Game.Input;
using Game.Prefabs;
using Game.Rendering;
using Game.SceneFlow;
using Game.Tools;
using Game.UI.Editor;
using Unity.Entities;
using UnityEngine;

using Object = UnityEngine.Object;
using Debug = UnityEngine.Debug;

namespace MW.EditorTools.Modules.ColourVariations
{
    public sealed partial class PaletteWindow : MonoBehaviour
    {
        sealed class Target
        {
            public PrefabBase Mesh, Root;
            public ColorProperties Colors;
        }

        bool append;

        string status = "Select a mesh in Object Info, then Get Selected Asset.";
        string pairs = "", channel2 = "";
        List<Target> targets = new List<Target>();
        Target target;
        object capturedSelection;
        World capturedWorld;
        World undoWorld;

        int groupIndex;
        int paletteRevision;
        List<ColorProperties.VariationSet> staged, undo;
        string previewState, undoState;
        Target undoTarget;
        PropertiesCopy copiedProperties;
        string copiedFrom, pendingPasteState, undoProperties;
        Target pendingPasteTarget;
        bool undoPaste, undoCreatedComponent;

        [Serializable]
        public sealed class PropertiesCopy
        {
            public bool active;
            public List<ColorProperties.VariationSet> variations;
            public List<ColorProperties.ColorChannelBinding> bindings;
            public List<ColorProperties.VariationGroup> groups;
            public Unity.Mathematics.int3 variationRanges, alphaRanges;
            public ColorSourceType externalSource;
            public PropertiesCopy DeepClone()
            {
                return new PropertiesCopy {
                    active = active, variationRanges = variationRanges, alphaRanges = alphaRanges, externalSource = externalSource,
                    variations = variations == null ? null : variations.Select(v => v == null ? null : new ColorProperties.VariationSet {
                        m_Colors = v.m_Colors == null ? null : (Color[])v.m_Colors.Clone(), m_VariationGroup = v.m_VariationGroup }).ToList(),
                    bindings = bindings == null ? null : bindings.Select(b => b == null ? null : new ColorProperties.ColorChannelBinding {
                        m_ChannelId = b.m_ChannelId, m_CanBeModifiedByExternal = b.m_CanBeModifiedByExternal }).ToList(),
                    groups = groups == null ? null : groups.Select(g => g == null ? null : new ColorProperties.VariationGroup {
                        m_Name = g.m_Name, m_Probability = g.m_Probability, m_MeshSyncMode = g.m_MeshSyncMode,
                        m_OverrideRandomness = g.m_OverrideRandomness, m_VariationRanges = g.m_VariationRanges, m_AlphaRanges = g.m_AlphaRanges }).ToList()
                };
            }
            public static PropertiesCopy Capture(ColorProperties c)
            {
                return new PropertiesCopy { active = c.active,
                    variations = c.m_ColorVariations, bindings = c.m_ChannelsBinding,
                    groups = c.m_VariationGroups, variationRanges = c.m_VariationRanges,
                    alphaRanges = c.m_AlphaRanges, externalSource = c.m_ExternalColorSource }.DeepClone();
            }
            public static void Paste(PropertiesCopy source, ColorProperties c)
            {
                // Clone afresh for each destination: no shared lists or arrays.
                var data = source.DeepClone();
                c.active = data.active;
                c.m_ColorVariations = data.variations;
                c.m_ChannelsBinding = data.bindings;
                c.m_VariationGroups = data.groups;
                c.m_VariationRanges = data.variationRanges;
                c.m_AlphaRanges = data.alphaRanges;
                c.m_ExternalColorSource = data.externalSource;
            }
        }
        int generated;
        const int PreviewPageSize = 3;
        int previewPage;

        EditorSection launcherSection;
        IWidget launcher;
        EditorPanelUISystem Panels { get { return CurrentWorld == null || !CurrentWorld.IsCreated ? null : CurrentWorld.GetExistingSystemManaged<EditorPanelUISystem>(); } }
        bool InEditor { get { return GameManager.instance != null && (GameManager.instance.gameMode & GameMode.Editor) != 0; } }
        bool EditorReady { get { return InEditor && !GameManager.instance.isGameLoading && CurrentWorld != null && CurrentWorld.IsCreated && Panels != null; } }
        World CurrentWorld { get { return World.DefaultGameObjectInjectionWorld; } }
        InspectorPanelSystem Inspector { get { return CurrentWorld.GetExistingSystemManaged<InspectorPanelSystem>(); } }
        void Update()
        {
            if (!EditorReady) { ReleaseWindowInput(); DetachLauncher(); target = null; staged = null; undo = null; targets.Clear(); pendingPasteTarget = null; capturedSelection = null; capturedWorld = null; visible = false; return; }
            if (CurrentWorld == null || !CurrentWorld.IsCreated) return;
            if (capturedWorld != null && capturedWorld != CurrentWorld) { DetachLauncher(); target = null; staged = null; undo = null; targets.Clear(); capturedWorld = null; visible = false; }
            if (target != null && target.Mesh == null) { target = null; staged = null; targets.Clear();  }
            UpdateWindowInput();

        }
        void DetachLauncher()
        {
            if (launcherSection != null && launcher != null)
                launcherSection.children = launcherSection.children.Where(x => !ReferenceEquals(x, launcher)).ToList();
            launcherSection = null; launcher = null;
        }
        void OpenPanel()
        {
            visible = true;
            if (target == null) Run(Capture);
        }
        void ClosePanel() { visible = false; ReleaseWindowInput(); }
        void OnDestroy() { ClosePanel(); DetachLauncher(); DisposeWindowStyle(); }
        void OnDisable() { ClosePanel(); DetachLauncher(); }
        void Run(Action action)
        {
            try { action(); }
            catch (Exception ex) { status = ex.Message; Debug.LogWarning("MW Colors: " + ex); }
            
        }
        Button MakeButton(string label, string key, Action action, Func<bool> disabled = null)
        {
            return new Button { displayName = LocalizedString.Value(label), path = key,
                action = delegate { Run(action); }, disabled = disabled, shouldTriggerValueChangedEvent = false };
        }
        void InvalidatePreview()
        {
            staged = null; previewPage = 0;
            status = "Colours changed. Generate combinations before applying.";
        }
        void Capture()
        {
            if (!EditorReady) throw new InvalidOperationException("Wait for editor loading to complete.");
            if (CurrentWorld == null || !CurrentWorld.IsCreated) throw new InvalidOperationException("Editor world is not available yet.");
            InspectorPanelSystem inspector = Inspector;
            var panels = CurrentWorld.GetExistingSystemManaged<EditorPanelUISystem>();
            if (inspector == null || panels == null || !ReferenceEquals(panels.activePanel, inspector))
                throw new InvalidOperationException("Select an object or mesh so its Object Info panel is active first.");
            capturedWorld = CurrentWorld;
            capturedSelection = inspector.selectedObject;
            paletteRevision++; targets.Clear(); target = null; staged = null; pendingPasteTarget = null;
            PrefabBase selected = null;
            var info = capturedSelection as ObjectMeshInfo;
            if (info != null) selected = info.m_Mesh;
            else if (capturedSelection is PrefabBase) selected = (PrefabBase)capturedSelection;
            else if (capturedSelection is ComponentBase) selected = ((ComponentBase)capturedSelection).prefab;
            else if (capturedSelection is ObjectSubObjectInfo) selected = ((ObjectSubObjectInfo)capturedSelection).m_Object;
            PrefabBase root = FindOwner(selected);
            AddTarget(selected, root);
            var geometry = selected as ObjectGeometryPrefab;
            if (geometry != null && geometry.m_Meshes != null)
                foreach (ObjectMeshInfo mesh in geometry.m_Meshes) if (mesh != null) AddTarget(mesh.m_Mesh, selected);
            if (targets.Count == 0) throw new InvalidOperationException("No compatible mesh found. Select a render mesh in Object Info.");
            Choose(targets[0]);
        }
        PrefabBase FindOwner(PrefabBase selected)
        {
            var tools = CurrentWorld.GetExistingSystemManaged<ToolSystem>();
            var prefabs = CurrentWorld.GetExistingSystemManaged<PrefabSystem>();
            if (tools == null || prefabs == null || tools.selected == Entity.Null) return null;
            EntityManager em = CurrentWorld.EntityManager;
            if (!em.Exists(tools.selected) || !em.HasComponent<PrefabRef>(tools.selected)) return null;
            PrefabBase root;
            if (!prefabs.TryGetPrefab<PrefabBase>(em.GetComponentData<PrefabRef>(tools.selected), out root) || root == null) return null;
            var geometry = root as ObjectGeometryPrefab;
            if (root == selected || (geometry != null && geometry.m_Meshes != null && geometry.m_Meshes.Any(m => m != null && m.m_Mesh == selected))) return root;
            return null;
        }
        void AddTarget(PrefabBase mesh, PrefabBase root)
        {
            ColorProperties c;
            if (mesh == null || targets.Any(t => t.Mesh == mesh)) return;
            mesh.TryGet<ColorProperties>(out c);
            if (c != null || mesh is RenderPrefab || mesh is CharacterOverlay)
                targets.Add(new Target { Mesh = mesh, Root = root, Colors = c });
        }
        void Choose(Target t)
        {
            ValidateTarget(t, false); paletteRevision++; target = t; groupIndex = 0; staged = null; pendingPasteTarget = null;
            if (t.Colors == null) { pairs = ""; channel2 = ""; status = "This mesh has no Colour Properties. Paste a copied setup to create it."; }
            else { try { LoadPalette(); } catch (Exception ex) { status = ex.Message; } }
        }
        string Group()
        {
            var groups = target.Colors.m_VariationGroups;
            if (groups == null || groups.Count == 0) return null;
            if (groupIndex >= groups.Count) groupIndex = 0;
            return groups[groupIndex].m_Name;
        }
        static int[] Bindings(ColorProperties c)
        {
            if (c.m_ChannelsBinding == null || c.m_ChannelsBinding.Count != 3 || c.m_ChannelsBinding.Any(x => x == null || x.m_ChannelId < 0 || x.m_ChannelId > 2))
                throw new InvalidOperationException("This asset needs three valid channel bindings (0–2).");
            return c.m_ChannelsBinding.Select(x => (int)x.m_ChannelId).ToArray();
        }
        static void ValidateTarget(Target t, bool write)
        {
            if (t == null || t.Mesh == null) throw new InvalidOperationException("The captured mesh is no longer available.");
            ColorProperties current;
            t.Mesh.TryGet<ColorProperties>(out current);
            if (current != t.Colors) throw new InvalidOperationException("ColorProperties changed. Capture the asset again.");
            if (write && (t.Mesh.isReadOnly || (t.Root != null && t.Root.isReadOnly))) throw new InvalidOperationException("This asset is read-only. Make an editable copy in the editor first.");
        }
        static void Validate(Target t, bool write = true, bool requireActive = true)
        {
            ValidateTarget(t, write);
            if (t.Colors == null) throw new InvalidOperationException("This mesh has no Colour Properties yet.");
            if (requireActive && !t.Colors.active) throw new InvalidOperationException("Enable this mesh's Colour Properties component first.");
            Bindings(t.Colors);
            var names = new HashSet<string>(StringComparer.Ordinal);
            if (t.Colors.m_VariationGroups != null)
                foreach (var group in t.Colors.m_VariationGroups)
                    if (group == null || string.IsNullOrEmpty(group.m_Name) || !names.Add(group.m_Name))
                        throw new InvalidOperationException("Variation groups must have unique, nonempty names.");
            if (t.Colors.m_ColorVariations == null) throw new InvalidOperationException("The variation list is missing.");
            foreach (var row in t.Colors.m_ColorVariations)
            {
                if (row == null || row.m_Colors == null || row.m_Colors.Length != 3) throw new InvalidOperationException("An existing variation has an invalid colour array.");
                if (!string.IsNullOrEmpty(row.m_VariationGroup) && !names.Contains(row.m_VariationGroup)) throw new InvalidOperationException("An existing variation references a missing group.");
                if (names.Count > 0 && string.IsNullOrEmpty(row.m_VariationGroup)) throw new InvalidOperationException("Assign the ungrouped variations to a group in the inspector first.");
            }
        }
        void CheckSelection(bool requireColors = true, bool write = true)
        {
            if (CurrentWorld != capturedWorld || Inspector == null || !ReferenceEquals(Inspector.selectedObject, capturedSelection))
                throw new InvalidOperationException("Selection changed. Return to the previous item or discard the palette edits to follow the new selection.");
            var panels = CurrentWorld.GetExistingSystemManaged<EditorPanelUISystem>();
            if (panels == null || (!ReferenceEquals(panels.activePanel, Inspector))) throw new InvalidOperationException("Return to the captured Object Info selection first.");
            if (requireColors) Validate(target, write, write);
            else ValidateTarget(target, write);
        }
        void LoadPalette()
        {
            Validate(target, false, false);
            int[] map = Bindings(target.Colors);
            var ps = new List<string>(); var ws = new List<string>();
            var seenPairs = new HashSet<string>(StringComparer.Ordinal);
            var seenChannel2 = new HashSet<string>(StringComparer.Ordinal);
            foreach (var row in target.Colors.m_ColorVariations.Where(x => SameGroup(x.m_VariationGroup, Group())))
            {
                string p = ToRgba(row.m_Colors[map[0]]).Hex() + " " + ToRgba(row.m_Colors[map[1]]).Hex();
                string w = ToRgba(row.m_Colors[map[2]]).Hex();
                if (seenPairs.Add(p)) ps.Add(p); if (seenChannel2.Add(w)) ws.Add(w);
            }
            paletteRevision++;
            pairs = string.Join("\n", ps.ToArray()); channel2 = string.Join("\n", ws.ToArray()); staged = null;
            status = "Loaded Channel 0 / Channel 1 pairs and Channel 2 colours from this group.";
        }
        static bool SameGroup(string a, string b) { return (a ?? "") == (b ?? ""); }
        void Preview()
        {
            staged = null;
            previewPage = 0;
            CheckSelection(true, false);
            var rows = Palette.Generate(pairs, channel2, Bindings(target.Colors));
            staged = Clone(target.Colors.m_ColorVariations.Where(x => append || !SameGroup(x.m_VariationGroup, Group())).ToList());
            foreach (var row in rows) staged.Add(new ColorProperties.VariationSet { m_Colors = row.Select(ToColor).ToArray(), m_VariationGroup = Group() });
            if (staged.Count > Palette.Limit) { staged = null; throw new InvalidOperationException("Result exceeds the 2,048-entry limit for this version."); }
            generated = rows.Count; previewState = State(target);
            status = "Combinations ready. Click Apply combinations to update this asset.";
        }
        static List<ColorProperties.VariationSet> Clone(List<ColorProperties.VariationSet> rows)
        {
            return rows.Select(x => new ColorProperties.VariationSet { m_Colors = (Color[])x.m_Colors.Clone(), m_VariationGroup = x.m_VariationGroup }).ToList();
        }
        static string State(Target t) { return t.Colors == null ? "<no-color-properties>" : JsonUtility.ToJson(t.Colors); }
        void CopyProperties()
        {
            CheckSelection(true, false);
            copiedProperties = PropertiesCopy.Capture(target.Colors);
            copiedFrom = target.Mesh.name;
            pendingPasteTarget = null;
            status = "Copied all Colour Properties from " + copiedFrom + ". Select the destination mesh, then click Paste colour settings. The copy remains available for repeated pastes.";
        }
        void PreparePaste()
        {
            CheckSelection(false);
            if (copiedProperties == null) throw new InvalidOperationException("Copy Colour Properties from a source mesh first.");
            if (target.Colors == null && !(target.Mesh is RenderPrefab) && !(target.Mesh is CharacterOverlay)) throw new InvalidOperationException("Colour Properties cannot be added to this prefab type.");
            pendingPasteTarget = target; pendingPasteState = State(target);
        }
        void PasteProperties()
        {
            CheckSelection(false);
            if (!ReferenceEquals(pendingPasteTarget, target) || pendingPasteState != State(target))
            { pendingPasteTarget = null; throw new InvalidOperationException("Destination changed. Review Paste again."); }
            bool created = target.Colors == null;
            string before = State(target);
            try
            {
                if (created) target.Colors = target.Mesh.AddComponent<ColorProperties>();
                PropertiesCopy.Paste(copiedProperties, target.Colors);
                Validate(target, true, false);
                Refresh(target);
            }
            catch
            {
                if (created && target.Colors != null) { target.Mesh.Remove<ColorProperties>(); target.Colors = null; }
                else if (!created) JsonUtility.FromJsonOverwrite(before, target.Colors);
                Refresh(target); throw;
            }
            undo = new List<ColorProperties.VariationSet>(); undoPaste = true;
            undoProperties = before; undoCreatedComponent = created;
            undoTarget = target; undoWorld = CurrentWorld; undoState = State(target);
            pendingPasteTarget = null; staged = null; groupIndex = 0;
            if (target.Colors.active) LoadPalette();
            status = "Pasted all Colour Properties to " + target.Mesh.name + (created ? " and added the component." : ".") + " Undo will " + (created ? "remove the added component." : "restore the previous settings.");
        }
        void Apply()
        {
            CheckSelection();
            if (staged == null || State(target) != previewState) { staged = null; throw new InvalidOperationException("Asset settings changed. Preview again before applying."); }
            var previous = Clone(target.Colors.m_ColorVariations);
            target.Colors.m_ColorVariations = Clone(staged);
            try { Refresh(target); }
            catch { target.Colors.m_ColorVariations = previous; Refresh(target); throw; }
            undo = previous; undoPaste = false; undoTarget = target; undoWorld = CurrentWorld; undoState = State(target); staged = null;
            status = "Applied " + generated + " combinations. Reselect the mesh if the inspector list is stale. MW Undo is available.";
        }
        void Undo()
        {
            if (undo == null || CurrentWorld != undoWorld) throw new InvalidOperationException("No undo is available in this editor session.");
            ValidateTarget(undoTarget, true);
            if (State(undoTarget) != undoState) throw new InvalidOperationException("The asset was edited after the MW operation. Undo stopped to preserve those edits.");
            if (undoPaste)
            {
                ColorProperties component = undoTarget.Colors;
                string after = State(undoTarget);
                try
                {
                    if (undoCreatedComponent) { undoTarget.Mesh.Remove<ColorProperties>(); undoTarget.Colors = null; }
                    else JsonUtility.FromJsonOverwrite(undoProperties, undoTarget.Colors);
                    Refresh(undoTarget);
                }
                catch
                {
                    if (undoCreatedComponent) { undoTarget.Mesh.components.Add(component); undoTarget.Mesh.isDirty = true; undoTarget.Colors = component; }
                    else JsonUtility.FromJsonOverwrite(after, undoTarget.Colors);
                    Refresh(undoTarget); throw;
                }
                if (target != null && target.Mesh == undoTarget.Mesh) target.Colors = undoTarget.Colors;
                undo = null; staged = null; pendingPasteTarget = null;
                status = "Paste undone. " + (undoCreatedComponent ? "The added Colour Properties component was removed." : "The previous Colour Properties were restored.");
                return;
            }
            var current = undoTarget.Colors.m_ColorVariations;
            undoTarget.Colors.m_ColorVariations = Clone(undo);
            try { Refresh(undoTarget); }
            catch { undoTarget.Colors.m_ColorVariations = current; Refresh(undoTarget); throw; }
            undo = null; staged = null; status = "Previous variations restored.";
        }
        void Refresh(Target t)
        {
            var prefabs = CurrentWorld.GetExistingSystemManaged<PrefabSystem>();
            if (prefabs == null) throw new InvalidOperationException("Prefab system unavailable.");
            Entity entity;
            if (!prefabs.TryGetEntity(t.Mesh, out entity)) throw new InvalidOperationException("Mesh is no longer registered. Capture the selection again.");
            var errors = CurrentWorld.GetExistingSystemManaged<EditorErrorPanelSystem>();
            if (errors != null) errors.OnPrefabModified();
            var meshColors = CurrentWorld.GetExistingSystemManaged<MeshColorSystem>();
            if (meshColors != null) meshColors.SetOverride(Entity.Null, null, -1);
            prefabs.UpdatePrefab(t.Mesh);
            if (t.Mesh.asset != null) t.Mesh.asset.MarkDirty();
            if (t.Root != null && t.Root != t.Mesh)
            {
                prefabs.UpdatePrefab(t.Root);
                if (t.Root.asset != null) t.Root.asset.MarkDirty();
            }
        }
        static Rgba ToRgba(Color c) { return new Rgba(c.r, c.g, c.b, c.a); }
        static Color ToColor(Rgba c) { return new Color(c.R, c.G, c.B, c.A); }
    }
}






