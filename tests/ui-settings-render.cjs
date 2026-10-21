// Exercise the actual Settings component with its published state contract.
// This runs React rendering; it does not simulate Cohtml or game input.
const fs = require('fs');
const path = require('path');
const vm = require('vm');
const ui = path.resolve(__dirname, '../UI');
const React = require(path.join(ui, 'node_modules/react'));
const {renderToStaticMarkup} = require(path.join(ui, 'node_modules/react-dom/server'));
const ts = require(path.join(ui, 'node_modules/typescript'));
let settings = {confirmation:true,recovery:true,shortcuts:true,inspectorControls:true,historyDepth:20,undoKey:'Z',redoKey:'Z',windowX:.4,windowY:.18};
const react = {...React, useState: initial => React.useState(initial)};
const button = ({children,onSelect,theme,selected,focusKey,disableHint,...props}) => React.createElement('button',props,children);
const exportsObject = {};
const code = ts.transpileModule(fs.readFileSync(path.join(ui,'src/index.tsx'),'utf8'),{compilerOptions:{module:ts.ModuleKind.CommonJS,jsx:ts.JsxEmit.React,esModuleInterop:true}}).outputText;
vm.runInNewContext(code,{exports:exportsObject,require:name=>{
  if(name==='./component-headings')return {registerComponentHeadings:()=>{}};
  if(name==='./assets/mw-editor-tools.png')return 'icon.png';
  if(name==='./launcher-position')return {};
    if(name==='react') return react;
    if(name==='cs2/api') return {bindValue:(group,key)=>({group,key}),useValue:b=>b.key==='width'?490:JSON.stringify({available:true,visible:true,asset:'No asset selected',status:'No history',undo:0,redo:0,canUndo:false,canRedo:false,recent:[],settings}),trigger:()=>{}};
    if(name==='cs2/modding') return {getModule:()=>({button:'native-button',editorButtonRow:'native-row'})};
    if(name==='cs2/ui') return {Button:button,Tooltip:({children})=>children};
    if(name==='./native-panel') return {NativePanel:({children})=>React.createElement('section',null,children)};
    if(name==='./colours/window') return {ColourVariationsContent:()=>React.createElement('div',null,'Colours content')};
    if(name==='./components') return {ComponentsContent:()=>React.createElement('div',null,'Components content')};
    if(name==='./prop-replacer') return {PropReplacerContent:()=>React.createElement('div',null,'Prop Replacer content')};
    if(name==='./multi-save') return {MultiSaveContent:()=>React.createElement('div',null,'Multi-Save content')};
    if(name==='./style.scss') return {};
    throw new Error('Unexpected import '+name);
},setTimeout,clearTimeout,setInterval,clearInterval});
const components=[];
exportsObject.default({append:(_,component)=>components.push(component)});
for(const flag of [true,false]) {
    settings={...settings,confirmation:flag,recovery:flag,shortcuts:flag,inspectorControls:flag};
    const html=renderToStaticMarkup(React.createElement(components[1]));
    if(html.includes('>Settings</button>') || html.includes('Confirm component removal and Clear lists') || html.includes('Record supported list deletions') || html.includes('Show recovery controls in the inspector')) throw new Error('Settings still in shared tools window');
    if(/<(input|select|option)\b/.test(html)) throw new Error('Unsupported HTML form control returned');
    for(const moved of ['History limit','Previous','Next','Reset window position','Enable Undo / Redo shortcuts']) if(html.includes(moved)) throw new Error('Options control still in shared window: '+moved);
}
console.log('PASS: Settings tab and moved switches absent with active settings on or off.');
