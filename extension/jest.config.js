/** @type {import('jest').Config} */
export default {
  testEnvironment: 'jest-environment-jsdom',
  extensionsToTreatAsEsm: ['.ts'],
  transform: {
    '^.+\\.tsx?$': [
      'ts-jest',
      {
        useESM: true,
        tsconfig: './tsconfig.test.json',
      },
    ],
  },
  moduleNameMapper: {
    '^(\\.{1,2}/.*)\\.js$': '$1',
  },
  setupFiles: ['./tests/setup.ts'],
  testMatch: ['**/tests/**/*.test.ts'],
};
