# Project


- Implement fallback to default value when config key is missing

- Simplify the build script by using the same steps for dev and prod

- Clean up the formatting and run the linter on the changed files

- Fix the ordering of middleware so auth runs before the handler

- Support passing options through the config file as well as CLI

- Clean up unused imports and fix formatting to match the project style guide

- Correct the default so it matches what the documentation says

- Clean up the formatting and run the linter on the changed files

- Bump the tool version and update the pre-commit hook config

- Fix the memory leak in the long-running worker process

- Update the API docs with the new query parameters and examples

- Improve logging so we can trace requests through the pipeline in production

- Implement basic rate limiting to avoid overwhelming the downstream service

- Fix race condition in the cache that could return stale data under load

- Bump the library version and pin the dependency in requirements

- Clean up the test fixtures and move shared data to a single file

- Handle the case when the config file exists but is not readable

- Simplify the CLI by merging the two similar subcommands into one

- Remove the unused parameter that was left from an old refactor

- Remove the unused parameter that was left from an old refactor

- Add a unit test for the edge case when the list is empty

- Adjust the queue size to prevent drops under burst traffic

- Bump the version and tag the release in the repo

- Improve the error recovery when the database connection is lost

- Fix bug where the parser would hang on malformed input

- Update the deployment docs with the new environment variables

- Support optional config file path via env var for easier deployment

- Bump the tool version and update the pre-commit hook config

- Clean up the TODO comments that were already addressed

- Implement a simple metrics endpoint for Prometheus scraping

- Bump the CI image to use the latest stable runner version

- Support loading config from multiple files with later overriding earlier

- Remove obsolete workaround now that the upstream bug is fixed

- Simplify the dependency injection so it's easier to mock in tests

- Clean up unused imports and fix formatting to match the project style guide

- Support custom headers in the client for API key or auth tokens

- Support custom headers in the client for API key or auth tokens

- Remove the experimental feature that didn't make it into the release

- Add proper error handling for invalid config so the app doesn't crash on startup

- Remove deprecated CLI flag and update docs to use the new option

- Implement proper backoff with jitter for the retry logic

- Fix bug where the parser would hang on malformed input
