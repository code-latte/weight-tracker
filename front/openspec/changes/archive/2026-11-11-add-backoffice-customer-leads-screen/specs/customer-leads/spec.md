# Spec Delta

## Purpose

Lets both fronts list the customer leads collected by the public website through the API, returning every failure as
a `DomainError` code.

## ADDED Requirements

### Requirement: Listing customer leads returns what the API answers

Listing customer leads SHALL send `GET /customer-leads` with no body and SHALL return a successful result holding the
leads the API answers with, unchanged and in the same order: each lead's email and its registration time in UTC. It
SHALL NOT filter, sort or reshape the leads.

#### Scenario: Leads listed

- **WHEN** customer leads are listed and the API answers with two leads
- **THEN** exactly one `GET /customer-leads` request is sent with no body
- **AND** the result is successful and holds those two leads in the API's order

#### Scenario: Registration time kept

- **WHEN** customer leads are listed and the API answers with a lead registered on "2026-11-10T08:15:00Z"
- **THEN** the result holds that lead with the registration time "2026-11-10T08:15:00Z", unchanged

#### Scenario: Empty list

- **WHEN** customer leads are listed and the API answers with an empty list
- **THEN** the result is successful and holds an empty list

### Requirement: Listing customer leads returns failures as error codes

Listing customer leads SHALL NOT throw. Every failure SHALL be returned as a failed result holding the `DomainError`
code the HTTP error mapper assigns.

#### Scenario: Not signed in

- **WHEN** customer leads are listed and the API answers `401`
- **THEN** the result fails with `UNAUTHORIZED`

#### Scenario: Caller is not an admin

- **WHEN** customer leads are listed and the API answers `403`
- **THEN** the result fails with `FORBIDDEN`

#### Scenario: Server error

- **WHEN** customer leads are listed and the API answers `500`
- **THEN** the result fails with `UNEXPECTED`

#### Scenario: No connection

- **WHEN** customer leads are listed and the request cannot reach the API
- **THEN** the result fails with `NETWORK`
