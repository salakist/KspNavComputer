module.exports = {
  extends: ['@commitlint/config-conventional'],
  defaultIgnores: true,
  rules: {
    'type-enum': [
      2,
      'always',
      [
        'build',
        'chore',
        'ci',
        'docs',
        'feat',
        'fix',
        'ops',
        'perf',
        'refactor',
        'style',
        'test'
      ]
    ],
    'scope-case':    [2, 'always', ['kebab-case', 'lower-case']],
    'subject-case':  [2, 'never', ['sentence-case', 'start-case', 'pascal-case', 'upper-case']],
    'subject-full-stop': [2, 'never', '.']
  },
  helpUrl: 'https://www.conventionalcommits.org/en/v1.0.0/'
};
