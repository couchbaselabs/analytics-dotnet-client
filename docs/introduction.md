---
_layout: landing
---

# Couchbase Analytics .NET Client

This library provides a lightweight, focused .NET client for interacting with the Couchbase Analytics service.

It is designed for high-throughput analytics workloads and exposes a simple API to:

- Create a cluster connection with credentials and options
- Configure timeouts, retries, and TLS settings
- Execute Analytics statements and stream results
- Target queries to a specific database and scope

## Supported platforms

- .NET 8.0

## Packages

This repo contains two projects:

- `Couchbase.Analytics` — the main client library
- `Couchbase.Text.Json` — internal JSON utilities used by the client

Installation instructions are in Getting Started](getting-started.md).