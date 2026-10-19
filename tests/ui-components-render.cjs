const fs=require('fs'),path=require('path'),vm=require('vm');
const ui=path.resolve(__dirname,'../UI'),React=require(path.join(ui,'node_modules/react'));
const {renderToStaticMarkup}=require(path.join(ui,'node_modules/react-dom/server'));
const ts=require(path.join(ui,'node_modules/typescript'));
let state={available:true,hasAsset:true,assetName:'Destination',selectionToken:'token',selectedIndices:[0],components:[{index:0,typeName:'Game.Prefabs.Workplace',active:true,valid:true}],transferMode:'ComponentsOnly',canApply:false};
const out={},actions=[];
const button=({children,onSelect,theme,selected,...props})=>{actions.push({label:children,onSelect,disabled:props.disabled});return React.createElement('button',props,children)};
const code=ts.transpileModule(fs.readFileSync(path.join(ui,'src/components.tsx'),'utf8'),{compilerOptions:{module:ts.ModuleKind.CommonJS,jsx:ts.JsxEmit.React,esModuleInterop:true}}).outputText;
let sent=[];
vm.runInNewContext(code,{exports:out,require:name=>name==='react'?React:name==='cs2/api'?{bindValue:()=>0,useValue:()=>JSON.stringify(state),trigger:(group,key,value)=>sent.push(value)}:name==='cs2/ui'?{Button:button,Tooltip:({children})=>children}:name==='cs2/modding'?{getModule:()=>({})}:(()=>{throw Error(name)})()});
const check=(ok,label)=>{if(!ok)throw Error(label)};
const render=()=>{actions.length=0;return renderToStaticMarkup(React.createElement(out.ComponentsContent))};
let html=render();check(!html.includes('Add missing components with their copied settings'),'mode explanation moved to tooltip');check(html.includes('Components (1)'),'heading is one text node');
check(actions.find(a=>a.label==='Paste').disabled,'apply needs preview');
actions.find(a=>a.label==='Copy all').onSelect();check(sent.pop()==='copy|token|0','copy-all sends underlying indices and selection token');
actions.find(a=>a.label==='Copy selected').onSelect();check(sent.pop()==='copy|token|0','copy-selected sends selected indices');
for(const [mode,label] of [['SettingsOnly','Replace present'],['ComponentsOnly','Add missing'],['ComponentsAndSettings','Replace and add']]){actions.find(a=>a.label===label).onSelect();check(sent.pop()==='mode|'+mode,'mode command '+mode)}
state={...state,transferMode:'SettingsOnly',canApply:true};html=render();check(!html.includes('Missing components are skipped'),'replace-present explanation hidden');check(!actions.find(a=>a.label==='Paste').disabled,'valid preview enables apply');
state={...state,transferMode:'ComponentsAndSettings'};html=render();check(!html.includes('add missing components with their settings'),'replace-plus-add explanation hidden');
state={...state,hasAsset:false,components:[],selectedIndices:[],canApply:false};render();check(!actions.find(a=>a.label==='Copy all')&&actions.find(a=>a.label==='Preview').disabled,'no selection disables copy and preview');
console.log('PASS: component transfer UI modes, copy commands and preview gating.');

