const byteUnitDictionary = Object.freeze(['', 'Ki', 'Mi', 'Gi', 'Ti', 'Pi'] as const)
export function toReadableSize(bytes: number) {
  let count = 0

  while (bytes >= 1000) {
    bytes /= 1024
    count++
  }

  return `${Math.round(bytes * 100) / 100} ${byteUnitDictionary[count]}B`
}

export const bufferSize = 1024 * 256

export function useEvent<T extends HTMLElement, K extends keyof HTMLElementEventMap>(
  element: T,
  name: K,
  handler: (element: T, event: HTMLElementEventMap[K]) => void
): () => void {
  element.addEventListener(name, (event) => handler(element, event))

  return () => element.removeEventListener(name, (event) => handler(element, event))
}

export function create(element: HTMLDivElement, onToken: (token: string) => Promise<void>) {
    const { google } = window

    google.accounts.id.initialize({
      client_id: '644062157599-6ddpbis484gcesi7dljvv7ccbb63mvdj.apps.googleusercontent.com',
      callback: (credentials) => onToken(credentials.credential)
    })

    google.accounts.id.renderButton(element, {
      theme: 'outline', size: 'large'
    })

    google.accounts.id.prompt()
}
