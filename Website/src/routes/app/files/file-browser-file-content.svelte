<script
	lang="ts"
>
	import { useServerContext } from '$lib/client/client';

	const {
		fileId
	}: {
		fileId: string;
	} =
		$props();
	const {
		getFileContents,
		getFileSnapshots,
		getFile,
		scanFile,
		getFileMime
	} =
		useServerContext();

	async function load() {
		const file =
			await getFile(
				fileId
			);
		const mime =
			await getFileMime(
				fileId
			);
		const fileContent =
			(
				await getFileContents(
					fileId,
					0,
					1
				)
			)[0];
		const fileSnapshot =
			(
				await getFileSnapshots(
					fileId,
					fileContent.id,
					0,
					1
				)
			)[0];
		const virusResult =
			await scanFile(
				fileId,
				fileContent.id,
				fileSnapshot.id
			);

		return {
			file,
			mime,
			fileContent,
			fileSnapshot,
			virusResult
		};
	}

	let promise =
		load();
</script>

<style
	lang="scss"
>
</style>
