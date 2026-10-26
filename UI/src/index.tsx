import React, {useEffect, useLayoutEffect, useRef, useState} from "react";
import {bindValue, trigger, useValue} from "cs2/api";
import {ModRegistrar, getModule} from "cs2/modding";
import {Button, Tooltip} from "cs2/ui";
import {NativePanel} from "./native-panel";
import {MultiSaveContent,registerWorkspaceSaveAll} from "./multi-save";
import {ColourVariationsContent} from "./colours/window";
import {ComponentsContent} from "./components";
import {registerViewportRowHighlight} from "./viewport-row-highlight";
import {registerComponentHeadings} from "./component-headings";
import {PropReplacerContent,Targeting} from "./prop-replacer";
import launcherIcon from "./assets/mw-editor-tools.png";
import {launcherLeft} from "./launcher-position";
import "./style.scss";
import {TabErrorBoundary} from "./tab-error-boundary";
import {editorToolsState} from "./editor-tools-state";
type Options = {confirmation:boolean; recovery:boolean; shortcuts:boolean; inspectorControls:boolean; historyDepth:number; undoKey:string; redoKey:string; undoModifiers:number; redoModifiers:number; windowX:number; windowY:number};
type State = {targeting?:Targeting;available:boolean;visible:boolean;asset:string;status:string;bulldozerDiagnostic:string;undo:number;redo:number;undoTarget:string;redoTarget:string;canArmMainTest:boolean;canArmBulldozerTest:boolean;nestedHistoryEnabled:boolean;mainHistoryEnabled:boolean;canUndo:boolean;canRedo:boolean;recent:string[];positionRevision:number;settings:Options;protectedComponents:boolean;installedRecovery:boolean};
const binding = editorToolsState;
const width = bindValue<number>("editorHierarchy","width",500);
const command = (value:string) => trigger("mwEditorTools","command",value);
const shortcutLabel = (mask:number,key:string) => [mask&1?"Ctrl":"",mask&2?"Shift":"",mask&4?"Alt":"",mask&8?"Windows":"",key].filter(Boolean).join("+");
const tabs = ["Replacer","Components","Colours","Multi-Save"];
const introductions = ["Replace individual or matching elements in the viewport or component lists.","Copy component settings, transfer localisation, or number a pack.","Create and randomise colour variations for your asset.","Save all unlocked assets in your workspace."];
function Launcher() {
    const raw = useValue(binding), workspaceWidth = useValue(width);
    const slot = useRef<HTMLDivElement>(null);
    const [offset,setOffset] = useState(0);
    const appliedOffset = useRef(0);
    appliedOffset.current = offset;
    useLayoutEffect(() => {
        let frame = 0, cancelled = false;
        const position = () => {
            frame = 0;
            if (cancelled) return;
            const element = slot.current;
            if (!element) return;
            const own = element.getBoundingClientRect();
            if (!own.width || !own.height) return;
            const neighbours = Array.from(document.querySelectorAll('button, [role="button"]'))
                .filter(button => {
                    if (element.contains(button)) return false;
                    // This existing MW launcher already yields to other icons.
                    // Give it a stable priority to prevent both icons trading slots.
                    if (button.closest('.mw-component-launcher-slot')) return false;
                    const style = window.getComputedStyle(button);
                    return style.display !== "none" && style.visibility !== "hidden" && style.visibility !== "collapse";
                }).map(button => button.getBoundingClientRect());
            const base = own.left-appliedOffset.current;
            const next = launcherLeft(base,own,neighbours,own.width*8/56)-base;
            if (Number.isFinite(next) && Math.abs(next-appliedOffset.current) > .5) setOffset(next);
        };
        // Developer-menu transitions can invalidate native layout measurements.
        // Never synchronously remeasure in response to our own offset update.
        const schedule = () => { if (!cancelled && !frame) frame = window.requestAnimationFrame(position); };
        schedule();
        const timer = window.setInterval(schedule,500);
        window.addEventListener("resize",schedule);
        return () => { cancelled = true; window.cancelAnimationFrame(frame); window.clearInterval(timer); window.removeEventListener("resize",schedule); };
    },[raw,workspaceWidth]);
    let state: Partial<State>;
    try { state = JSON.parse(raw || "{}"); } catch { return null; }
    if (!state.available) return null;
    return <div ref={slot} className="mw-tools-launcher" style={{position:"fixed",top:"65rem",left:`${workspaceWidth+20}rem`,marginLeft:`${offset}px`,width:"56rem",height:"56rem",zIndex:100,pointerEvents:"auto"}}>
        <Tooltip tooltip="MW Editor Tools"><Button className="mw-tools-launcher-button" focusKey="mwEditorTools.launcher"
            aria-label="MW Editor Tools" aria-expanded={!!state.visible} selected={state.visible}
            style={{display:"block",width:"56rem",height:"56rem",padding:0,border:0,backgroundColor:"transparent",color:"#fff"}}
            onSelect={()=>command(state.visible ? "close" : "open")}>
            <img className="mw-tools-launcher-icon" src={launcherIcon} alt="" aria-hidden="true"/>
        </Button></Tooltip>
    </div>;
}
function Tools() {
    const raw = useValue(binding), workspaceWidth = useValue(width);
    const [tab,setTab] = useState(tabs[0]);


    const state = JSON.parse(raw) as State;
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
    useEffect(() => {
        let x=0,y=0;
        const move=(event:MouseEvent)=>{x=event.clientX;y=event.clientY;};
        const capture=()=>{
            const hovered=document.querySelectorAll(":hover");
            const samples:string[]=[];let element=(hovered.length?hovered[hovered.length-1]:document.elementFromPoint(x,y)) as HTMLElement|null;
            if(element?.closest('.mw-tools-panel,.mw-tools-launcher'))return samples;
            for(let i=0;element&&i<5;i++,element=element.parentElement){const values=[element.getAttribute("aria-label"),element.getAttribute("title"),element.textContent];for(const value of values)if(value&&value.trim()&&value.length<=400&&!samples.includes(value.trim()))samples.push(value.trim());}
            return samples;
        };
        const timer=setInterval(()=>trigger("mwPropReplacer","hoverCandidate",JSON.stringify({samples:capture()})),100);
        const copy=(event:KeyboardEvent)=>{
            if(!(event.ctrlKey&&!event.shiftKey&&!event.altKey&&!event.metaKey&&event.key.toLowerCase()==="c"))return;
            const active=document.activeElement as HTMLElement|null;
            if(active&&(active.tagName==="INPUT"||active.tagName==="TEXTAREA"||active.tagName==="SELECT"||active.isContentEditable))return;
            const samples:string[]=[];let element=document.elementFromPoint(x,y) as HTMLElement|null;
            for(let i=0;element&&i<8;i++,element=element.parentElement){const values=[element.getAttribute("aria-label"),element.getAttribute("title"),element.innerText];for(const value of values)if(value&&value.trim()&&value.length<=1000&&!samples.includes(value.trim()))samples.push(value.trim());}
            trigger("mwPropReplacer","hoverCopy",JSON.stringify({samples}));
        };
        document.addEventListener("mousemove",move,true);document.addEventListener("keydown",copy,true);
        return()=>{clearInterval(timer);document.removeEventListener("mousemove",move,true);document.removeEventListener("keydown",copy,true);};
    },[]);
    if(!state.available || !state.settings) return null;
    const theme = getModule("game-ui/editor/themes/editor-button.module.scss","classes");
    const buttonClasses = getModule("game-ui/editor/widgets/button/button.module.scss","classes");
    const button = (label:string,action:()=>void,disabled=false) => <Button className={buttonClasses.button} theme={theme} onSelect={action} disabled={disabled}>{label}</Button>;
    return <>
        {state.visible && <NativePanel key={state.positionRevision} className="mw-tools-panel"
            initialPosition={{x:state.settings.windowX,y:state.settings.windowY}}
            onPositionChange={position=>trigger("mwEditorTools","windowPosition",JSON.stringify({...position,revision:state.positionRevision}))}
            header={<div className="mw-tools-heading"><span>MW Editor Tools</span><div className="mw-tools-header-actions"><Tooltip tooltip={`${shortcutLabel(state.settings.undoModifiers,state.settings.undoKey)} · Undo the last supported replacement, move, node edit or deletion. ${state.canUndo?"Ready to undo.":state.status||state.bulldozerDiagnostic||"No supported action to undo."}`}><div>{button("Undo",()=>command("undo"),!state.canUndo)}</div></Tooltip>{button("Close",()=>command("close"))}</div></div>}>
            <div className="mw-tools-body">
                <div className="mw-tools-tabs" role="tablist" aria-label="Editor tools">{tabs.map((name,index)=><Button key={name} id={`mw-tools-tab-${index}`} role="tab" aria-selected={tab===name} aria-controls="mw-tools-section" theme={theme} selected={tab===name} onSelect={()=>setTab(name)}>{name}</Button>)}</div>
                <div className="mw-tools-section" id="mw-tools-section" role="tabpanel" aria-labelledby={`mw-tools-tab-${tabs.indexOf(tab)}`}>
                <p className="mw-tools-introduction">{introductions[tabs.indexOf(tab)]}</p>
                <TabErrorBoundary key={tab}>
                {tab===tabs[0] && <PropReplacerContent targeting={state.targeting}/>}
                {tab===tabs[1] && <ComponentsContent/>}
                {tab===tabs[2] && <ColourVariationsContent/>}
                {tab===tabs[3] && <MultiSaveContent/>}
                </TabErrorBoundary>
                </div>
            </div>
        </NativePanel>}
    </>;
}
const register:ModRegistrar = registry => {registerWorkspaceSaveAll(registry);registerComponentHeadings(registry);registerViewportRowHighlight(registry);registry.append("Editor",Launcher);registry.append("Editor",Tools);};
export const hasCSS = true;
export default register;



