import { defineConfig } from 'orval';

export default defineConfig({
  api: {
    input: {
      target: '../wwwroot/openapi/v1.json',
    },
    output: {
      mode: 'tags-split',
      target: 'src/api/generated',
      schemas: 'src/api/generated/model',
      client: 'react-query',
      httpClient: 'fetch',
      clean: true,
      prettier: false,
      override: {
        mutator: {
          path: 'src/api/mutator/custom-fetch.ts',
          name: 'customFetch',
        },
      },
    },
  },
});
