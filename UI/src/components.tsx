import React, {useState} from "react";
import {bindValue, trigger, useValue} from "cs2/api";
import {Button, Tooltip} from "cs2/ui";
import {getModule} from "cs2/modding";

type State={available?:boolean;hasAsset?:boolean;assetName?:string;status?:string;selectionToken?:string;clipboardSummary?:string;copyResult?:string;selectedIndices?:number[];transferMode?:string;previewSummary?:string;canApply?:boolean;canRestore?:boolean;components?:{index:number;typeName:string;active:boolean;valid:boolean}[]};
const binding=bindValue<string>("mwComponentTool","state","{}");
const command=(value:string)=>trigger("mwComponentTool","command",value);
export function ComponentsContent(){
 const raw=useValue(binding);const [details,setDetails]=useState(false);let state:State;try{state=JSON.parse(raw||"{}");}catch{return <p>Component transfer unavailable.</p>}
 const selected=state.selectedIndices||[],rows=state.components||[];
 const theme=getModule("game-ui/editor/themes/editor-button.module.scss","classes"),classes=getModule("game-ui/editor/widgets/button/button.module.scss","classes");
 const button=(label:string,action:()=>void,disabled=false,active=false,tip="")=><Tooltip key={label} tooltip={tip||label}><div className="mw-tools-button-wrap"><Button className={classes.button} theme={theme} onSelect={action} disabled={disabled} selected={active}>{label}</Button></div></Tooltip>;
 const select=(indices:number[])=>command(`select|${state.selectionToken||""}|${indices.join(",")}`);
 if(!state.available)return <p>Component transfer unavailable.</p>;
 return <div className="mw-component-body">
  <div className="mw-tools-context">{state.hasAsset?state.assetName:"Select an asset in Object Info."}</div>
  {state.hasAsset&&<>
   <div className="mw-tools-subheading"><h3>{`Components (${rows.length})`}</h3><span>{selected.length?`${selected.length} selected`:""}</span></div>
   <div className="mw-component-list">{rows.map(row=><Button key={`${state.selectionToken}:${row.index}`} className="mw-component-row" theme={theme} disabled={!row.valid} selected={selected.includes(row.index)} onSelect={()=>select(selected.includes(row.index)?selected.filter(i=>i!==row.index):[...selected,row.index])}><span>{row.typeName.split(".").pop()?.replace(/([a-z0-9])([A-Z])/g,"$1 $2")}</span><span>{row.valid?(row.active?"Active":"Inactive"):"Missing"}</span></Button>)}</div>
   <div className="mw-tools-actions">{button("Copy selected",()=>command(`copy|${state.selectionToken}|${selected.join(",")}`),!selected.length,false,"Copy selected components and their settings. Then select a destination asset.")}{button("Copy all",()=>command(`copy|${state.selectionToken}|${rows.filter(r=>r.valid).map(r=>r.index).join(",")}`),!rows.some(r=>r.valid),false,"Copy all supported components and their settings.")}</div>
  </>}
  <div className="mw-tools-clipboard"><span>{state.clipboardSummary||"Clipboard empty."}</span>{button("Clear",()=>command("clearClipboard"),!state.clipboardSummary||state.clipboardSummary==="Clipboard empty.",false,"Clear the component clipboard.")}</div>
  <div className="mw-tools-actions mw-tools-modes" role="group" aria-label="Paste mode">{[
   ["ComponentsOnly","Add missing","Add missing components with copied settings. Leave existing components unchanged."],
   ["SettingsOnly","Replace present","Replace settings on matching components. Skip missing components."],
   ["ComponentsAndSettings","Replace and add","Replace settings on matching components and add missing components with copied settings."]
  ].map(([v,l,tip])=>button(l,()=>command(`mode|${v}`),false,state.transferMode===v,tip))}</div>
  <div className="mw-tools-actions">{button("Preview",()=>{setDetails(true);command(`preview|${state.selectionToken}`)},!state.hasAsset,false,"Check compatibility and review the changes before pasting.")}{button("Paste",()=>command(`apply|${state.selectionToken}`),!state.hasAsset||!state.canApply,false,"Apply the previewed changes. Existing activation is preserved; additions keep copied activation.")}{button("Restore",()=>command(`restore|${state.selectionToken}`),!state.hasAsset||!state.canRestore,false,"Restore the destination before the last component paste. Later edits prevent Restore.")}{button(details?"Hide details":"Details",()=>setDetails(!details),false,false,"Show skipped components, compatibility checks and transfer results.")}</div>
  {!details&&state.copyResult&&/failed|did not complete|refused|changed|skipped|unsupported/i.test(state.copyResult)&&<div className="mw-tools-result" role="status">{state.copyResult}</div>}
  {details&&<div className="mw-component-result" role="status"><p>{state.previewSummary}</p>{state.copyResult&&<p>{state.copyResult}</p>}</div>}
 </div>;
}

