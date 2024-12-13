<script lang="ts" module>
	import { colors as enderColorScheme } from './color-scheme/green.js';
	import { colors as enderDarkColorScheme } from './color-scheme/green-dark.js';
	import { derived, get } from 'svelte/store';
	import { persisted } from 'svelte-persisted-store';

	export type ColorSchemeName = 'Green' | 'Green Dark' | string;

	export type ColorValues = Record<ColorKey, number>;

	export type ColorKey =
		| 'primary'
		| 'onPrimary'
		| 'primaryContainer'
		| 'onPrimaryContainer'
		| 'primaryVariant'
		| 'onPrimaryVariant'
		| 'primaryContainerVariant'
		| 'onPrimaryContainerVariant'
		| 'background'
		| 'backgroundVariant'
		| 'onBackground'
		| 'onBackgroundVariant'
		| 'error'
		| 'errorBackground'
		| 'onError'
		| 'warning'
		| 'warningBackground'
		| 'onWarning'
		| 'info'
		| 'infoBackground'
		| 'onInfo'
		| 'shadow';

	export const intColorToHex = (color: number): string => `#${color.toString(16)}`;

	export function registerColorScheme(theme: string, colorScheme: ColorValues) {
		if (theme in registeredColors) {
			console.warn(`Color scheme ${theme} already exists. Overriding...`);
		}

		registeredColors[theme] = colorScheme;
	}
	export function getColorInt(
		key: ColorKey,
		theme: ColorSchemeName = get(currentColorScheme)
	): number | null {
		return registeredColors?.[theme]?.[key] ?? 0;
	}

	export function getColorHex(key: ColorKey, theme?: ColorSchemeName) {
		return `#${getColorInt(key, theme)?.toString(16).padStart(8, '0')}`;
	}

	export function getColorRgba(key: ColorKey, theme?: ColorSchemeName) {
		const hex = getColorHex(key, theme);

		const red = Number.parseInt(hex.slice(0, 2), 16);
		const green = Number.parseInt(hex.slice(2, 4), 16);
		const blue = Number.parseInt(hex.slice(4, 6), 16);
		const alpha = Number.parseInt(hex.slice(6, 8), 16);

		return `rgba(${red}, ${green}, ${blue}, ${alpha / 255})`;
	}

	function generateCssPropertyDeclarations(theme: ColorSchemeName) {
		const color = registeredColors[theme];

		let style = '';

		for (const key in color) {
			if (style.length !== 0) {
				style += '; ';
			}

			const value = color[key as ColorKey].toString(16).padStart(8, '0');

			style += `--${key}: #${value}`;
		}

		return style;
	}

	export function setColorScheme(theme: ColorSchemeName) {
		if (!(theme in registeredColors)) {
			throw new Error(`Color scheme ${theme} does not exist.`);
		}

		currentColorScheme.set(theme);
	}

	export const registeredColors: Record<ColorSchemeName, ColorValues> = {
		['Green']: enderColorScheme(),
		['Green Dark']: enderDarkColorScheme()
	};

	export const currentColorScheme = persisted('color-scheme', 'Green', {
		serializer: {
			parse: (value) => (value in registeredColors ? value : 'Green'),
			stringify: (value) => value
		}
	});
</script>

<script lang="ts">
	const { colorScheme = undefined }: { colorScheme?: ColorSchemeName } = $props();

	const useColorScheme = derived(currentColorScheme, (theme) => colorScheme ?? theme);
	const style = derived(
		useColorScheme,
		(theme) => `<style>body { ${generateCssPropertyDeclarations(theme)} }</style>`
	);
</script>

<svelte:head>
	{#key $useColorScheme}
		{@html $style}
	{/key}
</svelte:head>
