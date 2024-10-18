<script lang="ts">
	import { getContext, onMount } from 'svelte';
	import { writable, type Writable } from 'svelte/store';
	import { DashboardContextName, type DashboardContext } from '../dashboard';
	import FileManager, {
		type FileManagerOnPageCallback,
		type FileManagerOnFileIdCallback
	} from '../files/file-manager.svelte';
	import { goto } from '$app/navigation';
	import type { ScanFolderSortType } from '@rizzzi/enderdrive-lib/shared';
	import Title from '$lib/widgets/title.svelte';

	const { setMainContent } = getContext<DashboardContext>(DashboardContextName);

	const refresh: Writable<() => void> = writable(null as never);
	const sort: Writable<ScanFolderSortType> = writable('fileName');

	const onPage: FileManagerOnPageCallback = (...[, page]) => {
		goto(`/app/${page}`);
	};

	const onFileId: FileManagerOnFileIdCallback = (...[, newFileId]) => {
		goto(`/app/files${newFileId != null ? `?fileId=${newFileId}` : ''}`);
	};

	onMount(() => setMainContent(layout));
</script>

<Title title="Starred" />

{#snippet layout()}
	<FileManager page="starred" {onPage} {onFileId} {refresh} {sort} />
{/snippet}
