<script lang="ts">
	import {
		useServerContext,
		type FileResource,
		type VirusReportResource
	} from '$lib/client/client';
	import LoadingSpinner from '$lib/client/ui/loading-spinner.svelte';
	import { fly } from 'svelte/transition';

	const { selectedFileIds }: { selectedFileIds: string[] } = $props();
	const { getFile, scanFile, getFileContents, getFileSnapshots } = useServerContext();

	interface FileProperties {
		files: FileResource[];
		viruses: VirusReportResource[];
	}

	let promises: Promise<FileProperties> = $state(null as never);

	$effect(() => {
		promises = (async (): Promise<FileProperties> => {
			const files = await Promise.all(selectedFileIds.map(getFile));
			const viruses = (
				await Promise.all(
					files.map(async (file) => {
						const fileContent = (await getFileContents(file.id))[0];
						const fileSnapshot = (await getFileSnapshots(file.id, fileContent.id))[0];

						return await scanFile(file.id, fileContent.id, fileSnapshot.id);
					})
				)
			).flat(1);

			return { files, viruses };
		})();
	});
</script>

<div class="properties" transition:fly={{ x: 16, duration: 150 }}>
	<h2>File</h2>
	{#await promises}
		<div class="loading">
			<LoadingSpinner size="3em" />
		</div>
	{:then files}
		{#if files}
			<pre>{JSON.stringify(files.viruses, undefined, '  ')}</pre>
		{/if}
	{/await}
</div>

<style lang="scss">
	@use '../../../global.scss' as *;

	div.loading {
		align-items: center;
		flex-direction: row;
	}
</style>
