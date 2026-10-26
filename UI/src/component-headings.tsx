import React,{useEffect,useRef} from "react";
import {bindValue, trigger, useValue} from "cs2/api";
import {getModule, ModRegistrar} from "cs2/modding";
import {Tooltip} from "cs2/ui";

type Row = {index:number;valid:boolean;sectionId?:string};
type State = {available?:boolean;hasAsset?:boolean;selectionToken?:string;selectedIndices?:number[];components?:Row[]};
const binding = bindValue<string>("mwComponentTool","state","{}");
const rowHighlightBinding=bindValue<string>("mwPropReplacer","state","{}");
const command = (value:string) => trigger("mwComponentTool","command",value);
const stop = (event:React.SyntheticEvent) => event.stopPropagation();

export function headingSelection(state:State, sectionId:string) {
    if (!state.available || !state.hasAsset || !state.selectionToken || !sectionId) return null;
    const matches = (state.components || []).filter(row => row.valid && row.sectionId === sectionId);
    return matches.length === 1 ? matches[0] : null;
}

export const registerComponentHeadings:ModRegistrar = registry => {
    registry.extend("game-ui/editor/widgets/section/editor-section.tsx","EditorSectionBase",Original => function ComponentHeading(props:any) {
        const raw = useValue(binding);
        const rowRaw=useValue(rowHighlightBinding);
        let marked=false,highlightKey="";try{const data=JSON.parse(rowRaw||"{}");marked=data.highlightComponent===props.sectionId&&!!props.sectionId;highlightKey=data.highlightKey||"";}catch{}
        const useToggleState=getModule("game-ui/common/hooks/use-toggle-state.tsx","useToggleState") as ()=>{setState:(id:string,expanded:boolean)=>void};
        const toggles=useToggleState(),revealed=useRef("");
        useEffect(()=>{
            if(!marked||!highlightKey){revealed.current="";return;}
            if(revealed.current===highlightKey)return;revealed.current=highlightKey;
            toggles.setState(props.sectionId,true);
            if(!props.expanded)props.onExpandedChange?.(true);
        },[marked,highlightKey,props.sectionId]);
        if(marked)props={...props,className:[props.className,"mw-viewport-selected-component"].filter(Boolean).join(" ")};
        let state:State = {};
        try { state = JSON.parse(raw || "{}"); } catch { /* Leave the native heading untouched. */ }
        if (props.sectionId === "editorPanel.mwRecovery")
            return <Original {...props} title={<span style={{textTransform:"none"}}>{props.title}</span>}/>;
        const row = headingSelection(state, props.sectionId);
        if (!row) return <Original {...props}/>;
        const selected = state.selectedIndices || [];
        const checked = selected.includes(row.index);
        const select = () => command(`select|${state.selectionToken}|${(checked ? selected.filter(i => i !== row.index) : [...selected,row.index]).join(",")}`);
        const Checkbox = getModule("game-ui/common/input/toggle/checkbox/checkbox.tsx","Checkbox") as React.ComponentType<any>;
        const checkboxTheme = getModule("game-ui/editor/themes/editor-small-checkbox.module.scss","classes");
        const focusDisabled = getModule("game-ui/common/focus/focus-key.ts","FOCUS_DISABLED");
        // Match native embedded activation checkboxes: the enclosing heading is
        // a leaf focus node, so this visual Toggle must not register a child.
        // The wrapper handles selection keyboard events without expanding it.
        const action = (run:()=>void) => (event:React.SyntheticEvent) => { event.preventDefault(); event.stopPropagation(); run(); };
        const key = (run:()=>void) => (event:React.KeyboardEvent) => {
            event.stopPropagation();
            if (event.key === " " || event.key === "Enter") action(run)(event);
        };
        return <Original {...props}
            title={<span className="mw-component-heading-title">
                <Tooltip tooltip={checked ? "Selected for copying. Click to deselect this component." : "Select this component and its settings for copying. This does not change component activation."}>
                <span role="checkbox" tabIndex={0} aria-label="Select component for copying" aria-checked={checked}
                    className="mw-component-heading-check" onMouseDown={stop} onMouseUp={stop}
                    onClick={stop} onKeyDown={key(select)} onKeyUp={stop}>
                    <Checkbox checked={checked} onChange={select} theme={checkboxTheme} focusKey={focusDisabled}/>
                </span>
                </Tooltip>
                {props.title}
            </span>}/>;
    });
};
