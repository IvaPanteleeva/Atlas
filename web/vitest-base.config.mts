import { defineConfig } from 'vitest/config';

// Loaded by the Angular unit-test builder (angular.json: runnerConfig).
// The first lazy load of a route chunk can take >5 s on a busy machine (hook + backend tests + IDE);
// 15 s avoids false timeouts in the stop hook and in CI.
// At most 4 workers: with one worker per CPU the files fight for the CPU and the lazy-route tests time out
// when something else (dotnet test, subagents) is running too.
export default defineConfig({
    test: {
        testTimeout: 15000,
        maxWorkers: 4,
    },
});
