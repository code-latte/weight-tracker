# trainer-athletes Specification

## Purpose
Lets both fronts read and change the relationship between a trainer and their athletes through the API, returning
what the API answers unchanged and every failure as a `DomainError` code.

## Requirements

### Requirement: Listing my athletes returns what the API answers

Listing my athletes SHALL send `GET /trainer-athletes` with no body and SHALL return a successful result holding the
athletes the API answers with, unchanged and in the same order. Each one holds the relationship id, the athlete's
id, email, name and surname, the trainer's name and surname, and the relationship's start.

#### Scenario: Athletes listed

- **WHEN** my athletes are listed and the API answers with two athletes
- **THEN** exactly one `GET /trainer-athletes` request is sent with no body
- **AND** the result is successful and holds those two athletes in the API's order

#### Scenario: No athletes

- **WHEN** my athletes are listed and the API answers with an empty list
- **THEN** the result is successful and holds an empty list

### Requirement: Listing a trainer's athletes sends the trainer's id

Listing a trainer's athletes SHALL send `GET /trainer-athletes/{trainerId}` with the given id and no body, and SHALL
return the athletes the API answers with, unchanged.

#### Scenario: A trainer's athletes listed

- **WHEN** the athletes of trainer "3c4d…" are listed and the API answers with one athlete
- **THEN** exactly one `GET /trainer-athletes/3c4d…` request is sent with no body
- **AND** the result is successful and holds that athlete

### Requirement: Linking an athlete sends the athlete to the trainer's route

Linking an athlete SHALL send `POST /trainer-athletes/{trainerId}` with the given trainer id and a body holding only
the athlete's id. It SHALL return the relationship the API answers with.

#### Scenario: Athlete linked

- **WHEN** athlete "9a8b…" is linked to trainer "3c4d…" and the API accepts it
- **THEN** exactly one `POST /trainer-athletes/3c4d…` request is sent with the body `{ "athleteId": "9a8b…" }`
- **AND** the result is successful and its value is the relationship the API answered with

### Requirement: Ending a relationship sends the athlete to the trainer's end route

Ending a relationship SHALL send `PUT /trainer-athletes/{trainerId}/end` with the given trainer id and a body holding
only the athlete's id. It SHALL return the relationship the API answers with.

#### Scenario: Relationship ended

- **WHEN** the relationship between trainer "3c4d…" and athlete "9a8b…" is ended and the API accepts it
- **THEN** exactly one `PUT /trainer-athletes/3c4d…/end` request is sent with the body `{ "athleteId": "9a8b…" }`
- **AND** the result is successful and its value is the relationship the API answered with

### Requirement: Trainer athlete failures are returned as domain error codes

No trainer athlete operation SHALL throw. A failed request SHALL return a failed result whose code comes from the
shared HTTP error mapping. A status the mapping does not list SHALL become `UNEXPECTED`. None SHALL check the caller's
role.

#### Scenario: Caller not allowed

- **WHEN** the API answers any operation with `401` or `403`
- **THEN** the result is a failure with code `UNAUTHORIZED` or `FORBIDDEN` respectively

#### Scenario: Unknown trainer or relationship

- **WHEN** the API answers listing a trainer's athletes, linking or ending with `404`
- **THEN** the result is a failure with code `NOT_FOUND`

#### Scenario: Link refused

- **WHEN** the API answers linking with `409`
- **THEN** the result is a failure with code `CONFLICT` and the API's detail kept in the message

#### Scenario: Rejected body

- **WHEN** the API answers linking or ending with `400`
- **THEN** the result is a failure with code `VALIDATION`

#### Scenario: Server error or no connection

- **WHEN** the API answers with `500`, or the request cannot reach the API
- **THEN** the result is a failure with code `UNEXPECTED` or `NETWORK` respectively
