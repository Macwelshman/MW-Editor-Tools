import React, { useEffect, useLayoutEffect, useRef, useState } from "react";
import { getModule } from "cs2/modding";
import { PanelProps } from "cs2/ui";

type Position = { x: number; y: number };
type WindowProps = Omit<PanelProps, "draggable"> & {
    initialPosition?: Position;
    onPositionChange?: (position: Position) => void;
};

// Native panel/focus/drag handle, with a single pixel-coordinate owner.
export function NativePanel({ header, children, initialPosition, onPositionChange, ...props }: WindowProps) {
    const panelRef = useRef<HTMLDivElement>(null);
    const opening = useRef(initialPosition ?? { x: .5, y: .5 });
    const current = useRef<Position | null>(null);
    const pending = useRef(false);
    const [position, setPosition] = useState<Position | null>(null);
    const savePosition = useRef(onPositionChange);
    savePosition.current = onPositionChange;
    useLayoutEffect(() => {
        let cancelled = false, frame = 0, attempts = 0;
        const initialize = () => {
            if (cancelled) return;
            const rect = panelRef.current?.getBoundingClientRect();
            if ((!rect || rect.width <= 0 || rect.height <= 0) && ++attempts < 60) {
                frame = window.requestAnimationFrame(initialize);
                return;
            }
            const fraction = (value: number) => Number.isFinite(value) ? Math.max(0, Math.min(1, value)) : .2;
            const start = rect && rect.width > 0 && rect.height > 0 ? {
                x: fraction(opening.current.x) * Math.max(0, window.innerWidth - rect.width),
                y: fraction(opening.current.y) * Math.max(0, window.innerHeight - rect.height)
            } : { x: 20, y: 130 };
            current.current = start;
            setPosition(start);
        };
        initialize();
        return () => { cancelled = true; window.cancelAnimationFrame(frame); };
    }, []);
    useEffect(() => {
        const save = () => {
            if (!pending.current || !current.current) return;
            pending.current = false;
            const rect = panelRef.current?.getBoundingClientRect();
            if (!rect) return;
            const normalize = (offset: number, space: number) => space > 0 ? Math.max(0, Math.min(1, offset / space)) : 0;
            savePosition.current?.({ x: normalize(current.current.x, window.innerWidth - rect.width),
                y: normalize(current.current.y, window.innerHeight - rect.height) });
        };
        window.addEventListener("mouseup", save);
        window.addEventListener("blur", save);
        return () => { window.removeEventListener("mouseup", save); window.removeEventListener("blur", save); save(); };
    }, []);
    const drag = (clientX: number, clientY: number, offsetX: number, offsetY: number) => {
        const rect = panelRef.current?.getBoundingClientRect();
        if (!rect) return;
        const next = { x: Math.max(0, Math.min(clientX - offsetX, Math.max(0, window.innerWidth - rect.width))),
            y: Math.max(0, Math.min(clientY - offsetY, Math.max(0, window.innerHeight - 40))) };
        current.current = next;
        pending.current = true;
        setPosition(next);
    };
    const Panel = getModule("game-ui/common/panel/panel.tsx", "Panel") as React.ComponentType<PanelProps & React.RefAttributes<HTMLDivElement>>;
    const DragHandle = getModule("game-ui/common/input/drag-handle.tsx", "DragHandle") as
        React.ComponentType<React.PropsWithChildren<{ onDrag: typeof drag }>>;
    const TitleBar = getModule("game-ui/common/panel/panel-title-bar.tsx", "PanelTitleBar") as React.ComponentType<React.PropsWithChildren<{}>>;
    const ContentNavigation = getModule("game-ui/common/focus/auto-navigation-scope.tsx", "AutoNavigationScope") as
        React.ComponentType<React.PropsWithChildren<{ focusKey: string }>>;
    const editorTheme = getModule("game-ui/editor/themes/editor-panel.module.scss", "classes");
    return <div style={{ position:"fixed",left:0,top:0,width:"100%",height:"100%",zIndex:100,pointerEvents:"none" }}>
        <Panel {...props} ref={panelRef} transition={null} style={{...props.style,position:"absolute",left:position?.x ?? 0,top:position?.y ?? 0,
            visibility:position ? "visible" : "hidden",pointerEvents:position ? "auto" : "none"}}
            theme={editorTheme} header={<DragHandle onDrag={drag}><TitleBar><ContentNavigation focusKey="mwEditorTools.header">{header}</ContentNavigation></TitleBar></DragHandle>}>
            <ContentNavigation focusKey="mwEditorTools.content">{children}</ContentNavigation>
        </Panel>
    </div>;
}
