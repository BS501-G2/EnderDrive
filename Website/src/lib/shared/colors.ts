export type Color = [r: number, g: number, b: number, a?: number];

export interface Palette {
	name: string;
	night: boolean;

	colors: Color[] & { length: 10 };
}

export function fromRGBHex(color: number): Color {
	return [(color >> 16) & 0xff, (color >> 8) & 0xff, (color >> 0) & 0xff];
}

export function fromRGBAHex(color: number): Color {
	return [(color >> 24) & 0xff, (color >> 16) & 0xff, (color >> 8) & 0xff, (color >> 0) & 0xff];
}

export function rgbToCss(color: Color) {
	return `rgb${color[3] != null ? 'a' : ''}(${color.join(',')})`;
}
