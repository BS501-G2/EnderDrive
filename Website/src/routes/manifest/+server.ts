import defaultColors from '$lib/shared/colors/green';
import { json } from '@sveltejs/kit';
import type { RequestEvent } from './$types';
import type { Color } from '$lib/shared/colors';

const createSize = <T extends number>(size: T) =>
	({
		src: `/favicon.svg?size=${size}` as const,
		sizes: `${size}x${size}` as const,
		type: 'image/svg+xml' as const
	}) as const;

const createSizes = () =>
	[createSize(32), createSize(64), createSize(128), createSize(144), createSize(256)] as const;

const shortcut = <T extends string, V extends string>(name: T, url: V) =>
	({
		name,
		url,
		icons: createSizes()
	}) as const;

const getHex = (color: Color) => {
	let output: string = '#';

	for (const entry of color) {
		output += entry?.toString(16).padStart(2, '0') ?? '';
	}

	return output;
};

export function GET({ url }: RequestEvent) {
	const { searchParams } = new URL(url);

	const manifest = {
		lang: 'en-US',
		dir: 'ltr',
		name: 'EnderDrive',
		short_name: 'EnderDrive',
		description:
			'Secure and Private File Storage and Sharing Website for Melchora Aquino Elementary School.',
		icons: createSizes(),
		categories: ['education', 'utilities'],
		display_override: ['window-controls-overlay', 'fullscreen', 'minimal-ui'],
		display: 'standalone',
		launch_handler: {
			client_mode: 'focus-existing'
		},
		orientation: 'any',
		shortcuts: [
			shortcut('Upload', '/upload'),
			shortcut('Starred', '/starred'),
			shortcut('Feed', '/feed')
		],
		start_url: '/app',
		theme_color: `${searchParams.get('theme-color') ?? getHex(defaultColors.colors[8])}`,
		background_color: `${searchParams.get('background-color') ?? getHex(defaultColors.colors[8])}`
	} as const;

	return json(manifest);
}
