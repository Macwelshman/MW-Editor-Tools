import React from "react";
import {bindValue,trigger,useValue} from "cs2/api";
import {Button,Tooltip,Dropdown,DropdownToggle,DropdownItem} from "cs2/ui";
import {getModule} from "cs2/modding";
type State={highlightStatus?:string;available:boolean;hasRow:boolean;canPaste:boolean;selected:number;rows:{index:number;type:string;value:string}[];asset:string;rowType:string;current:string;clipboard:string;pasteBlocked:string;undoStatus:string;status:string};
const binding=bindValue<string>("mwPropReplacer","state","{}");
const command=(v:string)=>trigger("mwPropReplacer","command",v);
export type Targeting={active:boolean;available:boolean;hoverIsSelected:boolean;filter:string;hover:string;selected:string;owner:string;feedback:string;status:string;outlineStatus:string};
const targetTypes=[["all","All supported types"],["props","Props"],["foliage","Trees and plants"],["effects","Effects"],["markers","Markers"],["surfaces","Surfaces"],["spaces","Areas"],["networks","Networks"],["lanes","Lanes"]];
const targetCommand=(v:string)=>trigger("mwEditorTools","command",v);
export function PropReplacerContent({targeting}: {targeting?:Partial<Targeting>}){
 const raw=useValue(binding);let s:Partial<State>;try{s=JSON.parse(raw||"{}")}catch{return <p>Replacer unavailable.</p>}
 const target=targeting||{};
 const theme=getModule("game-ui/editor/themes/editor-button.module.scss","classes"),classes=getModule("game-ui/editor/widgets/button/button.module.scss","classes");
 const b=(label:string,action:string,disabled=false,tip="")=><Tooltip key={action} tooltip={tip||label}><div className="mw-tools-button-wrap"><Button className={classes.button} theme={theme} disabled={disabled} onSelect={()=>command(action)}>{label}</Button></div></Tooltip>;
 const hasTarget=!!target.selected,hasClipboard=!!s.clipboard&&!s.clipboard.startsWith("Clipboard empty");
 return <div className="mw-replacer-body">
  <div className="mw-tools-context">{s.asset||"Select an asset in Object Info."}</div>
  <div className="mw-targeting-controls">
   <Tooltip tooltip={target.active?"Stop replacing and return to the native Select tool. You can also press Escape.":target.available?"Start replacing: hover and select elements in the viewport, then use Copy or Replace. Starting does not change any asset data.":target.status||"Select an editable asset first."}><div className="mw-tools-button-wrap"><Button className={classes.button} theme={theme} selected={!!target.active} disabled={!target.active&&!target.available} onSelect={()=>targetCommand("targetToggle")}>{target.active?"Stop replacing":"Start replacing"}</Button></div></Tooltip>
   {target.active&&<div className="mw-targeting-filter"><span>Element type</span><Dropdown theme={{dropdownPopup:"mw-targeting-popup",dropdownMenu:"mw-targeting-menu",scrollable:"mw-targeting-menu-scroll",dropdownItem:"mw-targeting-item"}} content={targetTypes.map(([value,label])=><DropdownItem key={value} value={value} selected={(target.filter||"all")===value} onChange={value=>targetCommand(`targetFilter|${value}`)}>{label}</DropdownItem>)}><DropdownToggle className="mw-targeting-dropdown" aria-label="Element type">{targetTypes.find(([value])=>value===(target.filter||"all"))?.[1]||"All supported types"}</DropdownToggle></Dropdown></div>}
   {target.active&&target.selected&&<Button className={classes.button} theme={theme} onSelect={()=>targetCommand("targetClear")}>Clear selection</Button>}
  </div>
  {!target.active&&<div className="mw-targeting-feedback" role="status">{target.status?.startsWith("Replacing stopped for the Object Info edit.")?target.status:target.available?"Start replacing to select elements in the viewport. Copy an element, then select another and choose Replace, or Replace all to change matching elements in the current asset.":target.status||"Replacing is unavailable in this session."}</div>}
  {target.active&&<div className="mw-targeting-feedback mw-targeting-feedback-active" role="status">
   <div>Replacing active. Hover for a gold outline; click to select with a cyan outline. Copy an element, then select its replacement target and choose Replace. Replace all changes matching elements in the current asset. Shortcuts use gold hover; buttons use cyan selection. Press Escape or Stop replacing to finish.</div>
   {target.selected&&<div><strong>Selected:</strong> {target.selected}</div>}
   {target.hover&&!target.hoverIsSelected&&<div><strong>Hover:</strong> {target.hover}</div>}
   {s.highlightStatus&&<div>{s.highlightStatus}</div>}
   {target.owner&&<div><strong>Asset:</strong> {target.owner}</div>}
   {target.outlineStatus&&<div>{target.outlineStatus}</div>}
   {s.undoStatus&&<div><strong>Replacement Undo:</strong> {s.undoStatus}</div>}
   {target.feedback&&<div><strong>Undo:</strong> {target.feedback}</div>}
   <div>{target.status}</div>
  </div>}
  {!target.active&&!!s.rows?.length&&<div className="mw-component-list">{s.rows.map(row=><Button key={row.index} className="mw-component-row" theme={theme} selected={s.selected===row.index} onSelect={()=>command(`select|${row.index}`)}><span>{row.value}</span><span>{row.type}</span></Button>)}</div>}
  <div className="mw-tools-clipboard"><span>{s.clipboard||"Clipboard empty"}</span></div>
  {s.pasteBlocked&&<div className="mw-tools-result" role="status">{s.pasteBlocked}</div>}
  <div className="mw-tools-actions">{b("Copy",target.active?"copyTarget":"copy",target.active?!hasTarget:!s.hasRow,"Copy the cyan-selected element. Ctrl+C copies the gold-hovered viewport element.")}{b("Replace",target.active?"pasteTarget":"paste",!!s.pasteBlocked||(target.active?!hasTarget||!hasClipboard:!s.canPaste),"Replace the cyan-selected element. Ctrl+V replaces the gold-hovered viewport element, keeping its placement and settings. Undo or Ctrl+Z restores the last replacement.")}{b("Replace all",target.active?"batchTarget":"batch",!!s.pasteBlocked||(target.active?!hasTarget||!hasClipboard:!s.canPaste),"Replace all matching the cyan-selected element. Ctrl+Shift+V uses the gold-hovered viewport element. Destination placement and row settings are preserved. Undo or Ctrl+Z restores the batch.")}{b("Clear Clipboard","clear",!s.clipboard||s.clipboard.startsWith("Clipboard empty"),"Clear the element clipboard.")}</div>
  {s.status&&!/^(Hover an element|Chosen |Copied |Ctrl\+C copied)/.test(s.status)&&<Tooltip tooltip={s.status}><div className="mw-tools-result" role="status">{s.status.startsWith("Replaced ")?"Replacement complete. Use Undo to restore it.":s.status}</div></Tooltip>}
 </div>;
}
