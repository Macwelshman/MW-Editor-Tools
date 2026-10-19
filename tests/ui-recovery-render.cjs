// Render the actual recovery panel against its published state contract.
const fs=require('fs'),path=require('path'),vm=require('vm');
const ui=path.resolve(__dirname,'../UI');
const React=require(path.join(ui,'node_modules/react'));
const {renderToStaticMarkup}=require(path.join(ui,'node_modules/react-dom/server'));
const ts=require(path.join(ui,'node_modules/typescript'));
let diagnostics=false;let selectedTab='Replacer';
let state={available:true,visible:true,asset:'Mixed copy',status:'No history',undo:0,redo:0,canUndo:false,canRedo:false,canArmBulldozerTest:true,canArmMainTest:true,nestedHistoryEnabled:false,mainHistoryEnabled:false,recent:[],settings:{windowX:.4,windowY:.18}};
const react={...React,useState:initial=>React.useState(initial===false?diagnostics:initial==='Replacer'?selectedTab:initial)};
const button=({children,onSelect,theme,selected,focusKey,disableHint,...props})=>React.createElement('button',props,children);
const exported={};
const code=ts.transpileModule(fs.readFileSync(path.join(ui,'src/index.tsx'),'utf8'),{compilerOptions:{module:ts.ModuleKind.CommonJS,jsx:ts.JsxEmit.React,esModuleInterop:true}}).outputText;
vm.runInNewContext(code,{exports:exported,require:name=>{
  if(name==='react')return react;
  if(name==='cs2/api')return {bindValue:(group,key)=>({group,key}),useValue:b=>b.key==='width'?490:JSON.stringify(state),trigger:()=>{}};
  if(name==='cs2/modding')return {getModule:()=>({button:'native-button'})};
  if(name==='cs2/ui')return {Button:button,Tooltip:({children})=>children};
  if(name==='./native-panel')return {NativePanel:({children,header})=>React.createElement('section',null,header,children)};
  if(name==='./colours/window') return {ColourVariationsContent:()=>React.createElement('div',null,'Colours content')};
    if(name==='./components') return {ComponentsContent:()=>React.createElement('div',null,'Components content')};
    if(name==='./prop-replacer') return {PropReplacerContent:()=>React.createElement('div',null,'Prop Replacer content')};
    if(name==='./multi-save') return {MultiSaveContent:()=>React.createElement('div',null,'Multi-Save content')};
    if(name==='./style.scss')return {};
  throw new Error('Unexpected import '+name);
},setTimeout,clearTimeout,setInterval,clearInterval});
const components=[];exported.default({append:(_,component)=>components.push(component)});
const render=()=>renderToStaticMarkup(React.createElement(components[1]));
const check=(ok,message)=>{if(!ok)throw new Error(message);};

let html=render();
check((html.match(/role="tab"/g)||[]).length===4,'Four focused tabs');
check(!html.includes('Automatic Undo')&&!html.includes('Last action:')&&!html.includes('Sub-element test history'),'No routine Undo transcript or documentation panel');
check(/<button[^>]*disabled=""[^>]*>Undo<\/button>/.test(html),'Empty slot disables header Undo');
state={...state,undo:1,canUndo:true,undoTarget:'Prop: Grill01'};html=render();
check(!/<button[^>]*disabled=""[^>]*>Undo<\/button>/.test(html),'Ready action enables header Undo');
check(!html.includes('Grill01'),'Undo target is not a persistent confirmation');
for(const name of ['Replacer','Components','Colours','Multi-Save']){selectedTab=name;html=render();check(html.includes('>Undo<')&&(html.match(/role="tab"/g)||[]).length===4,'Header Undo available from '+name);check(html.includes('role="tabpanel"'),'Accessible active panel');}
selectedTab='Replacer';html=render();check(html.includes('Prop Replacer content'),'Replacer content retained');
state={...state,visible:false};check(!render().includes('role="tab"'),'Closed window hidden');
console.log('PASS: four compact tabs, shared header Undo and no persistent recovery transcript.');
