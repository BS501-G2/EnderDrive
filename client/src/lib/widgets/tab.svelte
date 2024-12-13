<script lang="ts" module>
	import type { Snippet } from 'svelte';
	import { writable, type Writable } from 'svelte/store';

export type TabItemExtra = Record<string, unknown>;

export type TabItem<T extends TabItemExtra = TabItemExtra> = {
	view: Snippet<[setTab: SetTabFunction, tab: TabItem<T>]>;
	name: string;
} & T;

export interface TabState<T extends TabItemExtra = TabItemExtra> {
	tabs: TabItem<T>[];
	currentIndex: number;
}

export interface TabId<T extends TabItemExtra = TabItemExtra> {
	[tabIdKey]: Writable<TabState<T>>;
}

export type SetTabFunction = (tabIndex: number) => void;
export type UpdateCallback<T extends TabItemExtra = TabItemExtra> = (
	tab: TabItem<T>,
	index: number
) => void;

const tabIdKey: unique symbol = Symbol('Id Key For Tab Component');

	export const createTabId = <T extends TabItemExtra = TabItemExtra>(
		tabs: TabItem<T>[],
		initialIndex: number = 0
	): TabId<T> => ({
		[tabIdKey]: writable({
			tabs,
			currentIndex: initialIndex < tabs.length ? initialIndex : 0
		})
	});
</script>

<script lang="ts" generics="T extends TabItemExtra = TabItemExtra">
	const {
		id,
		container,
		onUpdate,
		host,
		view: customView
	}: {
		id: TabId<T>;
		onUpdate?: UpdateCallback<T>;

		container?: Snippet<[setTab: SetTabFunction, content: Snippet]>;

		host?: Snippet<[tabs: TabItem<T>[], currentIndex: number, setTab: SetTabFunction]>;
		view?: Snippet<[view: Snippet]>;
	} = $props();

	const { [tabIdKey]: state } = id;

	const setTab: SetTabFunction = (tabIndex: number) => {
		state.update((value) => {
			if (!(tabIndex in value.tabs)) {
				throw new Error(`Tab ${tabIndex} does not exist.`);
			}

			value.currentIndex = tabIndex;
			return value;
		});

		onUpdate?.($state.tabs[tabIndex], tabIndex);
	};
</script>

{#snippet content()}
	{#if host}
		{@render host($state.tabs, $state.currentIndex, setTab)}
	{/if}

	{#snippet view()}
		{@render $state.tabs[$state.currentIndex].view(setTab, $state.tabs[$state.currentIndex])}
	{/snippet}

	{#if customView}
		{@render customView(view)}
	{:else}
		{@render view()}
	{/if}
{/snippet}

{#if $state.tabs.length > 0}
	{#if container}
		{@render container(setTab, content)}
	{:else}
		{@render content()}
	{/if}
{/if}
