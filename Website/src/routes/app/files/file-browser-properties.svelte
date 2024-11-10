<script lang="ts">
	import { useServerContext, type FileResource } from '$lib/client/client';
	import LoadingSpinner from '$lib/client/ui/loading-spinner.svelte';
	import { fly } from 'svelte/transition';

	const { selectedFileIds }: { selectedFileIds: string[] } = $props();
	const { getFile, scanFile, getFileContents, getFileSnapshots } = useServerContext();

	interface FileProperties {
		files: FileResource[]
		viruses: string[];
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
	<h2>asdasd</h2>
	{#await promises}
		<LoadingSpinner />
	{:then files}
		{JSON.stringify(files)}
	{/await}
</div>

<style lang="scss">
	@use '../../../global.scss' as *;

	div.properties {
		@include force-size(256px, &);
	}
</style>
