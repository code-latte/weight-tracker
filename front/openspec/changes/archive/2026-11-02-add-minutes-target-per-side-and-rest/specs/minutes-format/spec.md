## ADDED Requirements

### Requirement: A time in minutes is typed as M or M:SS

A time in minutes SHALL be accepted when, once surrounding spaces are trimmed, it is whole minutes (`M`), or whole
minutes, a colon and two-digit seconds from `00` to `59` (`M:SS`). It SHALL be greater than zero and at most `999:59`.
Any other text SHALL be rejected, including decimals (`1.5`, `1,5`), signs, a single-digit or out-of-range seconds part
and an empty value.

#### Scenario: Accepted values

- **WHEN** the value is `2`, `1:30`, `0:45`, `1:05` or `999:59`
- **THEN** it is accepted

#### Scenario: Rejected values

- **WHEN** the value is `0`, `0:00`, `1:60`, `1:5`, `1.5`, `1,5`, `-1`, `abc`, `1000` or empty
- **THEN** it is rejected

### Requirement: A typed time converts to minutes with at most two decimals

An accepted `M` SHALL convert to `M` minutes. An accepted `M:SS` SHALL convert to `M + SS / 60` minutes, rounded to
two decimals, the precision the API stores.

#### Scenario: Whole minutes

- **WHEN** `2` is converted
- **THEN** the result is `2`

#### Scenario: Minutes and seconds

- **WHEN** `1:30`, `0:45` and `1:20` are converted
- **THEN** the results are `1.5`, `0.75` and `1.33`

### Requirement: Minutes from the API are shown as M or M:SS

A number of minutes SHALL be shown as its whole number of seconds, rounded to the nearest second, written as `M` when
the seconds part is zero and as `M:SS` with two-digit seconds otherwise. It SHALL never be shown as a decimal and SHALL
NOT depend on the language.

#### Scenario: Whole minutes from the API

- **WHEN** the API value is `1`
- **THEN** it is shown as `1`

#### Scenario: Fractional minutes from the API

- **WHEN** the API values are `1.5`, `0.75` and `1.33`
- **THEN** they are shown as `1:30`, `0:45` and `1:20`

### Requirement: A typed time converts back to itself

Every accepted value SHALL be shown again exactly as its canonical form after converting it to minutes and back, so that
opening and saving without changes keeps the value. `M:00` SHALL come back as `M`.

#### Scenario: Round trip of every second

- **WHEN** each value from `0:01` to `2:00` is converted to minutes and back
- **THEN** each comes back as typed, except `1:00` and `2:00`, which come back as `1` and `2`
