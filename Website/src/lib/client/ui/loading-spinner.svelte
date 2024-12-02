<!-- https://www.benmvp.com/blog/how-to-create-circle-svg-gradient-loading-spinner -->

<script lang="ts" module>
  export type Size = `${number}${'px' | 'em' | 'rem'}`
</script>

<script lang="ts">
  import { writable } from 'svelte/store'
  import AnimationFrame from './animation-frame.svelte'

  const { size }: { size: Size } = $props()

  const ratio = writable<number>(0)

  const style =
    size != null
      ? `min-width: ${size}; min-height: ${size}; max-width: ${size}; max-height: ${size};`
      : `min-width: 100%; min-height: 100%; max-width: 100%; max-height: 100%;`
</script>

<AnimationFrame
  onframe={(last, current) => {
    const fps = 1000 / (current - (last ?? Date.now()))

    ratio.update((ratio) => (ratio + 1 / fps) % 1)
  }}
/>

<svg
  viewBox="0 0 200 200"
  fill="none"
  style="transform: rotate({$ratio * 360}deg); {style}"
  xmlns="http://www.w3.org/2000/svg"
>
  <defs>
    <linearGradient id="spinner-secondHalf">
      <stop offset="0%" stop-opacity="0" stop-color="currentColor" />
      <stop offset="100%" stop-opacity="0.5" stop-color="currentColor" />
    </linearGradient>
    <linearGradient id="spinner-firstHalf">
      <stop offset="0%" stop-opacity="1" stop-color="currentColor" />
      <stop offset="100%" stop-opacity="0.5" stop-color="currentColor" />
    </linearGradient>
  </defs>

  <g stroke-width="1">
    <path
      stroke="url(#spinner-secondHalf)"
      d="M 4 100 A 96 96 0 0 1 196 100"
      vector-effect="non-scaling-stroke"
    />
    <path
      stroke="url(#spinner-firstHalf)"
      d="M 196 100 A 96 96 0 0 1 4 100"
      vector-effect="non-scaling-stroke"
    />

    <path
      stroke="currentColor"
      stroke-linecap="round"
      d="M 4 100 A 96 96 0 0 1 4 98"
      vector-effect="non-scaling-stroke"
    />
  </g>

  <!-- <animateTransform
  from="0 0 0"
  to="360 0 0"
  attributeName="transform"
  type="rotate"
  repeatCount="indefinite"
  dur="1300ms"
  /> -->
</svg>
