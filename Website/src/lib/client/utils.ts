const byteUnitDictionary = Object.freeze(['', 'Ki', 'Mi', 'Gi', 'Ti', 'Pi'] as const)
export function toReadableSize(bytes: number) {
  let count = 0

  while (bytes >= 1000) {
    bytes /= 1024
    count++
  }

  return `${Math.round(bytes * 100) / 100} ${byteUnitDictionary[count]}B`
}

export const bufferSize = 1024 * 1024 * 12

export function useEvent<T extends HTMLElement, K extends keyof HTMLElementEventMap>(
  element: T,
  name: K,
  handler: (element: T, event: HTMLElementEventMap[K]) => void
): () => void {
  element.addEventListener(name, (event) => handler(element, event))

  return () => element.removeEventListener(name, (event) => handler(element, event))
}
