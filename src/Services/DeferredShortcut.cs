using System;
namespace MW.EditorTools.Services {
    internal sealed class DeferredShortcut {
        private int action=-1, frame;
        internal bool Request(int value,int now) {
            if(action!=-1 || value<0 || value>1)return false;
            action=value;frame=now;return true;
        }
        internal void Cancel(){action=-1;}
        internal void Drain(int now,bool valid,Action<int> execute) {
            if(action==-1 || now<=frame)return;
            var value=action;var age=now-frame;Cancel();
            if(valid && age<=3)execute(value);
        }
    }
}
