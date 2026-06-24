const CHUNK_SIZE = 40;

const chunk = (files) => {
  const groups = [];
  for (let i = 0; i < files.length; i += CHUNK_SIZE) {
    groups.push(files.slice(i, i + CHUNK_SIZE));
  }
  return groups;
};

const quote = (files) => files.map((file) => `"${file}"`).join(' ');

export default {
  '*.{ts,tsx}': (files) =>
    chunk(files).flatMap((group) => [
      `eslint --max-warnings=0 --fix ${quote(group)}`,
      `prettier --write ${quote(group)}`,
    ]),
  '*.{js,jsx,json,css,md,html,yml,yaml}': (files) =>
    chunk(files).map((group) => `prettier --write ${quote(group)}`),
};
