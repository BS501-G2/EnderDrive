<script lang="ts">
	import Button from '$lib/client/ui/button.svelte';

	// const { echo } = useClientContext();

	const sampleText = 'hello world';
	let result: string = $state('');
</script>

<p>Sample Text: {sampleText}</p>

<Button
	onclick={async () => {
		result = 'Loading...';

		try {
			const textEncoder = new TextEncoder();
			const textDecoder = new TextDecoder();

			// result = textDecoder.decode(await echo(textEncoder.encode(sampleText)));
		} catch (error: any) {
			result = `${error?.message ?? error}`;
		}
	}}
>
	<p>Request</p>
</Button>

<style lang="scss">
</style>
