import React, {useEffect, useState} from "react";
import {bindValue, trigger, useValue} from "cs2/api";
import {ModRegistrar, getModule} from "cs2/modding";
import {Button, Tooltip} from "cs2/ui";
import {NativePanel} from "./native-panel";
import {MultiSaveContent} from "./multi-save";
import {ColourVariationsContent} from "./colours/window";
import {ComponentsContent} from "./components";
import {PropReplacerContent} from "./prop-replacer";
import "./style.scss";
type Options = {confirmation:boolean; recovery:boolean; shortcuts:boolean; inspectorControls:boolean; historyDepth:number; undoKey:string; redoKey:string; undoModifiers:number; redoModifiers:number; windowX:number; windowY:number};
type State = {available:boolean;visible:boolean;asset:string;status:string;bulldozerDiagnostic:string;undo:number;redo:number;undoTarget:string;redoTarget:string;canArmMainTest:boolean;canArmBulldozerTest:boolean;nestedHistoryEnabled:boolean;mainHistoryEnabled:boolean;canUndo:boolean;canRedo:boolean;recent:string[];positionRevision:number;settings:Options;protectedComponents:boolean;installedRecovery:boolean};
const binding = bindValue<string>("mwEditorTools","state","{}");
const width = bindValue<number>("editorHierarchy","width",500);
const command = (value:string) => trigger("mwEditorTools","command",value);
const shortcutLabel = (mask:number,key:string) => [mask&1?"Ctrl":"",mask&2?"Shift":"",mask&4?"Alt":"",mask&8?"Windows":"",key].filter(Boolean).join("+");
const tabs = ["Replacer","Components","Colours","Multi-Save"];
const introductions = ["Replace individual or matching elements in the viewport or component lists.","Copy components and their settings between assets.","Create and randomise colour variations for your asset.","Save all unlocked assets in your workspace."];
function Launcher() {
    const raw = useValue(binding), workspaceWidth = useValue(width);
    let state: Partial<State>;
    try { state = JSON.parse(raw || "{}"); } catch { return null; }
    if (!state.available || state.visible) return null;
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
            if(active&&(active.tagName==="INPUT"||active.tagName==="TEXTAREA"||active.isContentEditable))return;
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
            header={<div className="mw-tools-heading"><span>MW Editor Tools</span><div className="mw-tools-header-actions"><Tooltip tooltip={`${shortcutLabel(state.settings.undoModifiers,state.settings.undoKey)} · Undo the last supported move, node edit or deletion. ${state.canUndo?"Ready to undo.":"No supported action to undo."}`}><div>{button("Undo",()=>command("undo"),!state.canUndo)}</div></Tooltip>{button("Close",()=>command("close"))}</div></div>}>
            <div className="mw-tools-body">
                <div className="mw-tools-tabs" role="tablist" aria-label="Editor tools">{tabs.map((name,index)=><Button key={name} id={`mw-tools-tab-${index}`} role="tab" aria-selected={tab===name} aria-controls="mw-tools-section" theme={theme} selected={tab===name} onSelect={()=>setTab(name)}>{name}</Button>)}</div>
                <div className="mw-tools-section" id="mw-tools-section" role="tabpanel" aria-labelledby={`mw-tools-tab-${tabs.indexOf(tab)}`}>
                <p className="mw-tools-introduction">{introductions[tabs.indexOf(tab)]}</p>
                {tab===tabs[0] && <PropReplacerContent/>}
                {tab===tabs[1] && <ComponentsContent/>}
                {tab===tabs[2] && <ColourVariationsContent/>}
                {tab===tabs[3] && <MultiSaveContent/>}
                </div>
            </div>
        </NativePanel>}
    </>;
}
const register:ModRegistrar = registry => {registry.append("Editor",Launcher);registry.append("Editor",Tools);};
export const hasCSS = true;
export default register;



