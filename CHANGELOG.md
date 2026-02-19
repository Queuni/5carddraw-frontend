# Changelog


## 2026-01-22
- Adjust the threshold so we only log when it's actually an issue

## 2026-01-24
- Implement fallback to default value when config key is missing

## 2026-01-25
- Bump the CI image to use the latest stable runner version

## 2026-01-26
- Remove the experimental feature that didn't make it into the release

## 2026-01-30
- Correct the docstring to match the actual behavior of the function

## 2026-02-02
- Simplify the config validation by using a declarative schema

## 2026-02-03
- Clean up the formatting and run the linter on the changed files

## 2026-02-03
- Support config reload without restart via SIGHUP or file watch

## 2026-02-04
- Adjust the pool size to match the actual concurrency we need

## 2026-02-04
- Clean up the test fixtures and move shared data to a single file

## 2026-02-04
- Support config reload without restart via SIGHUP or file watch

## 2026-02-04
- Improve the error recovery when the database connection is lost

## 2026-02-06
- Implement basic rate limiting to avoid overwhelming the downstream service

## 2026-02-06
- Bump minimum Python version to 3.10 and update type hints accordingly

## 2026-02-09
- Adjust log level for noisy messages that were filling the logs

## 2026-02-12
- Clean up duplicate logic between the sync and async code paths

## 2026-02-13
- Fix race condition in the cache that could return stale data under load

## 2026-02-13
- Update the deployment docs with the new environment variables

## 2026-02-14
- Clean up the deprecated alias and point callers to the new name

## 2026-02-17
- Handle the duplicate key case by merging the values instead of failing

## 2026-02-18
- Fix the ordering of middleware so auth runs before the handler

## 2026-02-19
- Add validation for the config schema before applying settings

## 2026-02-20
- Correct the default value for the feature flag in production

## 2026-02-23
- Simplify the dependency injection so it's easier to mock in tests

## 2026-02-24
- Clean up debug print statements before the release

## 2026-02-25
- Adjust log level for noisy messages that were filling the logs

## 2026-01-15
- Handle the duplicate key case by merging the values instead of failing

## 2026-01-19
- Support config reload without restart via SIGHUP or file watch

## 2026-01-21
- Handle missing optional field in the response without raising

## 2026-01-26
- Refactor config loading into a separate module for better testability

## 2026-01-28
- Adjust the batch size to reduce memory usage on large inputs

## 2026-01-30
- Refactor config loading into a separate module for better testability

## 2026-02-09
- Adjust the default concurrency limit based on load test results

## 2026-02-10
- Remove obsolete workaround now that the upstream bug is fixed

## 2026-02-11
- Remove the deprecated wrapper and use the library API directly

## 2026-02-11
- Add proper error handling for invalid config so the app doesn't crash on startup

## 2026-02-12
- Update dependencies and resolve compatibility warning from pytest

## 2026-02-17
- Fix incorrect type hint that was causing mypy to fail in CI

## 2026-02-17
- Simplify the CLI by merging the two similar subcommands into one

## 2026-02-18
- Adjust the queue size to prevent drops under burst traffic

## 2026-02-19
- Add validation for the config schema before applying settings
