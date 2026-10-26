import React,{createContext,useContext,useEffect,useLayoutEffect,useRef} from "react";
import {bindValue,useValue,trigger} from "cs2/api";
import {ModRegistrar,getModule} from "cs2/modding";
import {scheduleRowReveal} from "./row-reveal";
const binding=bindValue<string>("mwPropReplacer","state","{}");
const ListPath=createContext("");
type Selection={highlightKey?:string;highlightList?:string;highlightIndex?:number;highlightPage?:number;highlightComponent?:string};
let cachedRaw:string|undefined,cachedSelection:Selection={};
function read(raw:string):Selection {
 if(raw===cachedRaw)return cachedSelection;
 cachedRaw=raw;
 try{cachedSelection=JSON.parse(raw||"{}");}catch{cachedSelection={};}
 return cachedSelection;
}
export function listIdentity(parent:any,path:any):string {
 return parent?.group&&Array.isArray(parent.path)&&typeof path==="string"?[parent.group,...parent.path,path].join("."):"";
}
export function isViewportRow(state:Selection,list:string,index:any):boolean {
 return !!list&&state.highlightList===list&&Number.isInteger(index)&&index>=0&&state.highlightIndex===index;
}
export function revealList(state:Selection,list:string,data:any,send:(command:string,value:any)=>void) {
 if(!state.highlightKey||state.highlightList!==list||!list)return;
 if(!data.expanded&&!data.locked)send("setExpanded",true);
 if(Number.isInteger(state.highlightPage)&&(state.highlightPage??-1)>=0&&state.highlightPage!==data.currentPageIndex)send("setCurrentPageIndex",state.highlightPage);
}
export const registerViewportRowHighlight:ModRegistrar=registry=>{
 const pagedType="Game.UI.Widgets.PagedList";
 const maps=new WeakMap<object,{original:any;patched:any}>();
 const decorate=(components:any)=>{
  if(!components||typeof components[pagedType]!=="function")return components;
  const saved=maps.get(components);
  if(saved&&(saved.patched===components||saved.original===components[pagedType]&&Object.keys(components).length===Object.keys(saved.patched).length&&Object.keys(components).every(key=>key===pagedType||components[key]===saved.patched[key])))return saved.patched;
  const Original=components[pagedType];
  function ViewportList(props:any){
  const identity=listIdentity(props.parent,props.path),state=read(useValue(binding)),revealed=useRef("");
  useEffect(()=>{
   if(state.highlightList!==identity||!state.highlightKey){revealed.current="";return;}
   const key=`${state.highlightKey}|${state.highlightIndex}|${state.highlightPage}`;
   if(revealed.current===key)return;revealed.current=key;
   revealList(state,identity,props.props||{},(command,value)=>trigger(props.parent.group,command,[...props.parent.path,props.path],value));
  },[identity,state.highlightKey,state.highlightList,state.highlightIndex,state.highlightPage]);
  return <ListPath.Provider value={identity}><Original {...props}/></ListPath.Provider>;
  }
  const patched={...components,[pagedType]:ViewportList};
  maps.set(components,{original:Original,patched});maps.set(patched,{original:ViewportList,patched});
  return patched;
 };
 // Native widget maps capture BoundPagedList before mod extensions run. Patch a
 // scoped copy at the live renderer boundary instead of replacing that export.
 for(const name of ["WidgetRenderer","WidgetListRenderer"]){
  registry.extend("game-ui/widgets/components/widget-renderer.tsx",name,Original=>function ViewportWidgetRenderer(props:any){
   return <Original {...props} components={decorate(props.components)}/>;
  });
 }
 registry.extend("game-ui/editor/widgets/list/list.tsx","EditorListItem",Original=>function ViewportRow(props:any){
  const list=useContext(ListPath),state=read(useValue(binding)),element=useRef<HTMLDivElement>(null),marked=isViewportRow(state,list,props.focusKey);
  const scrollContext=getModule("game-ui/common/scrolling/scrollable-context.ts","ScrollableContext");
  const scrolling=useContext(scrollContext) as any,scrollingRef=useRef(scrolling);scrollingRef.current=scrolling;
  useLayoutEffect(()=>{
   if(!marked)return;
   const classes=getModule("game-ui/editor/widgets/list/list.module.scss","classes");
   const className=classes.elementName?.split(" ")[0];
   const box=className?element.current?.querySelector(`.${className}`):null;
   box?.classList.add("mw-viewport-selected-box");
   return()=>box?.classList.remove("mw-viewport-selected-box");
  },[marked,state.highlightKey]);
  useLayoutEffect(()=>{
   if(!marked)return;
   return scheduleRowReveal(()=>element.current,requestAnimationFrame,cancelAnimationFrame,node=>{
    const scroll=scrollingRef.current;
    const native=!!scroll?.container&&typeof scroll.scrollIntoView==="function";
    if(native)scroll.scrollIntoView(node);
    else node.scrollIntoView?.({block:"nearest",inline:"nearest"});
   });
  },[marked,state.highlightKey]);
  if(!marked)return <Original {...props}/>;
  const badge=<span className="mw-viewport-selected-label" title="Selected in viewport">Selected</span>;
  return <div ref={element} className="mw-viewport-selected-row"><Original {...props} label={props.elementName!=null?props.label:<>{props.label}{badge}</>} elementName={props.elementName!=null?<>{props.elementName}{badge}</>:props.elementName}/></div>;
 });
};
