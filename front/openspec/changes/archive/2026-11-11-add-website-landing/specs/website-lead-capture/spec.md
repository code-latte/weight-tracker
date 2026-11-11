# Spec Delta

## Purpose

Lets a visitor of Trainera's public website ask for early access by leaving their email, without an account, and
registers it as a lead through the API's public `POST /customer-leads`.

## ADDED Requirements

### Requirement: The early-access form asks only for an email and consent

The Founding Trainers section SHALL hold a form, reachable at `#acceso-anticipado`, with a required "Email" field, a
required checkbox to accept the privacy policy linking to `/privacidad`, and a "Quiero participar" button. The form
SHALL NOT ask for any other data. The consent SHALL be required to send but SHALL NOT be sent to the API.

#### Scenario: Form fields

- **WHEN** the visitor reaches the early-access form
- **THEN** it shows "Email", the privacy-policy checkbox linking to `/privacidad` and "Quiero participar"
- **AND** it shows no other field

### Requirement: The form checks its structure before sending

On "Quiero participar" the form SHALL send nothing while the email is empty, not an email address or longer than 254
characters, showing "Introduce un email válido.", or while the consent is not checked, showing "Debes aceptar la
política de privacidad." Each message SHALL be tied to its field for screen readers.

#### Scenario: Invalid email

- **WHEN** the visitor types "ana@" and clicks "Quiero participar"
- **THEN** no request is sent and the email field shows "Introduce un email válido."

#### Scenario: Consent missing

- **WHEN** the visitor types a valid email without checking the consent and clicks "Quiero participar"
- **THEN** no request is sent and the checkbox shows "Debes aceptar la política de privacidad."

### Requirement: A valid form registers the email as a lead

With a valid form, "Quiero participar" SHALL send exactly one `POST /customer-leads` with the body `{ "email": <the
typed email, trimmed> }` to the configured API base address. While it runs, the button SHALL show a loading state and
be disabled, so pressing it again sends nothing more.

#### Scenario: One request

- **WHEN** the visitor types " ana@example.com ", checks the consent and clicks "Quiero participar" twice quickly
- **THEN** exactly one request is sent, with the body `{ "email": "ana@example.com" }`
- **AND** the button is disabled and shows the loading state while it runs

### Requirement: An accepted lead shows the confirmation

When the API answers `204`, the form SHALL be replaced by: "¡Gracias por tu interés! Hemos recibido tu solicitud. Nos
pondremos en contacto contigo para conocer mejor tu forma de trabajar y explicarte cómo participar en Trainera." The
same confirmation SHALL show for an email already registered, since the API answers it the same way. It SHALL NOT
promise immediate access or automatic acceptance.

#### Scenario: Accepted

- **WHEN** the API answers `204`
- **THEN** the form is replaced by the confirmation and cannot be sent again until the page is reloaded

### Requirement: A refused or failed submission says why and keeps the email

When the request fails, the form SHALL stay with the typed email and consent and show: for `400`, "Introduce un email
válido."; for `429`, "Has enviado demasiadas solicitudes. Inténtalo de nuevo en unos minutos."; for no answer or any
other status, "No hemos podido enviar tu solicitud. Inténtalo de nuevo." The visitor SHALL be able to send again.

#### Scenario: Too many requests

- **WHEN** the API answers `429`
- **THEN** the form shows "Has enviado demasiadas solicitudes. Inténtalo de nuevo en unos minutos." and keeps the email

#### Scenario: Network failure

- **WHEN** the request gets no answer
- **THEN** the form shows "No hemos podido enviar tu solicitud. Inténtalo de nuevo." and "Quiero participar" is enabled

### Requirement: Bots filling the hidden field are not registered

The form SHALL include a field hidden from visitors and from assistive technology. When that field is filled, the form
SHALL show the confirmation without sending any request.

#### Scenario: Honeypot filled

- **WHEN** the hidden field holds a value and a valid form is sent
- **THEN** no request is sent and the confirmation is shown
