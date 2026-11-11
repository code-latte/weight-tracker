# Spec Delta

## ADDED Requirements

### Requirement: Administrators list every registered lead
`GET /customer-leads` SHALL return `200 OK` with a JSON array of every stored lead when the caller is an administrator. Each item SHALL contain exactly `email` (the stored, normalized email) and `registeredAtUtc` (the UTC registration time). The lead's internal identifier SHALL NOT be exposed. An empty store SHALL return an empty array.

#### Scenario: Administrator lists stored leads
- **WHEN** `lead-a@example.com` and `lead-b@example.com` have been registered and an administrator calls `GET /customer-leads`
- **THEN** the response is `200 OK`
- **AND** the body contains one item for each registered lead, with its normalized email and UTC registration time

#### Scenario: Items expose only email and registration time
- **WHEN** an administrator calls `GET /customer-leads` with at least one lead stored
- **THEN** every item has exactly the fields `email` and `registeredAtUtc`
- **AND** no item has an `id` field

#### Scenario: No leads stored
- **WHEN** no lead has been registered and an administrator calls `GET /customer-leads`
- **THEN** the response is `200 OK` with an empty array

### Requirement: Listed leads are ordered newest first
The leads returned by `GET /customer-leads` SHALL be ordered by registration time descending. Leads with the same registration time SHALL be ordered by email ascending.

#### Scenario: Newest lead comes first
- **WHEN** `first@example.com` is registered and `second@example.com` is registered later, and an administrator calls `GET /customer-leads`
- **THEN** `second@example.com` appears before `first@example.com`

### Requirement: Only administrators can list leads
`GET /customer-leads` SHALL require authentication. A request without a token or with an invalid token SHALL be rejected with `401 Unauthorized`. An authenticated caller whose role is not `ADMIN` SHALL be rejected with `403 Forbidden` and SHALL receive no lead data.

#### Scenario: Missing token
- **WHEN** a caller calls `GET /customer-leads` without a bearer token
- **THEN** the response is `401 Unauthorized`

#### Scenario: Invalid token
- **WHEN** a caller calls `GET /customer-leads` with an invalid bearer token
- **THEN** the response is `401 Unauthorized`

#### Scenario: Regular user is forbidden
- **WHEN** an authenticated `USER` calls `GET /customer-leads`
- **THEN** the response is `403 Forbidden`
- **AND** the body contains no lead email

#### Scenario: Trainer is forbidden
- **WHEN** an authenticated `TRAINER` calls `GET /customer-leads`
- **THEN** the response is `403 Forbidden`
- **AND** the body contains no lead email
