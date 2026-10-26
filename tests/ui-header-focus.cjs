const fs=require('fs'),vm=require('vm'),assert=require('assert');
const React=require('../UI/node_modules/react'),ts=require('../UI/node_modules/typescript');
const react={...React,useRef:v=>({current:v}),useState:v=>[v,()=>{}],useLayoutEffect:()=>{},useEffect:()=>{}};
const out={};vm.runInNewContext(ts.transpileModule(fs.readFileSync('UI/src/native-panel.tsx','utf8'),{compilerOptions:{module:ts.ModuleKind.CommonJS,jsx:ts.JsxEmit.React,esModuleInterop:true}}).outputText,{exports:out,require:n=>n==='react'?react:{getModule:(p,n)=>n==='classes'?{}:n}});
const header=React.createElement('div',null,React.createElement('Button',{key:'undo'}),React.createElement('Button',{key:'close'}));
const tree=out.NativePanel({header,children:'body'}).props.children.props.header;
const title=tree.props.children,scope=title.props.children;
assert.equal(scope.type,'AutoNavigationScope');assert.equal(scope.props.focusKey,'mwEditorTools.header');
function mount(element,parent,registered=[]){if(!React.isValidElement(element))return registered;
 let next=parent;
 if(['AutoNavigationScope','Button'].includes(element.type)){
  if(parent.single&&parent.keys.length)throw Error('Cannot register second focus key');parent.keys.push(element);registered.push([parent,element]);
  next={single:element.type==='Button',keys:[]};
 }
 React.Children.forEach(element.props.children,c=>mount(c,next,registered));return registered;
}
assert.throws(()=>mount(header,{single:true,keys:[]}),/second focus key/,'original header reproduces duplicate registration');
const parent={single:true,keys:[]},registered=mount(scope,parent);assert.equal(parent.keys.length,1);assert.equal(registered.length,3);
for(const [owner,key] of registered.reverse()){assert(owner.keys.includes(key),'unmount must unregister its exact registered key');owner.keys.splice(owner.keys.indexOf(key),1);}
assert.equal(parent.keys.length,0);
console.log('PASS: original header focus conflict reproduced; navigation group accepts Undo/Close and unregisters exact keys without mismatch.');
