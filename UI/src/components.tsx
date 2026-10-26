import React, {useState} from "react";
import {PackEditor} from "./pack-editor";
import {bindValue, trigger, useValue} from "cs2/api";
import {Button, Tooltip} from "cs2/ui";
import {getModule} from "cs2/modding";

type State={available?:boolean;hasAsset?:boolean;assetName?:string;selectionToken?:string;clipboardSummary?:string;copyResult?:string;transferMode?:string;previewSummary?:string;canRestore?:boolean;selectedIndices?:number[];components?:{index:number;valid:boolean;typeName?:string;fields?:string[]}[]};
const binding=bindValue<string>("mwComponentTool","state","{}");
const command=(value:string)=>trigger("mwComponentTool","command",value);
export function ComponentsContent(){
 const [task,setTask]=useState<"components"|"localisation"|"priorities">("components");
 const raw=useValue(binding);const [fieldPanel,setFieldPanel]=useState(false);const [fieldState,setFieldState]=useState<{token:string;choices:Record<number,string[]>}>({token:"",choices:{}});let state:State;try{state=JSON.parse(raw||"{}");}catch{return <p>Component transfer unavailable.</p>}
 const theme=getModule("game-ui/editor/themes/editor-button.module.scss","classes"),classes=getModule("game-ui/editor/widgets/button/button.module.scss","classes");
 const button=(label:string,action:()=>void,disabled=false,active=false,tip="")=><Tooltip key={label} tooltip={tip||label}><div className="mw-tools-button-wrap"><Button className={classes.button} theme={theme} onSelect={action} disabled={disabled} selected={active}>{label}</Button></div></Tooltip>;
 if(!state.available)return <p>Component transfer unavailable.</p>;
 const selected=state.selectedIndices||[],rows=state.components||[];
 const choices=fieldState.token===state.selectionToken?fieldState.choices:{};
 const copy=(indices:number[])=>{
  const suffix=Object.keys(choices).length?"|"+JSON.stringify({choices:Object.keys(choices).map(key=>({index:Number(key),names:choices[Number(key)]}))}):"";
  command(`copy|${state.selectionToken}|${indices.join(",")}${suffix}`);
 };
 const clipboardEmpty=!state.clipboardSummary||state.clipboardSummary==="Clipboard empty.";
 return <div className="mw-component-body">
  <div className="mw-tools-context">{state.hasAsset?state.assetName:"Select an asset in Object Info."}</div>
  <nav className="mw-component-tasks" aria-label="Component tasks">{button("Component copying",()=>setTask("components"),false,task==="components")}{button("Localisation",()=>setTask("localisation"),false,task==="localisation")}{button("Pack priorities",()=>setTask("priorities"),false,task==="priorities")}</nav>
  {task==="components"&&<>
  <section className="mw-task-card"><h3>Copy from this asset</h3><p>Tick components beside their headings in Object Info, then copy.</p><div className="mw-task-caption">{`${selected.length}\u00a0${selected.length===1?"component":"components"} selected`}</div>
  <div className="mw-tools-actions">{button("Copy",()=>copy(selected),!state.hasAsset||!selected.length,false,"Copy the components checked in Object Info, including their settings.")}{button("Copy All",()=>copy(rows.filter(r=>r.valid).map(r=>r.index)),!state.hasAsset||!rows.some(r=>r.valid),false,"Copy all supported attached components and their settings.")}{button("Select All",()=>command(`select|${state.selectionToken}|${rows.filter(r=>r.valid).map(r=>r.index).join(",")}`),!state.hasAsset||!rows.some(r=>r.valid),false,"Select all attached components in Object Info.")}{button("Deselect All",()=>command(`select|${state.selectionToken}|`),!state.hasAsset||!selected.length,false,"Deselect all component checkboxes in Object Info.")}</div>
  <div className="mw-tools-actions">{button(fieldPanel?"Hide field choices":"Choose fields to copy",()=>setFieldPanel(!fieldPanel),!selected.length)}</div>
  {fieldPanel&&selected.map(index=>{const row=rows.find(r=>r.index===index);if(!row)return null;const fields=row.fields||[],chosen=choices[index]||fields;
   return <section key={index}><h3>{row.typeName?.split(".").pop()?.replace(/([A-Z])([A-Z][a-z])/g,"$1 $2").replace(/([a-z0-9])([A-Z])/g,"$1 $2")}</h3><div className="mw-tools-actions">{button("All fields",()=>setFieldState({token:state.selectionToken||"",choices:{...choices,[index]:fields}}))}{button("No fields",()=>setFieldState({token:state.selectionToken||"",choices:{...choices,[index]:[]}}))}{row.typeName==="Game.Prefabs.UIObject"&&button("Group and Priority",()=>setFieldState({token:state.selectionToken||"",choices:{...choices,[index]:["m_Group","m_Priority"]}}))}</div><div className="mw-tools-actions">{fields.map(field=>button(field.replace(/^m_/,"").replace(/([a-z0-9])([A-Z])/g,"$1 $2"),()=>setFieldState({token:state.selectionToken||"",choices:{...choices,[index]:chosen.includes(field)?chosen.filter(f=>f!==field):[...chosen,field]}}),false,chosen.includes(field),"Include this field in the next component copy."))}</div>{!fields.length&&<p>No supported fields available.</p>}</section>;
  })}
  <p className="mw-task-caption">Thumbnails stay unchanged. Field choices are optional.</p></section>
  <section className="mw-task-card"><h3>Paste into this asset</h3>
  <div className="mw-tools-clipboard"><span>{state.clipboardSummary||"Clipboard empty."}</span></div>
  <p className="mw-task-caption">Choose how matching and missing components are handled.</p><div className="mw-tools-actions mw-tools-modes" role="group" aria-label="Paste mode">{[
   ["ComponentsOnly","Add missing","Add missing components with copied settings. Leave existing components unchanged."],
   ["SettingsOnly","Replace present","Replace settings on matching components. Skip missing components."],
   ["ComponentsAndSettings","Replace and add","Replace settings on matching components and add missing components with copied settings."]
  ].map(([v,l,tip])=>button(l,()=>command(`mode|${v}`),false,state.transferMode===v,tip))}</div>
  <div className="mw-tools-actions">{button("Paste",()=>command(`paste|${state.selectionToken}`),!state.hasAsset||clipboardEmpty,false,"Check compatibility and paste copied components. Existing activation is preserved; additions keep copied activation.")}{button("Recover",()=>command(`recover|${state.selectionToken}`),!state.hasAsset||!state.canRestore,false,"Recover the destination before the last component paste. Later edits prevent recovery.")}{button("Clear Clipboard",()=>command("clearClipboard"),clipboardEmpty,false,"Clear copied components from the clipboard.")}</div>
  </section>
  {(state.previewSummary||state.copyResult)&&<div className="mw-component-result" role="status">{state.previewSummary&&<p>{state.previewSummary}</p>}{state.copyResult&&<p>{state.copyResult}</p>}</div>}</>}
  <PackEditor hasAsset={state.hasAsset} token={state.selectionToken} view={task==="components"?"none":task}/>
 </div>;
}

