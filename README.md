# SQL Analyzer Improved

An improved version of SqlAnalyzer.Net that adds:
- Support for Dapper's CommandDefinition
- Support for interpolated strings
- Fixes to the SQL parsing logic


## Analyzers

| Rule\Library                                                                           |       Dapper       |       ADO.NET      |  Entity Framework  |
|----------------------------------------------------------------------------------------|:------------------:|:------------------:|:------------------:|
| [SQL001: SQL type is not specified](rules/SQL001.md)                                   | :heavy_check_mark: |                    |                    |
| [SQL002: SQL parameters mismatch](rules/SQL002.md)                                     | :heavy_check_mark: | :heavy_check_mark: |                    |
| [SQL003: Using 'Query' method is not optimal here](rules/SQL003.md)                    | :heavy_check_mark: |                    |                    |
| [SQL004: Using 'QueryMultiple' method is not optimal here](rules/SQL004.md)            | :heavy_check_mark: |                    |                    |
| [SQL005: Using 'SaveChanges' method in a loop can affect performance](rules/SQL005.md) |                    |                    | :heavy_check_mark: |