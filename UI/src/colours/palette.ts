export function color(value: string): string | null {
    const hex = value.trim().replace(/^#/, "");
    if (!/^(?:[a-f0-9]{6}|[a-f0-9]{8})$/i.test(hex)) return null;
    const rgb = [0, 2, 4].map(i => parseInt(hex.slice(i, i + 2), 16));
    const alpha = hex.length === 8 ? parseInt(hex.slice(6, 8), 16) / 255 : 1;
    return `rgba(${rgb.join(",")},${alpha})`;
}
export function paletteRows(text: string): string[][] {
    return text.split(/\r?\n/).map(line => line.trim()).filter(Boolean).map(line => line.split(/[\s,;|]+/));
}

