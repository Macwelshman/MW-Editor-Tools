import React from "react";
import {bindValue,trigger,useValue} from "cs2/api";
import {Button,Tooltip} from "cs2/ui";
import {getModule} from "cs2/modding";
type State={available:boolean;hasRow:boolean;canPaste:boolean;selected:number;rows:{index:number;type:string;value:string}[];asset:string;rowType:string;current:string;clipboard:string;status:string};
const binding=bindValue<string>("mwPropReplacer","state","{}");
const command=(v:string)=>trigger("mwPropReplacer","command",v);
export function PropReplacerContent(){
 const raw=useValue(binding);let s:Partial<State>;try{s=JSON.parse(raw||"{}")}catch{return <p>Replacer unavailable.</p>}
 const theme=getModule("game-ui/editor/themes/editor-button.module.scss","classes"),classes=getModule("game-ui/editor/widgets/button/button.module.scss","classes");
 const b=(label:string,action:string,disabled=false,tip="")=><Tooltip key={action} tooltip={tip||label}><div className="mw-tools-button-wrap"><Button className={classes.button} theme={theme} disabled={disabled} onSelect={()=>command(action)}>{label}</Button></div></Tooltip>;
 return <div className="mw-replacer-body">
  <div className="mw-tools-context">{s.asset||"Select an asset in Object Info."}</div>
  <Tooltip tooltip="Hover an element in Object Info or the viewport. Ctrl+C copies, Ctrl+V replaces, Ctrl+Shift+V replaces all matching elements in its asset list. Destination placement and row settings are preserved."><div className="mw-tools-shortcuts">Ctrl+C · Copy　 Ctrl+V · Replace　 Ctrl+Shift+V · Replace all</div></Tooltip>
  {!!s.rows?.length&&<div className="mw-component-list">{s.rows.map(row=><Button key={row.index} className="mw-component-row" theme={theme} selected={s.selected===row.index} onSelect={()=>command(`select|${row.index}`)}><span>{row.value}</span><span>{row.type}</span></Button>)}</div>}
  <div className="mw-tools-clipboard"><span>{s.clipboard||"Clipboard empty"}</span>{b("Clear","clear",!s.clipboard||s.clipboard.startsWith("Clipboard empty"),"Clear the element clipboard.")}</div>
  <div className="mw-tools-actions">{b("Copy","copy",!s.hasRow,"Copy the chosen element. You can also hover an element and press Ctrl+C.")}{b("Replace","paste",!s.canPaste,"Replace the chosen row, keeping its placement and settings.")}{b("Replace all","batch",!s.canPaste,"Replace every matching element in the chosen asset list.")}</div>
  {s.status&&!/^(Hover an element|Chosen |Copied |Ctrl\+C copied)/.test(s.status)&&<Tooltip tooltip={s.status}><div className="mw-tools-result" role="status">{s.status.startsWith("Replaced ")?"Replacement complete.":s.status}</div></Tooltip>}
 </div>;
}

