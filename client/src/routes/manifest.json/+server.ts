import { json } from '@sveltejs/kit';
import { getString } from '$lib/locale.svelte';
import { _sizes } from '../favicon.svg/+server';
import { LocaleKey, LocaleType } from '$lib/locale';
import { getColorHex } from '$lib/color-scheme.svelte';

export const prerender = false;

const icon = (size: number) => ({
	src: `/favicon.svg?size=${size}`,
	sizes: `${size}x${size}`,
	type: 'image/svg+xml'
});

const icons = () => _sizes.map((size) => icon(size));

const shortcut = (name: string, url: string) => ({ name, url, icons: icons() });

export async function GET(request: Request) {
	const { searchParams } = new URL(request.url);

	const locale = <LocaleType | null>searchParams.get('locale') ?? LocaleType.en_US;
	const color = <string | null>searchParams.get('theme') ?? 'Green';

	const getStringWithLocale = (key: LocaleKey) => getString(key, locale);

	return json(
		{
			lang: locale,
			dir: 'ltr',
			name: getStringWithLocale(LocaleKey.AppName),
			short_name: getStringWithLocale(LocaleKey.AppName),
			description: getStringWithLocale(LocaleKey.AppTagline),
			icons: icons(),
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
			theme_color: `${getColorHex('primaryContainer', color)}`,
			background_color: `${getColorHex('background', color)}`
		},
		{
			headers: {
				'content-type': 'application/manifest+json'
			}
		}
	);
}
