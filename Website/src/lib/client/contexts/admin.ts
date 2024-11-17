import {
	writable,
	type Writable
} from 'svelte/store';
import type { IconOptions } from '../ui/icon.svelte';
import {
	getContext,
	setContext,
	type Snippet
} from 'svelte';
import type { Page } from '@sveltejs/kit';

const contextName =
	'Admin Context';

export type AdminContext =
	ReturnType<
		typeof createAdminContext
	>['context'];

export function useAdminContext() {
	return getContext<AdminContext>(
		contextName
	);
}

export interface Tab {
	id: number;
	name: string;
	callback: (
		page: Page<
			Record<
				string,
				string
			>,
			| string
			| null
		>
	) => {
		path: string;
		icon: IconOptions;
		selected: boolean;
	};
}

export function createAdminContext() {
	const tabs: Writable<
		Tab[]
	> =
		writable(
			[]
		);
	const sidePanel: Writable<
		{
			id: number;
			name: string;
			icon: IconOptions;
			snippet: Snippet;
		}[]
	> =
		writable(
			[]
		);
	const titleStack: Writable<
		{
			id: number;
			title: string;
		}[]
	> =
		writable(
			[]
		);

	const context =
		setContext(
			contextName,
			{
				pushTab:
					(
						name: string,
						callback: Tab['callback']
					) => {
						const id =
							Math.random();

						tabs.update(
							(
								tabs
							) => [
								...tabs,
								{
									name,
									id,
									callback
								}
							]
						);

						return () =>
							tabs.update(
								(
									tabs
								) =>
									tabs.filter(
										(
											tab
										) =>
											tab.id !==
											id
									)
							);
					},

				pushSidePanel:
					(
						name: string,
						icon: IconOptions,
						snippet: Snippet
					) => {
						const id =
							Math.random();

						sidePanel.update(
							(
								value
							) => [
								...value,
								{
									id,
									name,
									icon,
									snippet
								}
							]
						);

						return () =>
							sidePanel.update(
								(
									value
								) =>
									value.filter(
										(
											value
										) =>
											value.id !==
											id
									)
							);
					},

				pushTitle:
					(
						title: string
					) => {
						const id =
							Math.random();

						titleStack.update(
							(
								titleStack
							) => [
								...titleStack,
								{
									id,
									title
								}
							]
						);

						return () =>
							titleStack.update(
								(
									value
								) =>
									value.filter(
										(
											value
										) =>
											value.id !==
											id
									)
							);
					}
			}
		);

	return {
		context,
		tabs,
		titleStack,
		sidePanel
	};
}
