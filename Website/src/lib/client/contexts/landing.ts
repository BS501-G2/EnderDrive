import { type Snippet, getContext, setContext } from 'svelte'
import { writable, type Writable } from 'svelte/store'
import type { IconOptions } from '../ui/icon.svelte'

export interface LandingContext {
  pushLandingEntry: (name: string, content: Snippet, showButton: boolean,  scrollTop?: Writable<number>) => () => void

  pushButton: (
    content: Snippet,
    icon: IconOptions,
    isSecondary: boolean,
    onClick: () => void
  ) => () => void

  pushFooter: (name: string, content: Snippet) => () => void

  openLogin: () => void
  closeLogin: () => void
}

export type LandingPageEntry = {
  id: number
  name: string
  content: Snippet
  showButton: boolean
  scrollTop?: number
}

export type LandingPageButton = {
  id: number
  icon: IconOptions
  content: Snippet
  isSecondary: boolean
  onclick: () => void
}

export interface LandingPageFooter {
  id: number
  name: string
  content: Snippet
}

const landingContextName = 'Landing Context'

export function useLandingContext() {
  return getContext<LandingContext>(landingContextName)
}

export function createLandingContext() {
  const pages: Writable<LandingPageEntry[]> = writable([])
  const buttons: Writable<LandingPageButton[]> = writable([])
  const authenticateDialog: Writable<boolean> = writable(false)
  const footer: Writable<LandingPageFooter[]> = writable([])

  const context = setContext<LandingContext>(landingContextName, {
    pushLandingEntry: (name: string, content: Snippet, showButton: boolean) => {
      const id = Math.random()

      pages.update((pageButtons) => [
        ...pageButtons,
        {
          id,
          name,
          content,
          showButton
        }
      ])

      return () => pages.update((pageButtons) => pageButtons.filter((entry) => entry.id !== id))
    },

    pushButton: (content, icon, isSecondary, onclick) => () => {
      const id = Math.random()

      buttons.update((buttons) => [
        ...buttons,
        {
          id,
          icon,
          content,
          isSecondary,
          onclick
        }
      ])

      return () => buttons.update((buttons) => buttons.filter((entry) => entry.id !== id))
    },

    pushFooter: (name, content) => {
      const id = Math.random()

      footer.update((footer) => [
        ...footer,
        {
          id,
          name,
          content
        }
      ])

      return () => footer.update((footer) => footer.filter((entry) => entry.id !== id))
    },

    openLogin: () => {
      authenticateDialog.set(true)
    },

    closeLogin: () => {
      authenticateDialog.set(false)
    }
  })

  return {
    context,
    pages,
    footer,
    buttons,
    authenticateDialog
  }
}
