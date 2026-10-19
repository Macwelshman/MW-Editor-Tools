type Bounds = {left:number; right:number; top:number; bottom:number; width:number; height:number};

// Start at the sidebar edge each time, so removed icons release their space.
export function launcherLeft(base:number, own:Bounds, neighbours:Bounds[], gap:number):number {
    let left = base;
    const row = neighbours.filter(rect => rect.width > 0 && rect.height > 0 &&
        rect.top < own.bottom && rect.bottom > own.top).sort((a,b) => a.left-b.left);
    for (const rect of row) {
        if (left + own.width + gap <= rect.left) break;
        if (left < rect.right + gap && left + own.width + gap > rect.left) left = rect.right + gap;
    }
    return left;
}
