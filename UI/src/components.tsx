import React from "react";
import {bindValue, trigger, useValue} from "cs2/api";
import {Button, Tooltip} from "cs2/ui";
import {getModule} from "cs2/modding";

type State={available?:boolean;hasAsset?:boolean;assetName?:string;selectionToken?:string;clipboardSummary?:string;copyResult?:string;transferMode?:string;previewSummary?:string;canRestore?:boolean;selectedIndices?:number[];components?:{index:number;valid:boolean}[]};
const binding=bindValue<string>("mwComponentTool","state","{}");
const command=(value:string)=>trigger("mwComponentTool","command",value);
export function ComponentsContent(){
 const raw=useValue(binding);let state:State;try{state=JSON.parse(raw||"{}");}catch{return <p>Component transfer unavailable.</p>}
 const theme=getModule("game-ui/editor/themes/editor-button.module.scss","classes"),classes=getModule("game-ui/editor/widgets/button/button.module.scss","classes");
 const button=(label:string,action:()=>void,disabled=false,active=false,tip="")=><Tooltip key={label} tooltip={tip||label}><div className="mw-tools-button-wrap"><Button className={classes.button} theme={theme} onSelect={action} disabled={disabled} selected={active}>{label}</Button></div></Tooltip>;
 if(!state.available)return <p>Component transfer unavailable.</p>;
 const selected=state.selectedIndices||[],rows=state.components||[];
 const clipboardEmpty=!state.clipboardSummary||state.clipboardSummary==="Clipboard empty.";
 return <div className="mw-component-body">
  <div className="mw-tools-context">{state.hasAsset?state.assetName:"Select an asset in Object Info."}</div>
  <p>Tick the selection checkbox beside each component heading in Object Info, then click Copy. You can select several components or use Select All.</p>
  <div className="mw-tools-actions">{button("Copy",()=>command(`copy|${state.selectionToken}|${selected.join(",")}`),!state.hasAsset||!selected.length,false,"Copy the components checked in Object Info, including their settings.")}{button("Copy All",()=>command(`copy|${state.selectionToken}|${rows.filter(r=>r.valid).map(r=>r.index).join(",")}`),!state.hasAsset||!rows.some(r=>r.valid),false,"Copy all supported attached components and their settings.")}{button("Select All",()=>command(`select|${state.selectionToken}|${rows.filter(r=>r.valid).map(r=>r.index).join(",")}`),!state.hasAsset||!rows.some(r=>r.valid),false,"Select all attached components in Object Info.")}{button("Deselect All",()=>command(`select|${state.selectionToken}|`),!state.hasAsset||!selected.length,false,"Deselect all component checkboxes in Object Info.")}</div>
  <div className="mw-tools-clipboard"><span>{state.clipboardSummary||"Clipboard empty."}</span></div>
  <div className="mw-tools-actions mw-tools-modes" role="group" aria-label="Paste mode">{[
   ["ComponentsOnly","Add missing","Add missing components with copied settings. Leave existing components unchanged."],
   ["SettingsOnly","Replace present","Replace settings on matching components. Skip missing components."],
   ["ComponentsAndSettings","Replace and add","Replace settings on matching components and add missing components with copied settings."]
  ].map(([v,l,tip])=>button(l,()=>command(`mode|${v}`),false,state.transferMode===v,tip))}</div>
  <div className="mw-tools-actions">{button("Paste",()=>command(`paste|${state.selectionToken}`),!state.hasAsset||clipboardEmpty,false,"Check compatibility and paste copied components. Existing activation is preserved; additions keep copied activation.")}{button("Restore",()=>command(`restore|${state.selectionToken}`),!state.hasAsset||!state.canRestore,false,"Restore the destination before the last component paste. Later edits prevent Restore.")}{button("Clear Clipboard",()=>command("clearClipboard"),clipboardEmpty,false,"Clear copied components from the clipboard.")}</div>
  <div className="mw-component-result" role="status">{state.previewSummary&&<p>{state.previewSummary}</p>}{state.copyResult&&<p>{state.copyResult}</p>}</div>
 </div>;
}

