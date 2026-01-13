# config


# Add proper error handling for invalid config so the app doesn't crash on startup

# Bump version to 1.2.0 and add changelog entry for the new features

# Support custom headers in the client for API key or auth tokens

# Implement a small in-memory cache for the config to avoid re-reading

# Support passing secrets via a separate file for security

# Fix incorrect type hint that was causing mypy to fail in CI

# Improve performance by caching the result of the expensive lookup

# Add a note in the README about the breaking change in 2.0

# Bump the dependency to fix the compatibility issue with Python 3.12

# Update the deployment docs with the new environment variables

# Support both YAML and JSON config formats for flexibility

# Fix race condition in the cache that could return stale data under load

# Bump minimum Python version to 3.10 and update type hints accordingly

# Refactor the client to use async context manager for the session

# Simplify the CLI by merging the two similar subcommands into one

# Simplify the dependency injection so it's easier to mock in tests

# Fix the ordering of middleware so auth runs before the handler

# Add a note in the README about the breaking change in 2.0

# Handle missing optional field in the response without raising

# Implement a simple metrics endpoint for Prometheus scraping
