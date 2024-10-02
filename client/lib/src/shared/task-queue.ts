interface QueueItem<T = unknown, A = never> {
  callback: () => T | Promise<T>;

  resolve: (value: T) => void;
  reject: (reason?: A) => void;

  stack: Error;
}

export interface TaskQueue {
  pushQueue: <T, A extends unknown[]>(
    callback: (...args: A) => T | Promise<T>,
    ...args: A
  ) => Promise<T>;
}

export const createTaskQueue = (): TaskQueue => {
  const queue: QueueItem[] = [];

  const runQueue = (() => {
    let isRunning = false;

    return () => {
      const run = () => {
        const item = <QueueItem>queue.shift();

        if (!item) {
          isRunning = false;
          return;
        }

        const promise = new Promise((resolve, reject) => {
          try {
            const result = item.callback();

            if (result instanceof Promise) {
              result.then(resolve).catch(reject);
            } else {
              resolve(result);
            }
          } catch (error: unknown) {
            reject(error);
          }
        });

        promise.then(item.resolve).catch(item.reject).finally(run);
      };

      if (!isRunning) {
        isRunning = true;
        run();
      }
    };
  })();

  const pushQueue = <T, A extends unknown[]>(
    callback: (...args: A) => T | Promise<T>,
    ...args: A
  ): Promise<T> => {
    return new Promise<T>((resolve, reject) => {
      queue.push({
        callback: async () => await callback(...args),
        resolve: resolve as never,
        reject,
        stack: new Error("Failed to complete task."),
      });

      runQueue();
    });
  };

  return { pushQueue };
};
