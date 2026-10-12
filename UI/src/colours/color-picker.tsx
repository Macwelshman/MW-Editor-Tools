import React, { useEffect, useLayoutEffect, useRef, useState } from "react";
import { createPortal } from "react-dom";
import { AutoNavigationScope } from "cs2/input";
import { EditorButton as Button } from "./native-panel";
import { color } from "./palette";

function hex(h: number, s: number, v: number, a: number) {
    const c = v * s, x = c * (1 - Math.abs(h / 60 % 2 - 1)), m = v - c;
    const rgb = h < 60 ? [c,x,0] : h < 120 ? [x,c,0] : h < 180 ? [0,c,x] : h < 240 ? [0,x,c] : h < 300 ? [x,0,c] : [c,0,x];
    return "#" + [...rgb.map(n => Math.round((n + m) * 255)), a].map(n => n.toString(16).padStart(2,"0")).join("").toUpperCase();
}
function hsv(value: string) {
    const text = color(value) ? value.trim().replace(/^#/, "") : "FFFFFF";
    const [r,g,b] = [0,2,4].map(i => parseInt(text.slice(i,i+2),16) / 255);
    const max = Math.max(r,g,b), min = Math.min(r,g,b), d = max-min;
    const h = !d ? 0 : max === r ? 60*((g-b)/d % 6) : max === g ? 60*((b-r)/d+2) : 60*((r-g)/d+4);
    return { h: (h+360)%360, s: max ? d/max : 0, v: max, a: text.length === 8 ? parseInt(text.slice(6),16) : 255 };
}
export function ColorPicker({value, label, anchor, onChange, onClose}: {value:string; label:string; anchor:{left:number;right:number;top:number;bottom:number}; onChange:(value:string)=>void; onClose:()=>void}) {
    const [draft,setDraft] = useState(() => hsv(value));
    const dialog = useRef<HTMLDivElement>(null);
    const [position, setPosition] = useState({ left: anchor.right + 12, top: anchor.top });
    useLayoutEffect(() => {
        const place = () => {
            const rect = dialog.current?.getBoundingClientRect();
            if (!rect) return;
            const left = anchor.right + 12 + rect.width <= window.innerWidth - 8 ? anchor.right + 12 : anchor.left - rect.width - 12;
            setPosition({ left: Math.max(8, Math.min(left, window.innerWidth - rect.width - 8)),
                top: Math.max(8, Math.min(anchor.top, window.innerHeight - rect.height - 8)) });
        };
        place(); window.addEventListener("resize", place);
        return () => window.removeEventListener("resize", place);
    }, [anchor]);
    const close = useRef<HTMLButtonElement>(null);
    useEffect(() => {
        const previous = document.activeElement as HTMLElement;
        close.current?.focus();
        return () => previous?.focus();
    }, []);
    const update = (change: Partial<typeof draft>) => setDraft({...draft,...change});
    const pick = (e: React.MouseEvent<SVGSVGElement>) => {
        if (e.type === "mousemove" && !(e.buttons & 1)) return;
        const rect = e.currentTarget.getBoundingClientRect();
        const x = (e.clientX-rect.left)/rect.width*200-100, y = (e.clientY-rect.top)/rect.height*200-100;
        update({h:(Math.atan2(y,x)*180/Math.PI+360)%360, s:Math.min(1,Math.hypot(x,y)/96)});
    };
    const result = hex(draft.h,draft.s,draft.v,draft.a);
    return createPortal(<AutoNavigationScope><div className="mwet-colors-picker-overlay" onMouseDown={e => {if(e.target === e.currentTarget) onClose();}}
        onKeyDown={e => { if(e.key === "Escape") {e.stopPropagation();onClose();} if(e.key === "Tab") {
            const controls = Array.from(e.currentTarget.querySelectorAll<HTMLElement>("button,input"));
            const first = controls[0], last = controls[controls.length-1];
            if(e.shiftKey && document.activeElement === first) {e.preventDefault();last.focus();}
            else if(!e.shiftKey && document.activeElement === last) {e.preventDefault();first.focus();}
        }}}>
        <div ref={dialog} className="mwet-colors-picker-dialog" style={{ left: position.left, top: position.top }} role="dialog" aria-label={"Edit " + label}>
            <div className="mwet-colors-picker-heading"><strong>{label}</strong><button ref={close} onClick={onClose} aria-label="Close colour picker">×</button></div>
            <svg className="mwet-colors-wheel" viewBox="0 0 200 200" onMouseDown={pick} onMouseMove={pick} aria-label="Colour wheel">
                <defs><radialGradient id="mwet-colors-wheel-white"><stop offset="0" stopColor="white"/><stop offset="1" stopColor="white" stopOpacity="0"/></radialGradient></defs>
                {Array.from({length:120},(_,i) => {const a=i*3*Math.PI/180,b=(i*3+3.5)*Math.PI/180;
                    return <path key={i} fill={hex(i*3,1,1,255)} d={`M100 100 L${100+96*Math.cos(a)} ${100+96*Math.sin(a)} A96 96 0 0 1 ${100+96*Math.cos(b)} ${100+96*Math.sin(b)} Z`}/>;})}
                <circle cx="100" cy="100" r="96" fill="url(#mwet-colors-wheel-white)"/>
                <circle cx={100+96*draft.s*Math.cos(draft.h*Math.PI/180)} cy={100+96*draft.s*Math.sin(draft.h*Math.PI/180)} r="5" fill="none" stroke="black" strokeWidth="3"/>
            </svg>
            {([['Hue','h',359],['Saturation','s',100],['Brightness','v',100],['Opacity','a',255]] as const).map(([name,key,max]) => {
                const percent = key === 's' || key === 'v';
                const number = Math.min(max, Math.round(draft[key] * (percent ? 100 : 1)));
                return <label className="mwet-colors-slider-row" key={key}><span>{name}</span><input type="range" min="0" max={max} step="1" value={number} onChange={e => update({[key]:Number(e.currentTarget.value) / (percent ? 100 : 1)})}/><span>{number}</span></label>;
            })}
            <div className="mwet-colors-picker-result"><span className="mwet-colors-swatch" style={{backgroundColor:result}}/>{result}</div>
            <div className="mwet-colors-actions"><Button variant="primary" onSelect={() => {onChange(result);onClose();}}>Use colour</Button><Button onSelect={onClose}>Cancel</Button></div>
        </div>
    </div></AutoNavigationScope>, document.body);
}

