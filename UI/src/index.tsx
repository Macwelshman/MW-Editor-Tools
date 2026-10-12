import React, {useEffect, useState} from "react";
import {bindValue, trigger, useValue} from "cs2/api";
import {ModRegistrar, getModule} from "cs2/modding";
import {Button, Tooltip} from "cs2/ui";
import {NativePanel} from "./native-panel";
import {MultiSaveContent} from "./multi-save";
import {ColourVariationsContent} from "./colours/window";
import "./style.scss";
type Options = {confirmation:boolean; recovery:boolean; shortcuts:boolean; inspectorControls:boolean; historyDepth:number; undoKey:string; redoKey:string; undoModifiers:number; redoModifiers:number; windowX:number; windowY:number};
type State = {available:boolean;visible:boolean;asset:string;status:string;bulldozerDiagnostic:string;undo:number;redo:number;undoTarget:string;redoTarget:string;canArmMainTest:boolean;canArmBulldozerTest:boolean;nestedHistoryEnabled:boolean;mainHistoryEnabled:boolean;canUndo:boolean;canRedo:boolean;recent:string[];positionRevision:number;settings:Options;protectedComponents:boolean;installedRecovery:boolean};
const binding = bindValue<string>("mwEditorTools","state","{}");
const width = bindValue<number>("editorHierarchy","width",500);
const command = (value:string) => trigger("mwEditorTools","command",value);
const shortcutLabel = (mask:number,key:string) => [mask&1?"Ctrl":"",mask&2?"Shift":"",mask&4?"Alt":"",mask&8?"Windows":"",key].filter(Boolean).join("+");
const tabs = ["History & Recovery","Prop Replacer","Components","Colour Variations","Multi-Save"];
function Launcher() {
    const raw = useValue(binding), workspaceWidth = useValue(width);
    let state: Partial<State>;
    try { state = JSON.parse(raw || "{}"); } catch { return null; }
    if (!state.available) return null;
    return <div className="mw-tools-launcher" style={{position:"fixed",top:"130rem",left:`${workspaceWidth+20}rem`,zIndex:100,pointerEvents:"auto"}}>
        <Tooltip tooltip="MW Editor Tools"><Button className="mw-tools-launcher-button" focusKey="mwEditorTools.launcher"
            aria-label="MW Editor Tools" selected={state.visible}
            style={{display:"block",width:"56rem",height:"56rem",padding:0,border:0,backgroundColor:"#263b47",color:"#fff"}}
            onSelect={()=>command(state.visible ? "close" : "open")}>
            <svg width="100%" height="100%" viewBox="0 0 56 56" aria-hidden="true">
                <rect x="1" y="1" width="54" height="54" rx="6" fill="#263b47" stroke="#7ca4ba" strokeWidth="2"/>
                <path d="M14 16h28M14 28h28M14 40h28" stroke="#fff" strokeWidth="3"/>
                <path d="M22 11v10M35 23v10M24 35v10" stroke="#61d7ea" strokeWidth="6"/>
            </svg>
        </Button></Tooltip>
    </div>;
}
function Tools() {
    const raw = useValue(binding), workspaceWidth = useValue(width);
    const [tab,setTab] = useState(tabs[0]);
    const [diagnostics,setDiagnostics] = useState(false);

    const state = JSON.parse(raw) as State;
    useEffect(() => {
        const timer = setTimeout(() => {
            const element = document.querySelector<HTMLElement>(".mw-tools-launcher");
            const rect = element?.getBoundingClientRect();
            const style = element ? window.getComputedStyle(element) : null;
            trigger("mwEditorTools","frontendDiagnostic",JSON.stringify({available:state.available,settings:!!state.settings,
                workspaceWidth,launcher:!!element,left:rect?.left,top:rect?.top,width:rect?.width,height:rect?.height,
                position:style?.position,display:style?.display,visibility:style?.visibility}));
        },250);
        return () => clearTimeout(timer);
    },[state.available,!!state.settings,workspaceWidth]);
    useEffect(() => {
        const report = () => {
            const active = document.activeElement as HTMLElement|null;
            const typing = !!active && (active.tagName === "INPUT" || active.tagName === "TEXTAREA" || active.tagName === "SELECT" ||
                active.isContentEditable || !!active.closest('[contenteditable="true"],[role="textbox"],[role="combobox"]')) || !!document.querySelector('.mwet-colors-picker-overlay');
            trigger("mwEditorTools","typing",typing);
        };
        report(); const timer = setInterval(report,100);
        document.addEventListener("focusin",report); document.addEventListener("focusout",report);
        return () => {clearInterval(timer); document.removeEventListener("focusin",report); document.removeEventListener("focusout",report); trigger("mwEditorTools","typing",true);};
    },[]);
    if(!state.available || !state.settings) return null;
    const theme = getModule("game-ui/editor/themes/editor-button.module.scss","classes");
    const buttonClasses = getModule("game-ui/editor/widgets/button/button.module.scss","classes");
    const button = (label:string,action:()=>void,disabled=false) => <Button className={buttonClasses.button} theme={theme} onSelect={action} disabled={disabled}>{label}</Button>;
    return <>
        {state.visible && <NativePanel key={state.positionRevision} className="mw-tools-panel"
            initialPosition={{x:state.settings.windowX,y:state.settings.windowY}}
            onPositionChange={position=>trigger("mwEditorTools","windowPosition",JSON.stringify({...position,revision:state.positionRevision}))}
            header={<div className="mw-tools-heading"><span>MW Editor Tools</span>{button("Close",()=>command("close"))}</div>}>
            <div className="mw-tools-body">
                <div className="mw-tools-tabs" role="tablist" aria-label="Editor tools">{tabs.map((name,index)=><Button key={name} id={`mw-tools-tab-${index}`} role="tab" aria-selected={tab===name} aria-controls="mw-tools-section" theme={theme} selected={tab===name} onSelect={()=>setTab(name)}>{index===0?"History":index===3?"Colours":name}</Button>)}</div>
                <div className="mw-tools-section" id="mw-tools-section" role="tabpanel" aria-labelledby={`mw-tools-tab-${tabs.indexOf(tab)}`}>
                {tab===tabs[0] && <>
                    <div className="mw-tools-context">{state.asset}</div>
                    <div className="mw-tools-actions">
                        {button(`Undo (${state.undo})`,()=>command("undo"),!state.canUndo)}
                        {button(`Redo (${state.redo})`,()=>command("redo"),!state.canRedo)}
                        {button("Clear history",()=>command("clear"),state.undo+state.redo===0)}
                    </div>
                    {state.undo>0 && state.undoTarget && <div>Next Undo: {state.undoTarget}</div>}
                    {state.redo>0 && state.redoTarget && <div>Next Redo: {state.redoTarget}</div>}
                    <div className="mw-tools-test">
                        <strong>Bulldozer Undo · experimental</strong>
                        <div className="mw-tools-test-status" role="status" aria-live="polite">{state.bulldozerDiagnostic || "Select a disposable asset copy."}</div>
                        {button(state.nestedHistoryEnabled?"Prop Undo enabled":"Enable prop Undo",()=>command("enableNestedHistory"),state.nestedHistoryEnabled||!state.canArmBulldozerTest)}
                        <p>Enable for the selected asset, then Everything-delete a supported prop or nested group and use Undo or Redo above.</p>
                        {button(state.mainHistoryEnabled?"Standalone prop Undo enabled":"Enable standalone prop Undo",()=>command("armMainTest"),state.mainHistoryEnabled||!state.canArmMainTest)}
                        <p>Supported standalone props are captured automatically in Main or Everything mode when recovery is on. The button also enables this mode manually. Switching between asset and standalone recovery clears previous history.</p>
                        <p>Areas and surfaces are captured automatically on an area-only asset. Mixed areas with props, networks, lanes or effects are not supported yet.</p>
                    <strong>Use a disposable copy. Do not save these edits.</strong>
                    </div>
                    <p>{state.status}</p>
                    {button(diagnostics?"Hide diagnostic tools":"Show diagnostic tools",()=>setDiagnostics(!diagnostics))}
                    {diagnostics && <div className="mw-tools-diagnostics">
                        {button("Check full prop history",()=>command("startFullPropTest"),!state.canArmBulldozerTest)}
                        <p>Checks the complete enabled prop set. Wait for “Test ready” and follow the number of deletions shown there.</p>
                        {button("Check two prop deletions",()=>command("startNestedBatchTest"),!state.canArmBulldozerTest)}
                        <p>Start the check on a saved test copy, wait for “Test ready”, Everything-delete two supported props or groups, then wait for Passed or Stopped.</p>
                        <p>Automatic nested test: wait for “Test ready”, then Everything-delete one group. Undo → Redo → Undo runs automatically.</p>
                        {button("Start test",()=>command("startNestedRecoveryTest"))}
                        <p>Older manual test controls</p>
                        {button("Arm Sub-element history test",()=>command("armBulldozerTest"),!state.canArmBulldozerTest)}
                        {button("Arm Main prop history test",()=>command("armMainTest"),!state.canArmMainTest)}
                        {button("Arm last bound prop test",()=>command("armLastPropTest"),!state.canArmBulldozerTest)}
                        {button("Create area test copy",()=>command("createAreaFixture"),!state.canArmBulldozerTest)}
                        <p>Select the saved mixed test asset first. This creates a separate area-only test copy, leaving the original unchanged. Place once, press Escape, select the outer asset, then Everything-delete one surface or walking area and use Undo → Redo → Undo. Do not save test edits.</p>
                        {button("Create nested fixture",()=>command("createNestedFixture"),!state.canArmBulldozerTest)}
                        {button("Inspect nested fixture",()=>command("inspectNestedFixture"),!state.available)}
                        {button("Arm nested group test",()=>command("armNestedGroupTest"),!state.canArmBulldozerTest)}
                        {button("Arm nested set test",()=>command("armNestedForestTest"),!state.canArmBulldozerTest)}
                    </div>}
                    <p>{shortcutLabel(state.settings.undoModifiers,state.settings.undoKey)}: Undo · {shortcutLabel(state.settings.redoModifiers,state.settings.redoKey)}: Redo</p>
                    <div className="mw-tools-history">{state.recent?.length ? state.recent.map((label,index)=><div key={index}>{label}</div>):"No recoverable deletions in this context."}</div>
                </>}
                {tab===tabs[4] && <MultiSaveContent/>}
                {tab===tabs[3] && <ColourVariationsContent/>}
                {tab!==tabs[0] && tab!==tabs[3] && tab!==tabs[4] && <p>{tab} is planned for a later phase. Its existing standalone mod remains separate.</p>}
                </div>
            </div>
        </NativePanel>}
    </>;
}
const register:ModRegistrar = registry => {registry.append("Editor",Launcher);registry.append("Editor",Tools);};
export const hasCSS = true;
export default register;







