# exercise-creation Specification

## Purpose

Creates a custom exercise owned by the signed-in caller through the API, checks the structural limits of its form, and
returns every outcome, including failures, as a result that a front can show.

## Requirements

### Requirement: Creating an exercise sends the request and returns the created exercise

Creating an exercise SHALL take `languageCode`, `name`, `muscle`, `type` and an optional `videoUrl`, and SHALL send
`POST /exercises` with exactly those fields as the body. On success it SHALL return the exercise the API answers with,
unchanged. It SHALL NOT send `videoUrl` when none was given.

#### Scenario: Exercise created

- **WHEN** an exercise is created with `languageCode` `es`, `name` `Remo con banda`, `muscle` `MIDDLE_BACK` and `type`
  `STRENGTH`, and the API answers 201 with the exercise
- **THEN** exactly one `POST /exercises` request is sent with that body
- **AND** the result is successful and its value is the exercise the API answered with, unchanged

#### Scenario: Video link given

- **WHEN** an exercise is created with a `videoUrl`
- **THEN** the body of the request carries that `videoUrl`

#### Scenario: Created exercise is the caller's

- **WHEN** the API answers 201 with an exercise whose `ownerId` is the caller's id
- **THEN** the exercise in the successful result has that `ownerId`

### Requirement: Exercise creation failures are returned as results

Creating an exercise SHALL return a `DomainError` result instead of throwing: `400` as `VALIDATION`, `401` as
`UNAUTHORIZED`, `403` as `FORBIDDEN`, any other status as `UNEXPECTED`, and a request that gets no response as
`NETWORK`.

#### Scenario: Invalid input

- **WHEN** the API answers 400
- **THEN** the result is a failure with code `VALIDATION`

#### Scenario: No session

- **WHEN** the API answers 401
- **THEN** the result is a failure with code `UNAUTHORIZED`

#### Scenario: Not allowed

- **WHEN** the API answers 403
- **THEN** the result is a failure with code `FORBIDDEN`

#### Scenario: Server error

- **WHEN** the API answers 500
- **THEN** the result is a failure with code `UNEXPECTED`

#### Scenario: No response

- **WHEN** the request is rejected without a response
- **THEN** the result is a failure with code `NETWORK`

### Requirement: The exercise form is checked structurally with the API's limits

The exercise form check SHALL mirror the limits the API declares and SHALL NOT add any other rule:

- the name is required and, once trimmed, SHALL be between 3 and 150 characters;
- the muscle and the type are required;
- the video link is optional and, when given and trimmed, SHALL be at most 500 characters and an absolute `http` or
  `https` address with no whitespace and a dot in its host.

It SHALL report each field's violation as a code (`required`, `minLength`, `maxLength` or `url`) and SHALL report no
violation for a valid form. It SHALL NOT compare the name with any other exercise.

#### Scenario: Valid form

- **WHEN** the name is "Remo con banda", the muscle and the type are chosen and the link is empty
- **THEN** there are no violations

#### Scenario: Name required

- **WHEN** the name is empty or only spaces
- **THEN** the name's violation is `required`

#### Scenario: Name too short

- **WHEN** the name is "ab", with or without surrounding spaces
- **THEN** the name's violation is `minLength`

#### Scenario: Name too long

- **WHEN** the name has 151 characters
- **THEN** the name's violation is `maxLength`

#### Scenario: Name at the limits

- **WHEN** the name has exactly 3 characters, or exactly 150
- **THEN** the name has no violation

#### Scenario: Muscle and type required

- **WHEN** no muscle and no type are chosen
- **THEN** the muscle's and the type's violations are `required`

#### Scenario: Link not a web address

- **WHEN** the link is "ftp://example.com/video", "example", "https://example" or "https://exa mple.com"
- **THEN** the link's violation is `url`

#### Scenario: Link too long

- **WHEN** the link has more than 500 characters
- **THEN** the link's violation is `maxLength`

#### Scenario: Link valid

- **WHEN** the link is "https://www.youtube.com/watch?v=abc"
- **THEN** the link has no violation
