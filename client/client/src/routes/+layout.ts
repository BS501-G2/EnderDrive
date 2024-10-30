import { goto } from '$app/navigation';
import type { RequestEvent } from '@sveltejs/kit';
import { get } from 'svelte/store';
import { privacyAccepted } from './privacy-accepted';

export async function load({ url }: RequestEvent) {
	const agreementPath = '/privacy-agreement';

	if (!get(privacyAccepted) && url.pathname !== agreementPath) {
		await goto(agreementPath);
	}
}
