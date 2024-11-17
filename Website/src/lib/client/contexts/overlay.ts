import {
  writable,
  type Writable
} from 'svelte/store';
import type { IconOptions } from '../ui/icon.svelte';
import {
  getContext,
  setContext
} from 'svelte';

export interface OverlayContext {
  pushButton: (
    tooltip: string,
    icon: IconOptions,
    onclick: (
      event: MouseEvent & {
        currentTarget: EventTarget &
          HTMLButtonElement;
      }
    ) => void
  ) => () => void;
}

export interface WindowButton {
  id: number;
  tooltip: string;

  icon: IconOptions;
  onclick: (
    event: MouseEvent & {
      currentTarget: EventTarget &
        HTMLButtonElement;
    }
  ) => void;
}

const overlayContextName =
  'Overlay Context';

export function createOverlayContext() {
  const buttons: Writable<
    WindowButton[]
  > =
    writable(
      []
    );

  const context =
    setContext<OverlayContext>(
      overlayContextName,
      {
        pushButton:
          (
            tooltip,
            icon,
            onclick
          ) => {
            const id =
              Math.random();

            buttons.update(
              (
                buttons
              ) => [
                ...buttons,
                {
                  id,
                  tooltip,
                  icon,
                  onclick
                }
              ]
            );

            return () =>
              buttons.update(
                (
                  buttons
                ) =>
                  buttons.filter(
                    (
                      button
                    ) =>
                      button.id !==
                      id
                  )
              );
          }
      }
    );

  return {
    buttons,
    context
  };
}

export function getOverlayContext() {
  return getContext<OverlayContext>(
    overlayContextName
  );
}
