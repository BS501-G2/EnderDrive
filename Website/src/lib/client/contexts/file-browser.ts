import { getContext, setContext, type Snippet } from 'svelte';
import { persisted } from 'svelte-persisted-store';
import { writable } from 'svelte/store';

const contextName = 'File Browser Context';

export enum FileBrowserResolveType {
	File,
	Shared,
	Starred,
	Trash
}

export type FileBrowserResolve =
	| [type: FileBrowserResolveType.File, fileId: string | null]
	| [type: FileBrowserResolveType.Shared]
	| [type: FileBrowserResolveType.Trash]
	| [type: FileBrowserResolveType.Starred];

export interface FileBrowserOptions {
	resolve: FileBrowserResolve;

	onFileId?: (
		event: MouseEvent & { currentTarget: EventTarget & HTMLButtonElement },
		fileId: string | null
	) => Promise<void>;
}

export type FileBrowserContext = ReturnType<typeof createFileBrowserContext>['context'];

export function useFileBrowserContext() {
	return getContext<FileBrowserContext>(contextName);
}

export function createFileBrowserContext() {
	const showDetails = persisted('file-browser-config-show-details', false);
	const isLoading = writable(false);
	const files = writable<File[]>([]);

	const top = writable<{ id: number; snippet: Snippet }[]>([]);
	const actions = writable<{ id: number; snippet: Snippet }[]>([]);

	const context = setContext(contextName, {
		showDetails,

		pushTop: (snippet: Snippet) => {
			const id = Math.random();

			top.update((value) => [...value, { id, snippet }]);

			return () => top.update((value) => value.filter((value) => value.id !== id));
		},

		pushAction: (snippet: Snippet) => {
			const id = Math.random();

			actions.update((actions) => [...actions, { id, snippet }]);

			return () => actions.update((actions) => actions.filter((action) => action.id !== id));
		}
	});

	return { showDetails, top, context, isLoading, files, actions };
}
