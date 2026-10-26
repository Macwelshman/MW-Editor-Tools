import React, { useEffect, useRef, useState } from "react";
import { bindValue, trigger, useValue } from "cs2/api";
import { Button as GameButton, Scrollable, Tooltip } from "cs2/ui";
import { EditorButton, RadioOption } from "./native-panel";
import { color, paletteRows } from "./palette";
import { ColorPicker } from "./color-picker";
import "./window.scss";

type State = {
    available?: boolean; visible?: boolean; changed?: boolean; hasTarget?: boolean; hasColors?: boolean; canPaste?: boolean;
    canReset?: boolean; canUndo?: boolean; canApply?: boolean; confirmPaste?: boolean; append?: boolean; readOnly?: boolean;
    target?: string; source?: string; group?: string; status?: string; pairs?: string; channel2?: string; blockedReason?:string;
    targets?: string[]; targetIndex?: number; groupIndex?: number; groupCount?: number; paletteRevision?: number; selectionRevision?: number;
    page?: number; pages?: number; total?: number; generated?: number;
    preview?: { number: number; colors: string[] }[];
    previewRows?: string[];
    windowX?: number; windowY?: number;
};
const stateBinding = bindValue<string>("mwEditorToolsColors", "state", "{}");
const command = (action: string, extra = {}) => trigger("mwEditorToolsColors", "command", JSON.stringify({ action, ...extra }));
function Button({ className = "", ...props }: React.ComponentProps<typeof GameButton>) {
    return <EditorButton {...props} className={"mwet-colors-button " + className} />;
}
export function Swatch({ value, channel, showHex = true }: { value: string; channel: number; showHex?: boolean }) {
    const fill = color(value);
    return <div className="mwet-colors-color" title={fill ? value : "Invalid hex colour"}>
        <span className="mwet-colors-channel">{`Channel ${channel}`}</span>
        <span className="mwet-colors-swatch" style={{ backgroundColor: fill || "transparent" }}>{!fill && "!"}</span>
        {showHex && <span className="mwet-colors-value">{value}</span>}
    </div>;
}
function LoadedColors({ text, channel, onChange }: { text: string; channel: number; onChange: (text: string) => void }) {
    const rows = paletteRows(text);
    const [editing, setEditing] = useState<number | null>(null);
    const [anchor, setAnchor] = useState({ left: 0, right: 0, top: 0, bottom: 0 });
    const content = rows.length === 0 ? <p>No colours loaded.</p> : rows.map((row, i) => {
        const column = channel === 2 ? 0 : channel;
        const value = row[column] || "Missing value";
        return <div key={i}>
            <div className="mwet-colors-loaded-row"><span className="mwet-colors-number">{i + 1}</span>
                <button type="button" className="mwet-colors-edit-swatch" aria-label={"Edit channel " + channel + " colour " + (i + 1)} aria-expanded={editing === i}
                    title="Click to adjust colour" onClick={e => {
                        const rect = e.currentTarget.getBoundingClientRect();
                        setAnchor({ left: rect.left, right: rect.right, top: rect.top, bottom: rect.bottom });
                        setEditing(editing === i ? null : i);
                    }}><Swatch channel={channel} value={value} /></button>
            </div>
            {editing === i && <ColorPicker anchor={anchor} value={value} label={"Channel " + channel + " · Colour " + (i + 1)} onClose={() => setEditing(null)} onChange={next => {
                const updated = rows.map(r => [...r]); updated[i][column] = next;
                onChange(updated.map(r => r.join(" ")).join("\n"));
            }} />}
            {row.length !== (channel === 2 ? 1 : 2) && <span className="mwet-colors-warning">Expected {channel === 2 ? "one colour" : "two paired colours"} per line.</span>}
        </div>;
    });
    return rows.length>3
        ? <Scrollable vertical className="mwet-colors-loaded mwet-colors-loaded-scroll" trackVisibility="scrollable"><div>{content}</div></Scrollable>
        : <div className="mwet-colors-loaded mwet-colors-loaded-static">{content}</div>;
}
function ColorSection({ title, children, initiallyExpanded = true }: React.PropsWithChildren<{ title: string; initiallyExpanded?: boolean }>) {
    const [expanded, setExpanded] = useState(initiallyExpanded);
    return <section className={"mwet-colors-color-section" + (expanded ? " mwet-colors-section-expanded" : "")}>
        <button type="button" className="mwet-colors-section-toggle" aria-expanded={expanded} onClick={() => setExpanded(!expanded)}>
            <span className={"mwet-colors-collapse-arrow" + (expanded ? " mwet-colors-expanded" : "")} aria-hidden="true" />{title}
        </button>
        {expanded && <div className="mwet-colors-section-details">{children}</div>}
    </section>;
}
function InfoLines({ text, className = "" }: { text?: string; className?: string }) {
    return <div className={"mwet-colors-info " + className}>{(text || "").replace(/\bColor\b/g,"Colour").replace(/\bColors\b/g,"Colours").replace(/\bcolor\b/g,"colour").replace(/\bcolors\b/g,"colours").replace(/([.!?])\s+(?=[A-Z])/g, "$1\n").split("\n").filter(Boolean).map((line, i) => <p key={i}>{line}</p>)}</div>;
}
// Keep palette drafts when switching away from Colours or closing the tools window.
let savedDraft: { revision?: number; pairs: string; channel2: string; append: boolean } | undefined;
export function ColourVariationsContent() {
    const raw = useValue(stateBinding);
    const s: State = JSON.parse(raw || "{}");
    const [pairs, setPairs] = useState(savedDraft && savedDraft.revision === s.paletteRevision ? savedDraft.pairs : s.pairs || "");
    const [channel2, setChannel2] = useState(savedDraft && savedDraft.revision === s.paletteRevision ? savedDraft.channel2 : s.channel2 || "");
    const [append, setAppend] = useState(savedDraft && savedDraft.revision === s.paletteRevision ? savedDraft.append : !!s.append);
        useEffect(() => {
        const draft = savedDraft && savedDraft.revision === s.paletteRevision ? savedDraft : undefined;
        setPairs(draft?.pairs ?? s.pairs ?? ""); setChannel2(draft?.channel2 ?? s.channel2 ?? ""); setAppend(draft?.append ?? !!s.append);
    },
        [s.pairs, s.channel2, s.append, s.target, s.paletteRevision]);
    useEffect(() => { command("open"); return () => command("close"); }, [s.available]);

    const dirty = pairs !== (s.pairs || "") || channel2 !== (s.channel2 || "") || append !== !!s.append;
    const editDraft = (nextPairs: string, nextChannel2: string, nextAppend: boolean) => {
        savedDraft = { revision: s.paletteRevision, pairs: nextPairs, channel2: nextChannel2, append: nextAppend };
        setPairs(nextPairs); setChannel2(nextChannel2); setAppend(nextAppend);
    };
    const [pendingMesh, setPendingMesh] = useState<number | null>(null);
    const followed = useRef<number | undefined>();
    const hasDraft = dirty || (s.generated || 0) > 0;
    useEffect(() => {
        if (!s.available || !s.visible || !s.changed || hasDraft || s.blockedReason || followed.current === s.selectionRevision) return;
        followed.current = s.selectionRevision;
        command("follow", { index: s.selectionRevision });
    }, [s.available, s.visible, s.changed, s.selectionRevision, hasDraft, s.blockedReason]);
    if (!s.available) return <p>Waiting for Colour Variations…</p>;
    const blocked = !!s.changed || !!s.blockedReason;
    const preview = s.hasColors && (s.generated || 0) > 0 ? (
        <section className="mwet-colors-combination-summary">
            <p>{`${s.generated}\u00a0${s.generated===1?"combination":"combinations"} generated from the selected colours.`}</p>
            <p>{`${s.total}\u00a0total ${s.total===1?"variation":"variations"} after applying.`}</p>
            {dirty && <p className="mwet-colors-warning">Colours changed. Generate combinations again before applying.</p>}
        </section>
    ) : null;
    const footer = <div className="mwet-colors-footer">
        {preview}
        <div className="mwet-colors-actions mwet-colors-footer-actions">
            <Button className="mwet-colors-apply" variant="primary" disabled={!s.canApply || dirty} onSelect={() => command("apply")}>Apply combinations</Button>
            <Button className="mwet-colors-undo" disabled={blocked || !s.canUndo} onSelect={() => command("undo")}>Undo last edit</Button>
        </div>
    </div>;
    return <div className="mwet-colors-native-window mwet-colors-integrated">
        <div className="mwet-colors-window-layout">
        {s.blockedReason&&<p role="status">{s.blockedReason}</p>}
        <div className="mwet-colors-toolbar">
            {s.changed && hasDraft ? <div className="mwet-colors-confirm" role="alert">
                <p>There are uncommitted palette edits for {s.target}. Return to that item to keep editing, or discard them to use the newly selected item.</p>
                <Button disabled={!!s.blockedReason} onSelect={() => { followed.current = s.selectionRevision; command("follow", { index: s.selectionRevision }); }}>Discard edits and follow selection</Button>
            </div> : null}
        </div>
        <div className="mwet-colors-body mwet-colors-fixed-controls">
            <div className="mwet-colors-body-inner">
            {s.hasTarget&&<h3>Target mesh</h3>}
            {s.hasTarget && <p className="mwet-colors-target-name">{s.target || s.targets?.[s.targetIndex || 0]}</p>}
            {(s.targets || []).length > 1 && <label className="mwet-colors-target-choice">Choose mesh <select aria-label="Choose target mesh" value={s.targetIndex || 0} onChange={e => { const index = Number(e.currentTarget.value); if (hasDraft) setPendingMesh(index); else command("target", { index }); }}>{s.targets!.map((t,i) => <option key={i} value={i}>{t}</option>)}</select></label>}
            {pendingMesh !== null && <div className="mwet-colors-confirm" role="alert">
                <p>Discard uncommitted palette edits and switch mesh?</p>
                <Button disabled={blocked} onSelect={() => { command("target", { index: pendingMesh }); setPendingMesh(null); }}>Discard edits and switch mesh</Button>
                <Button onSelect={() => setPendingMesh(null)}>Keep editing</Button>
            </div>}
            {!s.hasTarget && <p>Select an object or mesh in Object Info.</p>}
            {s.hasTarget && <>
                <h3>Copy Colour Properties</h3>
                <p>{s.source ? "Copied from: " + s.source : ""}</p>
                <div className="mwet-colors-actions mwet-colors-copy-actions"><Button disabled={blocked || !s.hasColors} onSelect={() => command("copy")}>Copy colour settings</Button>
                    <Tooltip tooltip="Apply the copied Colour Properties to this mesh immediately. Reset can restore its previous settings."><div><Button disabled={blocked || !s.canPaste} onSelect={() => command("paste")}>Paste colour settings</Button></div></Tooltip>
                    <Tooltip tooltip="Restore this mesh's colour settings from before its last paste. Later edits prevent reset."><div><Button disabled={blocked || !s.canReset} onSelect={() => command("reset")}>Reset</Button></div></Tooltip></div>
                {!s.hasColors && <p>Paste will add Colour Properties to this mesh.</p>}

            </>}
            {s.hasColors && <>
                <h3>Variation group</h3><p>{s.group}</p>
                {(s.groupCount || 0) > 1 && <div className="mwet-colors-actions">
                    <Button disabled={blocked} onSelect={() => command("group", { index: ((s.groupIndex || 0) + s.groupCount! - 1) % s.groupCount! })}>Previous group</Button>
                    <Button disabled={blocked} onSelect={() => command("group", { index: ((s.groupIndex || 0) + 1) % s.groupCount! })}>Next group</Button></div>}
                <Tooltip tooltip="Reloads colours from the selected asset's current variation group into the channel lists. Replaces unsaved colour edits and clears generated combinations. Does not change the asset.">
                    <div><Button className="mwet-colors-wide" disabled={blocked} onSelect={() => command("load")}>Reload colours from asset</Button></div>
                </Tooltip>
                <InfoLines text="Channels 0 and 1 stay paired by row. Channel 2 combines with every pair. Click a swatch to adjust its colour." />
                <div className="mwet-colors-channel-sections">{[0, 1, 2].map(channel => <ColorSection key={channel} title={"Channel " + channel}>
                    <LoadedColors channel={channel} text={channel === 2 ? channel2 : pairs} onChange={value => channel === 2 ? editDraft(pairs, value, append) : editDraft(value, channel2, append)} />
                </ColorSection>)}</div>
                <Button variant="primary" className="mwet-colors-wide mwet-colors-preview-button" disabled={blocked} onSelect={() => command("preview", { pairs, channel2, append })}>Generate combinations</Button>
                <div className="mwet-colors-group-mode" role="radiogroup" aria-label="Group mode">
                    <span className="mwet-colors-group-mode-label">Group mode</span>
                    <div className="mwet-colors-group-mode-options">
                        <RadioOption label="Replace group" checked={!append} onSelect={() => editDraft(pairs, channel2, false)} />
                        <RadioOption label="Append to group" checked={append} onSelect={() => editDraft(pairs, channel2, true)} />
                    </div>
                </div>
                <InfoLines className="mwet-colors-status" text={s.status} />
                {dirty && <InfoLines className="mwet-colors-status" text="Palette changed. Generate combinations before applying." />}
                {s.readOnly && <InfoLines className="mwet-colors-status" text="This asset is read-only. Make an editable copy to apply changes." />}
            </>}
            {s.hasTarget&&!s.hasColors && <InfoLines className="mwet-colors-status" text={s.status} />}
            </div>
        </div>
        </div>
        {s.hasTarget&&footer}</div>;
}







