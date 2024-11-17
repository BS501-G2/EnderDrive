import {
	getContext,
	setContext
} from 'svelte';
import type { Readable } from 'svelte/store';
import {
	derived,
	get,
	type Writable
} from 'svelte/store';
import { persisted } from 'svelte-persisted-store';

import Green from '../../shared/colors/green';
import {
	rgbToCss,
	type Color,
	type Palette
} from '$lib/shared/colors';

const colors: Palette[] =
	[
		Green
	];

const colorContextName =
	'Color Context';

export interface ColorContext {
	currentPalette: Readable<Palette>;

	setColorScheme: (
		current: Palette
	) => boolean;

	useColor: Readable<
		(
			index: number,
			alpha?: number
		) => Color
	>;
	useCssColor: Readable<
		(
			index: number,
			alpha?: number
		) =>
			| `rgb(${number},${number},${number})`
			| `rgba(${number},${number},${number},${number})`
	>;

	useCssVarColor: Readable<
		(
			index: number
		) => `--${string}`
	>;

	printStyleHTML: Readable<
		() => string
	>;
}

export function useColorContext() {
	return getContext<ColorContext>(
		colorContextName
	);
}

export function createColorContext() {
	const currentPalette: Writable<Palette> =
		persisted(
			'color-scheme',
			colors[0],
			{
				serializer:
					{
						stringify:
							(
								palette
							) =>
								palette.name,
						parse:
							(
								name
							) =>
								colors.find(
									(
										palette
									) =>
										palette.name ===
										name
								) ??
								colors[0]
					}
			}
		);

	const context: ColorContext =
		{
			currentPalette:
				derived(
					currentPalette,
					(
						value
					) =>
						value ??
						colors[0]
				),

			setColorScheme:
				(
					palette: Palette
				) => {
					if (
						palette ==
						get(
							currentPalette
						)
					) {
						return false;
					}

					currentPalette.set(
						palette
					);
					return true;
				},

			useColor:
				derived(
					currentPalette,
					(
						palette
					) =>
						(
							index: number,
							alpha?: number
						) => {
							const result =
								palette
									.colors[
									index
								];
							if (
								result ==
								null
							) {
								throw new Error(
									'Invalid color index'
								);
							}

							const color: Color =
								[
									...result
								];
							if (
								alpha !=
								null
							) {
								color[3] =
									alpha;
							}

							return color as Color;
						}
				),

			useCssColor:
				derived(
					currentPalette,
					() =>
						(
							index: number,
							alpha?: number
						) => {
							const color =
								get(
									context.useColor
								)(
									index,
									alpha
								);

							return rgbToCss(
								color
							) as never;
						}
				),

			useCssVarColor:
				derived(
					currentPalette,
					() =>
						(
							index: number
						) =>
							`--color-${index + 1}` as never
				),

			printStyleHTML:
				derived(
					currentPalette,
					(
						palette
					) =>
						() => {
							let output: string =
								'';

							for (
								let index = 0;
								index <
								palette
									.colors
									.length;
								index++
							) {
								output += `${get(context.useCssVarColor)(index)}: ${get(context.useCssColor)(index)};`;
							}

							return `<style>:root{${output}}</style>`;
						}
				)
		};

	return setContext(
		colorContextName,
		context
	);
}
