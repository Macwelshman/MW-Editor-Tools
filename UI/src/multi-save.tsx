import React, {useEffect, useState} from "react";
import {bindValue, trigger, useValue} from "cs2/api";
import {getModule} from "cs2/modding";
import {Button, Scrollable, Tooltip} from "cs2/ui";
type SaveState = {available?:boolean;busy?:boolean;eligible?:number;locked?:number;saved?:number;failed?:number;skipped?:number;total?:number;processed?:number;current?:string;status?:string;report?:string};
const binding=bindValue<string>("mwEditorToolsMultiSave","state","{}");
const command=(value:string)=>trigger("mwEditorToolsMultiSave","command",value);
export function MultiSaveContent(){
    const raw=useValue(binding);
    let state:SaveState;try{state=JSON.parse(raw||"{}");}catch{state={};}
    const [details,setDetails]=useState(false);
    useEffect(()=>{command("open");return ()=>command("close");},[state.available]);
    useEffect(()=>{if(state.failed||state.status?.startsWith("Save All could not start:"))setDetails(true);},[state.failed,state.status]);
    const theme=getModule("game-ui/editor/themes/editor-button.module.scss","classes");
    const classes=getModule("game-ui/editor/widgets/button/button.module.scss","classes");
    const total=state.total||0,processed=state.processed||0;
    const progress=total?Math.max(0,Math.min(100,processed/total*100)):0;
    const status=state.status||"";
    const ready=!status||status==="Ready."||status==="Ready"||status.startsWith("Workspace scan:");
    const finished=status.startsWith("Finished.");
    const summary=state.busy
        ? /Waiting|Stopping|Preparing/.test(status)?status:`Saving ${Math.min(processed+1,total)} of ${total}…`
        : finished?`${state.saved||0} assets saved.${state.failed?` ${state.failed} failed.`:""}${state.skipped?` ${state.skipped} skipped.`:""}`
        : ready?`${state.eligible||0} assets ready to save.`:status;
    return <div className="mw-tools-multisave">
        <div className="mw-tools-actions">
            <Tooltip tooltip="Save all unlocked workspace assets, including hidden entries. Existing files are updated; new assets use their current names."><div className="mw-tools-button-wrap"><Button theme={theme} className={classes.button} disabled={!state.available||!!state.busy||!state.eligible} onSelect={()=>command("saveAll")}>Save all assets</Button></div></Tooltip>
            {state.busy&&<Button theme={theme} className={classes.button} onSelect={()=>command("stop")}>Stop</Button>}
        </div>
        <p role="status" aria-live="polite">{summary}</p>
        {state.busy&&<div className="mw-tools-save-progress" role="progressbar" aria-label="Assets processed" aria-valuemin={0} aria-valuemax={total} aria-valuenow={Math.min(processed,total)}><div style={{width:`${progress}%`}}/></div>}
        <Button theme={theme} className={classes.button} aria-expanded={details} onSelect={()=>setDetails(!details)}>{details?"Hide details":"Details"}</Button>
        {details&&<div className="mw-tools-save-details">
            <p>Saves all unlocked workspace assets, including hidden entries. Repeated instances save once.</p>
            <p>{`${state.eligible||0} unlocked · ${state.locked||0} locked assets excluded`}</p>
            {state.busy&&state.current&&<p>{state.current}</p>}
            <p>Stop finishes the current asset first. Existing files are updated; new assets use their current names.</p>
            {!!state.report&&<Scrollable vertical className="mw-tools-save-report" trackVisibility="scrollable"><div>{state.report}</div></Scrollable>}
        </div>}
    </div>;
}

