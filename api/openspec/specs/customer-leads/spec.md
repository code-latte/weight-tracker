# customer-leads Specification

## Purpose

Lets anyone interested in the product leave their email before they have an account, so the team can contact them later, without exposing which emails are already registered.

## Requirements

### Requirement: Anonymous callers register a lead by email
`POST /customer-leads` SHALL accept a JSON body `{ "email": "<address>" }` without authentication, store the email as a customer lead, and respond `204 No Content` with no body. A bearer token, valid or not, SHALL have no effect on the outcome.

#### Scenario: New email is registered
- **WHEN** an anonymous caller posts a well-formed email that is not yet registered
- **THEN** the response is `204 No Content`
- **AND** exactly one customer lead is stored for that email

#### Scenario: Invalid token is ignored
- **WHEN** a caller posts a well-formed email with an invalid bearer token
- **THEN** the response is `204 No Content`
- **AND** the lead is stored

### Requirement: Lead emails are validated and normalized
The email SHALL be validated with the same rules as a user's email. It SHALL be trimmed and stored in lowercase, and it SHALL be at most 254 characters. A missing, empty or malformed email SHALL be rejected with `400 Bad Request` and nothing SHALL be stored.

#### Scenario: Email is stored normalized
- **WHEN** a caller posts `"  Lead@Example.COM "`
- **THEN** the response is `204 No Content`
- **AND** the stored email is `lead@example.com`

#### Scenario: Malformed email is rejected
- **WHEN** a caller posts an email without a domain, such as `"lead@"`
- **THEN** the response is `400 Bad Request`
- **AND** no customer lead is stored

#### Scenario: Missing email is rejected
- **WHEN** a caller posts a body without `email`
- **THEN** the response is `400 Bad Request`
- **AND** no customer lead is stored

#### Scenario: Too long email is rejected
- **WHEN** a caller posts a well-formed email longer than 254 characters
- **THEN** the response is `400 Bad Request`
- **AND** no customer lead is stored

### Requirement: Registration time is recorded by the server in UTC
Each stored lead SHALL carry the UTC date and time at which the server registered it. The client SHALL NOT be able to supply or influence it.

#### Scenario: Registration time is set on creation
- **WHEN** a new email is registered
- **THEN** the stored lead's registration time is the server's UTC time at the moment of the request

#### Scenario: Client-supplied time is ignored
- **WHEN** a caller posts a body with `email` and an extra `registeredAtUtc` field
- **THEN** the response is `204 No Content`
- **AND** the stored registration time is the server's UTC time, not the supplied one

### Requirement: Each email is stored once without revealing duplicates
An email SHALL be stored as a lead at most once, compared after normalization. Registering an email that is already stored SHALL respond `204 No Content`, exactly like a new one. It SHALL NOT store another lead or change the stored one's registration time, including when two requests for the same email arrive at the same time.

#### Scenario: Duplicate email is accepted silently
- **WHEN** a caller posts an email that is already registered
- **THEN** the response is `204 No Content`
- **AND** only one customer lead exists for that email
- **AND** its registration time is unchanged

#### Scenario: Duplicate in different casing
- **WHEN** `lead@example.com` is registered and a caller then posts `LEAD@example.com`
- **THEN** the response is `204 No Content`
- **AND** only one customer lead exists for `lead@example.com`

### Requirement: Lead registration is rate limited per client
`POST /customer-leads` SHALL accept at most a configured number of requests per client within a configured time window. Requests beyond that limit SHALL be rejected with `429 Too Many Requests` without storing anything. The limit SHALL apply only to this endpoint.

#### Scenario: Requests over the limit are rejected
- **WHEN** a client sends more requests than the configured limit within one window
- **THEN** each request beyond the limit responds `429 Too Many Requests`
- **AND** the emails in rejected requests are not stored

#### Scenario: Other endpoints are not limited
- **WHEN** a client has exhausted its `POST /customer-leads` limit
- **THEN** a request to another endpoint, such as `POST /auth/login`, is not rejected with `429`

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
