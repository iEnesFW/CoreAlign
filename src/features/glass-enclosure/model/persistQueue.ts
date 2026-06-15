let tail: Promise<unknown> = Promise.resolve();

export const enqueuePersist = <T>(task: () => Promise<T>): Promise<T> => {
  const run = tail.then(task, task);
  tail = run.then(
    () => undefined,
    () => undefined,
  );
  return run;
};
